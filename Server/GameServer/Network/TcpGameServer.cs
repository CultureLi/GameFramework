using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GameServer.Configuration;
using GameServer.Game;
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
    private readonly Dictionary<Type, Func<ClientSession, IMessage, Task>> _handlers = new();
    private readonly object _onlineLock = new();
    private readonly Dictionary<string, ClientSession> _online = new(StringComparer.Ordinal);

    private TcpListener? _listener;
    private long _nextSessionId;

    public TcpGameServer(ServerConfig config)
    {
        Config = config;
        Rsa = RSA.Create(2048);
        PublicKeyXml = BuildPublicKeyXml(Rsa);

        MessageRegistry.Initialize();
        Register<LoginReq>(HandleLoginAsync);
        Register<HeartBeatReq>(HandleHeartbeatAsync);
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

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        await session.SendAsync(new LoginAck
        {
            Code = LoginCodes.Success,
            Message = result.Message,
            PlayerId = result.PlayerId,
            Token = token,
            ServerTime = UnixMilliseconds()
        });

        Log.Info($"账号 {account} 登录成功 playerId={result.PlayerId} " +
                 $"会话={session.Id} 地址={session.RemoteEndPoint}");
    }

    private static Task HandleHeartbeatAsync(ClientSession session, HeartBeatReq request)
    {
        return session.SendAsync(new HeartBeatAck
        {
            ClientTime = request.ClientTime,
            ServerTime = UnixMilliseconds()
        });
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
