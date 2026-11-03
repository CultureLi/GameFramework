using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using GameServer.Protocol;
using GameServer.Utility;

namespace GameServer.Network;

/// <summary>
/// 一条客户端连接。负责握手、收包、发包、心跳时间戳和关闭清理。
/// </summary>
public sealed class ClientSession
{
    private readonly TcpGameServer _server;
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly CancellationTokenSource _cts;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _stateLock = new();
    private readonly TaskCompletionSource _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private AesCrypto? _crypto;
    private long _lastReceivedTicks = DateTime.UtcNow.Ticks;
    private int _closed;
    private int _failedLogins;
    private bool _loggedIn;
    private string? _account;
    private ulong _playerId;

    public ClientSession(TcpGameServer server, long id, TcpClient client, CancellationToken serverToken)
    {
        _server = server;
        Id = id;
        _client = client;
        _client.NoDelay = true;
        _stream = client.GetStream();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(serverToken);
        RemoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
    }

    public long Id { get; }

    public string RemoteEndPoint { get; }

    public Task Completion => _completion.Task;

    public DateTime LastReceivedUtc => new(Interlocked.Read(ref _lastReceivedTicks), DateTimeKind.Utc);

    public bool LoggedIn
    {
        get
        {
            lock (_stateLock)
                return _loggedIn;
        }
    }

    public string? Account
    {
        get
        {
            lock (_stateLock)
                return _account;
        }
    }

    public ulong PlayerId
    {
        get
        {
            lock (_stateLock)
                return _playerId;
        }
    }

    public void SetLogin(string account, ulong playerId)
    {
        lock (_stateLock)
        {
            _account = account;
            _playerId = playerId;
            _loggedIn = true;
        }
    }

    public int IncrementFailedLogin()
    {
        lock (_stateLock)
            return ++_failedLogins;
    }

    public void Touch() => Interlocked.Exchange(ref _lastReceivedTicks, DateTime.UtcNow.Ticks);

    /// <summary>
    /// 握手 + 消息循环。所有异常都在这里收敛，结束时通知服务器清理。
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            using (var handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
            {
                handshakeCts.CancelAfter(_server.Config.HandshakeTimeout);
                await HandshakeAsync(handshakeCts.Token);
            }

            Touch();
            Log.Info($"会话 {Id} 握手完成 地址={RemoteEndPoint}");

            while (!_cts.IsCancellationRequested)
            {
                var frame = await PacketCodec.ReadFrameAsync(_stream, PacketCodec.MaxCsBody, _cts.Token);
                Touch();
                await DispatchAsync(frame);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Info($"会话 {Id} 已关闭 地址={RemoteEndPoint}");
        }
        catch (EndOfStreamException)
        {
            Log.Info($"会话 {Id} 客户端断开 地址={RemoteEndPoint}");
        }
        catch (SocketException ex)
        {
            Log.Info($"会话 {Id} 网络断开: {ex.SocketErrorCode} 地址={RemoteEndPoint}");
        }
        catch (ObjectDisposedException)
        {
            Log.Info($"会话 {Id} 连接已释放 地址={RemoteEndPoint}");
        }
        catch (IOException ex)
        {
            Log.Info($"会话 {Id} IO 断开: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error($"会话 {Id} 处理异常 地址={RemoteEndPoint}", ex);
        }
        finally
        {
            Close();
            _completion.TrySetResult();
            _server.OnSessionClosed(this);
        }
    }

    /// <summary>
    /// 发送一条 protobuf 消息：protobuf 序列化 -> AES 加密 -> 加消息头。
    /// </summary>
    public async Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _closed) != 0)
            return;

        var crypto = _crypto ?? throw new InvalidOperationException("密钥交换尚未完成");

        var msgId = MessageRegistry.GetId(message.GetType());
        var body = crypto.Encrypt(message.ToByteArray());
        var frame = PacketCodec.EncodeFrame(msgId, PacketCodec.FlagCrypt, body);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        await _sendLock.WaitAsync(linkedCts.Token);
        try
        {
            await _stream.WriteAsync(frame, linkedCts.Token);
            await _stream.FlushAsync(linkedCts.Token);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 幂等关闭。心跳超时、服务器退出、客户端断开都会走到这里。
    /// </summary>
    public void Close()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
            return;

        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            _client.Close();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task HandshakeAsync(CancellationToken cancellationToken)
    {
        // 1. 明文下发 RSA 公钥 XML
        var publicKeyBytes = Encoding.UTF8.GetBytes(_server.PublicKeyXml);
        await PacketCodec.WriteLengthPrefixedAsync(_stream, publicKeyBytes, cancellationToken);

        // 2. 读取客户端用 RSA 公钥加密的 AES Key + IV
        var encryptedKey = await PacketCodec.ReadLengthPrefixedAsync(_stream, 1024, cancellationToken);

        byte[] plain;
        try
        {
            plain = _server.Rsa.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidDataException("RSA 解密 AES 密钥失败", ex);
        }

        const int ivLength = 16;
        var keyLength = plain.Length - ivLength;
        if (keyLength is not (16 or 24 or 32))
            throw new InvalidDataException($"AES 密钥长度非法: {keyLength}");

        _crypto = new AesCrypto(plain[..keyLength], plain[keyLength..]);
    }

    private async Task DispatchAsync(Frame frame)
    {
        var body = frame.Body;

        if ((frame.Flags & PacketCodec.FlagZip) != 0)
            body = PacketCodec.Decompress(body);

        // 握手完成后客户端的所有业务消息都必须是加密的
        if ((frame.Flags & PacketCodec.FlagCrypt) == 0)
            throw new InvalidDataException("收到未加密的业务消息");

        body = _crypto!.Decrypt(body);
        var message = MessageRegistry.Parse(frame.MsgId, body);
        await _server.HandleMessageAsync(this, message);
    }
}
