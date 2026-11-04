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
using GameServer.Pathfinding;
using GameServer.Protocol;
using GameServer.Utility;
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

    private TcpListener? _listener;
    private Task? _gameLoopTask;
    private long _nextSessionId;

    public TcpGameServer(ServerConfig config)
    {
        Config = config;
        Rsa = RSA.Create(2048);
        PublicKeyXml = BuildPublicKeyXml(Rsa);
        _navMesh = NavMeshWorld.Load(config.NavMeshPath);

        MessageRegistry.Initialize();
        Register<LoginReq>(HandleLoginAsync);
        Register<HeartBeatReq>(HandleHeartbeatAsync);
        Register<PathFindReq>(HandlePathFindAsync);
    }

    public ServerConfig Config { get; }

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

        if (!_navMesh.FindPath(start, target, out var corners, out var error))
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
            pathId = player.SetPath(points, 4f);
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
