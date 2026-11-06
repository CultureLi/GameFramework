using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using DotRecast.Core.Numerics;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GameServer.Configuration;
using GameServer.Game;
using GameServer.Map;
using GameServer.Pathfinding;
using GameServer.Protocol;
using GameServer.Utility;
using GameMain;
using Type = System.Type;

namespace GameServer.Network;

/// <summary>
/// TCP 游戏服务器：接受连接、管理会话、分发消息、心跳超时检测。
/// </summary>
public sealed class TcpGameServer
{
    private readonly AccountStore _accounts = new();
    private readonly ConcurrentDictionary<long, ClientSession> _sessions = new();
    private readonly ConcurrentDictionary<ulong, PlayerEntity> _players = new();
    private readonly Dictionary<Type, Func<ClientSession, IMessage, Task>> _handlers = new();
    private readonly object _onlineLock = new();
    private readonly object _playerLock = new();
    private readonly Dictionary<string, ClientSession> _online = new(StringComparer.Ordinal);
    private readonly NavMeshWorld _navMesh;
    private readonly MapDataStore _mapData;
    private readonly GateAccessStore _gateAccess;

    private TcpListener? _listener;
    private Task? _gameLoopTask;
    private long _nextSessionId;

    public TcpGameServer(ServerConfig config)
    {
        Config = config;
        Rsa = RSA.Create(2048);
        PublicKeyXml = BuildPublicKeyXml(Rsa);
        ConfigManager = new ServerConfigMgr(config.ConfigDirectory);
        _navMesh = NavMeshWorld.Load(config.NavMeshPath);
        _mapData = MapDataStore.Load(config.MapDataDirectory, ConfigManager);
        _gateAccess = new GateAccessStore(_mapData.Gates);
        _navMesh.RegisterMapGates(_mapData.Gates);

        MessageRegistry.Initialize();
        Register<LoginReq>(HandleLoginAsync);
        Register<HeartBeatReq>(HandleHeartbeatAsync);
        Register<PathFindReq>(HandlePathFindAsync);
        Register<GateStateQueryReq>(HandleGateStateQueryAsync);
        Register<GateStateReq>(HandleGateStateAsync);
        Register<SetMoveSpeedReq>(HandleSetMoveSpeedAsync);
        Register<GateInfoReq>(HandleGateInfoAsync);
        Register<GateApplyReq>(HandleGateApplyAsync);
        Register<GateApproveReq>(HandleGateApproveAsync);
        Register<GateRemoveAccessReq>(HandleGateRemoveAccessAsync);
        Register<GateClaimReq>(HandleGateClaimAsync);
    }

    public ServerConfig Config { get; }

    /// <summary>已加载的客户端配置表（与 Unity 工程共用同一份生成代码和数据）。</summary>
    public ServerConfigMgr ConfigManager { get; }

    public RSA Rsa { get; }

    public string PublicKeyXml { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(Config.Host, Config.Port);
        _listener = listener;

        try
        {
            listener.Start();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            listener.Stop();
            throw new InvalidOperationException(
                $"端口 {Config.Port} 已被占用。请关闭占用该端口的程序，" +
                $"或使用 --addr 指定其他端口，例如 --addr 127.0.0.1:19001。", ex);
        }

        Log.Info($"GameServer 已启动，监听 {Config.Host}:{Config.Port}，" +
                 $"握手超时 {Config.HandshakeTimeout.TotalSeconds:0}s，" +
                 $"心跳超时 {Config.HeartbeatTimeout.TotalSeconds:0}s");

        var monitorTask = HeartbeatMonitorAsync(cancellationToken);
        _gameLoopTask = GameLoopAsync(cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    Log.Error("接受连接失败", ex);
                    continue;
                }

                var session = new ClientSession(
                    this,
                    Interlocked.Increment(ref _nextSessionId),
                    client,
                    cancellationToken);

                _sessions[session.Id] = session;
                Log.Info($"新连接 会话={session.Id} 地址={session.RemoteEndPoint}");

                // 每个会话独立跑，内部自行捕获异常并清理
                _ = session.RunAsync();
            }
        }
        finally
        {
            listener.Stop();

            foreach (var session in _sessions.Values)
                session.Close();

            var completions = _sessions.Values.Select(s => s.Completion).ToArray();
            if (completions.Length > 0)
            {
                await Task.WhenAny(
                    Task.WhenAll(completions),
                    Task.Delay(TimeSpan.FromSeconds(2)));
            }

            try
            {
                await monitorTask;
            }
            catch (OperationCanceledException)
            {
            }

            if (_gameLoopTask != null)
            {
                try
                {
                    await _gameLoopTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            Rsa.Dispose();
            Log.Info("GameServer 已停止");
        }
    }

    /// <summary>
    /// 消息分发入口。没有注册的类型在未登录时直接断开，已登录时记录日志。
    /// </summary>
    public async Task HandleMessageAsync(ClientSession session, IMessage message)
    {
        if (_handlers.TryGetValue(message.GetType(), out var handler))
        {
            await handler(session, message);
            return;
        }

        if (!session.LoggedIn)
        {
            Log.Warn($"会话 {session.Id} 未登录就发送 {message.GetType().Name}，断开");
            session.Close();
            return;
        }

        Log.Warn($"会话 {session.Id} 未处理的消息类型 {message.GetType().Name}");
    }

    public void OnSessionClosed(ClientSession session)
    {
        _sessions.TryRemove(session.Id, out _);

        if (session.Player is { } player)
        {
            _players.TryRemove(player.PlayerId, out _);
            Log.Info($"玩家 {player.PlayerId} 已从世界移除");
        }

        var account = session.Account;
        if (account == null)
        {
            Log.Info($"会话 {session.Id} 已清理 地址={session.RemoteEndPoint}");
            return;
        }

        lock (_onlineLock)
        {
            if (_online.TryGetValue(account, out var current) && current == session)
                _online.Remove(account);
        }

        Log.Info($"账号 {account} 下线 会话={session.Id} 地址={session.RemoteEndPoint}");
    }

    private void Register<T>(Func<ClientSession, T, Task> handler) where T : IMessage
    {
        _handlers[typeof(T)] = (session, message) => handler(session, (T)message);
    }

    private async Task HandleLoginAsync(ClientSession session, LoginReq request)
    {
        if (session.LoggedIn)
        {
            await session.SendAsync(new LoginAck
            {
                Code = LoginCodes.AlreadyOnline,
                Message = "当前连接已登录"
            });
            return;
        }

        var account = request.Account.Trim();
        var password = request.Password;

        if (!IsValidAccount(account) || string.IsNullOrEmpty(password) || password.Length > 64)
        {
            await session.SendAsync(new LoginAck
            {
                Code = LoginCodes.InvalidParam,
                Message = "账号或密码格式错误"
            });
            return;
        }

        var result = _accounts.Authenticate(account, password);
        if (result.Code != LoginCodes.Success)
        {
            var failed = session.IncrementFailedLogin();
            await session.SendAsync(new LoginAck
            {
                Code = result.Code,
                Message = result.Message
            });

            Log.Warn($"账号 {account} 登录失败: {result.Message} ({failed}/5) 地址={session.RemoteEndPoint}");

            if (failed >= 5)
                session.Close();

            return;
        }

        var alreadyOnline = false;
        lock (_onlineLock)
        {
            if (_online.ContainsKey(account))
            {
                alreadyOnline = true;
            }
            else
            {
                _online[account] = session;
            }
        }

        if (alreadyOnline)
        {
            await session.SendAsync(new LoginAck
            {
                Code = LoginCodes.AlreadyOnline,
                Message = "账号已在别处登录"
            });
            Log.Warn($"账号 {account} 重复登录被拒绝 地址={session.RemoteEndPoint}");
            return;
        }

        session.SetLogin(account, result.PlayerId);

        // 登录成功后创建服务端玩家实体，出生点来自导航网格
        var spawn = _navMesh.SpawnPoint;
        var spawnPoint = new PathPoint(spawn.X, spawn.Y, spawn.Z);
        var player = new PlayerEntity(result.PlayerId, spawnPoint);
        player.GateAccessMask = _gateAccess.BuildMask(player.PlayerId);
        _players[result.PlayerId] = player;
        session.Player = player;

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        await session.SendAsync(new LoginAck
        {
            Code = LoginCodes.Success,
            Message = result.Message,
            PlayerId = result.PlayerId,
            Token = token,
            ServerTime = UnixMilliseconds(),
            SpawnPos = new Vec3 { X = spawnPoint.X, Y = spawnPoint.Y, Z = spawnPoint.Z },
            SpawnRotationY = 0f
        });

        // 初始位置同步，客户端进入场景后可以立刻对齐
        await session.SendAsync(BuildMoveSync(player));

        Log.Info($"账号 {account} 登录成功 playerId={result.PlayerId} " +
                 $"会话={session.Id} 出生点=({spawnPoint.X:F2}, {spawnPoint.Y:F2}, {spawnPoint.Z:F2}) " +
                 $"地址={session.RemoteEndPoint}");
    }

    private static Task HandleHeartbeatAsync(ClientSession session, HeartBeatReq request)
    {
        return session.SendAsync(new HeartBeatAck
        {
            ClientTime = request.ClientTime,
            ServerTime = UnixMilliseconds()
        });
    }

    private async Task HandleGateStateQueryAsync(ClientSession session, GateStateQueryReq request)
    {
        if (!session.LoggedIn)
        {
            await session.SendAsync(new GateStateSync { Code = 2 });
            return;
        }

        foreach (var gate in _navMesh.Gates)
            await session.SendAsync(BuildGateSync(gate, 0));

        Log.Info($"会话 {session.Id} 查询关卡状态，共 {_navMesh.Gates.Count} 个");
    }

    private async Task HandleGateStateAsync(ClientSession session, GateStateReq request)
    {
        if (!session.LoggedIn)
        {
            await session.SendAsync(new GateStateSync
            {
                GateId = request.GateId,
                Open = request.Open,
                Code = 2
            });
            return;
        }

        if (!_navMesh.TrySetGateState(request.GateId, request.Open))
        {
            await session.SendAsync(new GateStateSync
            {
                GateId = request.GateId,
                Open = request.Open,
                Code = 1
            });
            Log.Warn($"会话 {session.Id} 请求的关卡不存在: {request.GateId}");
            return;
        }

        var gate = _navMesh.FindGate(request.GateId);
        if (gate == null)
        {
            await session.SendAsync(new GateStateSync
            {
                GateId = request.GateId,
                Open = request.Open,
                Code = 1
            });
            return;
        }

        BroadcastGateState(BuildGateSync(gate, 0));
        Log.Info($"[gate] 关卡 {gate.GateId} {(gate.Open ? "打开" : "关闭")}，广播给所有在线玩家");
    }

    private static GateStateSync BuildGateSync(NavMeshGate gate, int code) => new()
    {
        GateId = gate.GateId,
        Open = gate.Open,
        Code = code,
        Center = new Vec3 { X = gate.Center.X, Y = gate.Center.Y, Z = gate.Center.Z },
        Size = new Vec3 { X = gate.Size.X, Y = gate.Size.Y, Z = gate.Size.Z },
        RotationY = gate.RotationY
    };

    private void BroadcastGateState(GateStateSync message)
    {
        BroadcastToPlayers(message);
    }

    private void BroadcastToPlayers(IMessage message)
    {
        foreach (var session in _sessions.Values)
        {
            if (session.Player == null)
                continue;

            _ = SafeSendAsync(session, message);
        }
    }

    private async Task HandleGateInfoAsync(ClientSession session, GateInfoReq request)
    {
        var player = session.Player;
        if (player == null)
        {
            await session.SendAsync(new GateInfoAck
            {
                Code = GateResultCode.NotLoggedIn,
                InstanceId = request.InstanceId,
                Message = "未登录",
            });
            return;
        }

        var info = GetGateInfoForViewer(request.InstanceId, player.PlayerId);
        if (info == null)
        {
            await session.SendAsync(new GateInfoAck
            {
                Code = GateResultCode.NotFound,
                InstanceId = request.InstanceId,
                Message = "关卡不存在",
            });
            return;
        }

        await session.SendAsync(BuildGateInfoAck(info));
    }

    private async Task HandleGateApplyAsync(ClientSession session, GateApplyReq request)
    {
        var player = session.Player;
        if (player == null)
        {
            await session.SendAsync(new GateApplyAck
            {
                Code = GateResultCode.NotLoggedIn,
                InstanceId = request.InstanceId,
                Message = "未登录",
            });
            return;
        }

        var result = _gateAccess.Apply(request.InstanceId, player.PlayerId);
        await session.SendAsync(new GateApplyAck
        {
            Code = result.Code,
            InstanceId = request.InstanceId,
            Status = result.Status,
            Message = result.Message,
        });

        if (result.Code == GateResultCode.Success &&
            result.Status == GateStatus.Pending &&
            result.Snapshot is { OwnerPlayerId: > 0 } snapshot)
        {
            SendGateSyncTo(snapshot.OwnerPlayerId, request.InstanceId);
        }
    }

    private async Task HandleGateApproveAsync(ClientSession session, GateApproveReq request)
    {
        var player = session.Player;
        if (player == null)
        {
            await session.SendAsync(new GateApproveAck
            {
                Code = GateResultCode.NotLoggedIn,
                InstanceId = request.InstanceId,
                Message = "未登录",
            });
            return;
        }

        var result = _gateAccess.Approve(request.InstanceId, player.PlayerId, request.PlayerId);
        await session.SendAsync(new GateApproveAck
        {
            Code = result.Code,
            InstanceId = request.InstanceId,
            Message = result.Message,
        });

        if (result.Code == GateResultCode.Success)
        {
            RefreshAllGateMasks();
            SendGateSyncTo(player.PlayerId, request.InstanceId);
            SendGateSyncTo(request.PlayerId, request.InstanceId);
        }
    }

    private async Task HandleGateRemoveAccessAsync(ClientSession session, GateRemoveAccessReq request)
    {
        var player = session.Player;
        if (player == null)
        {
            await session.SendAsync(new GateRemoveAccessAck
            {
                Code = GateResultCode.NotLoggedIn,
                InstanceId = request.InstanceId,
                Message = "未登录",
            });
            return;
        }

        var result = _gateAccess.RemoveAccess(request.InstanceId, player.PlayerId, request.PlayerId);
        await session.SendAsync(new GateRemoveAccessAck
        {
            Code = result.Code,
            InstanceId = request.InstanceId,
            Message = result.Message,
        });

        if (result.Code == GateResultCode.Success)
        {
            RefreshAllGateMasks();
            SendGateSyncTo(player.PlayerId, request.InstanceId);
            SendGateSyncTo(request.PlayerId, request.InstanceId);
        }
    }

    private async Task HandleGateClaimAsync(ClientSession session, GateClaimReq request)
    {
        var player = session.Player;
        if (player == null)
        {
            await session.SendAsync(new GateClaimAck
            {
                Code = GateResultCode.NotLoggedIn,
                InstanceId = request.InstanceId,
                Message = "未登录",
            });
            return;
        }

        var result = _gateAccess.Claim(request.InstanceId, player.PlayerId);
        await session.SendAsync(new GateClaimAck
        {
            Code = result.Code,
            InstanceId = request.InstanceId,
            Message = result.Message,
        });

        if (result.Code == GateResultCode.Success)
        {
            // 未归属门是公开的，认领后其他玩家会立即失去通行权。
            RefreshAllGateMasks();
            SendGateSyncTo(player.PlayerId, request.InstanceId);
        }
    }

    private GateInfoSnapshot? GetGateInfoForViewer(string instanceId, ulong viewerPlayerId)
    {
        var basic = _gateAccess.GetInfo(instanceId, viewerPlayerId, includeLists: false);
        if (basic == null)
            return null;

        return basic.MyStatus == GateStatus.Owner
            ? _gateAccess.GetInfo(instanceId, viewerPlayerId, includeLists: true)
            : basic;
    }

    private static GateInfoAck BuildGateInfoAck(GateInfoSnapshot info)
    {
        var ack = new GateInfoAck
        {
            Code = GateResultCode.Success,
            InstanceId = info.InstanceId,
            OwnerPlayerId = info.OwnerPlayerId,
            IsOwner = info.MyStatus == GateStatus.Owner,
            MyStatus = info.MyStatus,
        };

        foreach (ulong playerId in info.AllowedPlayers)
            ack.AllowedPlayers.Add(new GatePlayerInfo { PlayerId = playerId });

        foreach (ulong playerId in info.PendingRequests)
            ack.PendingRequests.Add(new GatePlayerInfo { PlayerId = playerId });

        return ack;
    }

    private static GateAccessSync BuildGateSync(GateInfoSnapshot info, int code = GateResultCode.Success)
    {
        var sync = new GateAccessSync
        {
            Code = code,
            InstanceId = info.InstanceId,
            OwnerPlayerId = info.OwnerPlayerId,
            MyStatus = info.MyStatus,
        };

        foreach (ulong playerId in info.AllowedPlayers)
            sync.AllowedPlayers.Add(new GatePlayerInfo { PlayerId = playerId });

        foreach (ulong playerId in info.PendingRequests)
            sync.PendingRequests.Add(new GatePlayerInfo { PlayerId = playerId });

        return sync;
    }

    private void SendGateSyncTo(ulong playerId, string instanceId)
    {
        if (playerId == 0)
            return;

        var session = FindSessionByPlayerId(playerId);
        if (session == null)
            return;

        var info = GetGateInfoForViewer(instanceId, playerId);
        if (info != null)
            _ = SafeSendAsync(session, BuildGateSync(info));
    }

    private ClientSession? FindSessionByPlayerId(ulong playerId)
    {
        foreach (var session in _sessions.Values)
        {
            if (session.Player?.PlayerId == playerId)
                return session;
        }

        return null;
    }

    /// <summary>
    /// 权限变化后重建所有在线玩家的位图。位图只是缓存，离线玩家下次登录时会重新构建。
    /// 正在移动的玩家会被停下，下一帧重新寻路。
    /// </summary>
    private void RefreshAllGateMasks()
    {
        foreach (var session in _sessions.Values)
        {
            var player = session.Player;
            if (player == null)
                continue;

            var newMask = _gateAccess.BuildMask(player.PlayerId);
            bool maskChanged = player.GateAccessMask == null ||
                               !player.GateAccessMask.ContentEquals(newMask);
            player.GateAccessMask = newMask;

            if (!maskChanged)
                continue;

            bool wasMoving;
            lock (_playerLock)
            {
                wasMoving = player.Moving;
                if (wasMoving)
                    player.Stop();
            }

            if (wasMoving)
                _ = SafeSendAsync(session, BuildMoveSync(player));
        }
    }

    private async Task HandleSetMoveSpeedAsync(ClientSession session, SetMoveSpeedReq request)    {
        var player = session.Player;
        if (player == null)
        {
            await session.SendAsync(new SetMoveSpeedSync
            {
                Speed = request.Speed,
                Code = 2
            });
            return;
        }

        float speed = request.Speed;
        if (float.IsNaN(speed) || float.IsInfinity(speed) ||
            speed < PlayerEntity.MinSpeed || speed > PlayerEntity.MaxSpeed)
        {
            await session.SendAsync(new SetMoveSpeedSync
            {
                PlayerId = player.PlayerId,
                Speed = player.Speed,
                Code = 1
            });
            Log.Warn($"玩家 {player.PlayerId} 请求的移动速度非法: {speed}");
            return;
        }

        player.SetSpeed(speed);
        BroadcastToPlayers(new SetMoveSpeedSync
        {
            PlayerId = player.PlayerId,
            Speed = player.Speed,
            Code = 0
        });

        Log.Info($"[speed] 玩家 {player.PlayerId} 移动速度设置为 {player.Speed:0.##}");
    }

    private async Task HandlePathFindAsync(ClientSession session, PathFindReq request)
    {
        var player = session.Player;
        if (player == null)
        {
            await session.SendAsync(new PathFindAck
            {
                Seq = request.Seq,
                Code = PathFindCodes.NotLoggedIn,
                Message = "未登录"
            });
            return;
        }

        if (request.Target == null)
        {
            await session.SendAsync(new PathFindAck
            {
                Seq = request.Seq,
                Code = PathFindCodes.InvalidTarget,
                Message = "目标点为空"
            });
            return;
        }

        var start = new RcVec3f(0f, 0f, 0f);
        lock (_playerLock)
        {
            start = new RcVec3f(player.X, player.Y, player.Z);
        }

        var target = new RcVec3f(request.Target.X, request.Target.Y, request.Target.Z);

        if (!_navMesh.FindPath(start, target, player.GateAccessMask, out var corners, out var error))
        {
            var code = error.Contains("目标点", StringComparison.Ordinal)
                ? PathFindCodes.InvalidTarget
                : PathFindCodes.Unreachable;

            await session.SendAsync(new PathFindAck
            {
                Seq = request.Seq,
                Code = code,
                Message = error
            });

            Log.Warn($"[path] playerId={player.PlayerId} 寻路失败: {error}");
            return;
        }

        var points = new List<PathPoint>(corners.Count);
        foreach (var corner in corners)
            points.Add(new PathPoint(corner.X, corner.Y, corner.Z));

        uint pathId;
        lock (_playerLock)
        {
            pathId = player.SetPath(points);
        }

        var ack = new PathFindAck
        {
            Seq = request.Seq,
            Code = PathFindCodes.Success,
            Message = "OK",
            PathId = pathId,
            Speed = player.Speed
        };

        foreach (var point in points)
            ack.Corners.Add(new Vec3 { X = point.X, Y = point.Y, Z = point.Z });

        if (points.Count > 0)
            ack.Start = new Vec3 { X = points[0].X, Y = points[0].Y, Z = points[0].Z };

        await session.SendAsync(ack);

        Log.Info($"[path] playerId={player.PlayerId} pathId={pathId} corners={points.Count} " +
                 $"start=({points[0].X:F2},{points[0].Y:F2},{points[0].Z:F2}) " +
                 $"end=({points[^1].X:F2},{points[^1].Y:F2},{points[^1].Z:F2})");
    }

    private async Task GameLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        var lastTimestamp = Stopwatch.GetTimestamp();
        var syncAccumulator = 0f;

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var now = Stopwatch.GetTimestamp();
                var dt = (float)(now - lastTimestamp) / Stopwatch.Frequency;
                lastTimestamp = now;

                // 心跳/切后台导致的超大 dt 限制一下，避免角色瞬移
                if (dt > 0.2f)
                    dt = 0.2f;

                foreach (var player in _players.Values)
                {
                    // 与 HandlePathFindAsync 的 SetPath 串行化
                    lock (_playerLock)
                        player.Update(dt);
                }

                syncAccumulator += dt;
                if (syncAccumulator < 0.1f)
                    continue;

                syncAccumulator = 0f;

                lock (_playerLock)
                    BroadcastMoveSync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void BroadcastMoveSync()
    {
        if (_players.IsEmpty)
            return;

        foreach (var player in _players.Values)
        {
            var message = BuildMoveSync(player);
            foreach (var session in _sessions.Values)
            {
                if (session.Player == null)
                    continue;

                _ = SafeSendAsync(session, message);
            }
        }
    }

    private static MoveSync BuildMoveSync(PlayerEntity player) => new()
    {
        PlayerId = player.PlayerId,
        PathId = player.PathId,
        Position = new Vec3 { X = player.X, Y = player.Y, Z = player.Z },
        RotationY = player.RotationY,
        Moving = player.Moving,
        CornerIndex = player.PathIndex
    };

    private async Task SafeSendAsync(ClientSession session, IMessage message)
    {
        try
        {
            await session.SendAsync(message);
        }
        catch (Exception ex)
        {
            Log.Warn($"会话 {session.Id} 位置同步失败: {ex.Message}");
            session.Close();
        }
    }

    private async Task HeartbeatMonitorAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var now = DateTime.UtcNow;

                foreach (var session in _sessions.Values)
                {
                    if (now - session.LastReceivedUtc <= Config.HeartbeatTimeout)
                        continue;

                    Log.Warn($"会话 {session.Id} 心跳超时，主动断开 地址={session.RemoteEndPoint}");
                    session.Close();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool IsValidAccount(string account)
    {
        if (account.Length is 0 or > 32)
            return false;

        foreach (var c in account)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }

    private static long UnixMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string BuildPublicKeyXml(RSA rsa)
    {
        var parameters = rsa.ExportParameters(false);
        return "<RSAKeyValue><Modulus>" + Convert.ToBase64String(parameters.Modulus!) +
               "</Modulus><Exponent>" + Convert.ToBase64String(parameters.Exponent!) +
               "</Exponent></RSAKeyValue>";
    }
}
