using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GameServer.Protocol;
using GameServer.Utility;

namespace GameServer.TestClient;

/// <summary>
/// 命令行测试客户端，不依赖 Unity，用来验证服务端协议：
/// TCP -> RSA 公钥 -> AES 密钥交换 -> LZ4 -> protobuf -> 登录/心跳/断线。
/// </summary>
internal static class Program
{
    private const string Usage =
        "用法: TestClient [--addr 127.0.0.1:19000] [--account test] [--password 123456] " +
        "[--heartbeats 3] [--idle 0]";

    private static async Task<int> Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch (IOException)
        {
            // 没有控制台时忽略
        }

        var address = "127.0.0.1:19000";
        var account = "test";
        var password = "123456";
        var heartbeats = 3;
        var idleSeconds = 0;

        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--addr":
                        address = NextValue(args, ref i);
                        break;
                    case "--account":
                        account = NextValue(args, ref i);
                        break;
                    case "--password":
                        password = NextValue(args, ref i);
                        break;
                    case "--heartbeats":
                        heartbeats = int.Parse(NextValue(args, ref i));
                        break;
                    case "--idle":
                        idleSeconds = int.Parse(NextValue(args, ref i));
                        break;
                    case "-h":
                    case "--help":
                        Console.WriteLine(Usage);
                        return 0;
                    default:
                        throw new ArgumentException($"未知参数: {args[i]}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}\n{Usage}");
            return 1;
        }

        var separator = address.LastIndexOf(':');
        if (separator < 0)
        {
            Console.WriteLine($"地址格式应为 host:port，实际: {address}");
            return 1;
        }

        var host = address[..separator];
        var port = int.Parse(address[(separator + 1)..]);

        using var client = new TcpClient();
        client.NoDelay = true;

        try
        {
            using (var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                await client.ConnectAsync(host, port, connectCts.Token);

            Log.Info($"[client] 已连接 {host}:{port}");

            var stream = client.GetStream();

            AesCrypto crypto;
            using (var handshakeCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                crypto = await HandshakeAsync(stream, handshakeCts.Token);

            Log.Info("[client] 密钥交换完成");

            using (var loginCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                await SendAsync(stream, crypto, new LoginReq
                {
                    Account = account,
                    Password = password
                }, loginCts.Token);

                Log.Info($"[client] 已发送 LoginReq account={account}");

                var response = await ReceiveAsync(stream, crypto, loginCts.Token);
                if (response is not LoginAck loginAck)
                {
                    Log.Error($"[client] 期望 LoginAck，实际收到 {response.GetType().Name}");
                    return 2;
                }

                Log.Info($"[client] LoginAck code={loginAck.Code} message={loginAck.Message} " +
                         $"playerId={loginAck.PlayerId} token={loginAck.Token}");

                if (loginAck.Code != 0)
                    return 2;
            }

            if (idleSeconds > 0)
                return await RunIdleTestAsync(stream, crypto, idleSeconds);

            for (var i = 0; i < heartbeats; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));

                var clientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                using var heartbeatCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await SendAsync(stream, crypto, new HeartBeatReq { ClientTime = clientTime }, heartbeatCts.Token);

                var response = await ReceiveAsync(stream, crypto, heartbeatCts.Token);
                if (response is not HeartBeatAck heartbeatAck)
                {
                    Log.Error($"[client] 期望 HeartBeatAck，实际收到 {response.GetType().Name}");
                    return 3;
                }

                var rtt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - heartbeatAck.ClientTime;
                Log.Info($"[client] HeartBeatAck clientTime={heartbeatAck.ClientTime} " +
                         $"serverTime={heartbeatAck.ServerTime} rtt={rtt}ms");
            }

            Log.Info("[client] 测试完成");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("[client] 测试失败", ex);
            return 1;
        }
    }

    /// <summary>
    /// 登录后不再发送心跳，等待服务端按 heartbeat-timeout 主动断开。
    /// </summary>
    private static async Task<int> RunIdleTestAsync(NetworkStream stream, AesCrypto crypto, int idleSeconds)
    {
        Log.Info($"[client] 停止发送心跳 {idleSeconds}s，等待服务端断开...");
        await Task.Delay(TimeSpan.FromSeconds(idleSeconds));

        using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            var message = await ReceiveAsync(stream, crypto, waitCts.Token);
            Log.Warn($"[client] 服务端未断开，收到意外消息 {message.GetType().Name}");
            return 3;
        }
        catch (OperationCanceledException)
        {
            Log.Warn("[client] 等待服务端断开超时");
            return 3;
        }
        catch (Exception ex)
        {
            Log.Info($"[client] 服务端已断开（预期）: {ex.GetType().Name}");
            return 0;
        }
    }

    private static async Task<AesCrypto> HandshakeAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var publicKeyXml = await PacketCodec.ReadLengthPrefixedAsync(stream, 4096, cancellationToken);
        var document = XDocument.Parse(Encoding.UTF8.GetString(publicKeyXml));

        var modulus = Convert.FromBase64String(document.Root!.Element("Modulus")!.Value);
        var exponent = Convert.FromBase64String(document.Root!.Element("Exponent")!.Value);

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = modulus,
            Exponent = exponent
        });

        var aesKey = RandomNumberGenerator.GetBytes(32);
        var aesIv = RandomNumberGenerator.GetBytes(16);
        var combined = aesKey.Concat(aesIv).ToArray();
        var encryptedKey = rsa.Encrypt(combined, RSAEncryptionPadding.Pkcs1);

        await PacketCodec.WriteLengthPrefixedAsync(stream, encryptedKey, cancellationToken);
        return new AesCrypto(aesKey, aesIv);
    }

    private static async Task SendAsync(
        NetworkStream stream, AesCrypto crypto, IMessage message, CancellationToken cancellationToken)
    {
        var msgId = MessageRegistry.GetId(message.GetType());
        var body = crypto.Encrypt(message.ToByteArray());

        // 与客户端 Sender 一致：先加密再 LZ4 压缩
        body = PacketCodec.Compress(body);
        var frame = PacketCodec.EncodeFrame(msgId, PacketCodec.FlagCrypt | PacketCodec.FlagZip, body);

        await stream.WriteAsync(frame, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<IMessage> ReceiveAsync(
        NetworkStream stream, AesCrypto crypto, CancellationToken cancellationToken)
    {
        var frame = await PacketCodec.ReadFrameAsync(stream, PacketCodec.MaxScBody, cancellationToken);

        var body = frame.Body;
        if ((frame.Flags & PacketCodec.FlagZip) != 0)
            body = PacketCodec.Decompress(body);
        if ((frame.Flags & PacketCodec.FlagCrypt) != 0)
            body = crypto.Decrypt(body);

        return MessageRegistry.Parse(frame.MsgId, body);
    }

    private static string NextValue(string[] args, ref int index)
    {
        if (++index >= args.Length)
            throw new ArgumentException($"参数 {args[index - 1]} 缺少值");

        return args[index];
    }
}
