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
        "[--heartbeats 3] [--idle 0] [--path] [--target x,y,z] [--gates] [--speed]";

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
        var pathTest = false;
        var gateTest = false;
        var speedTest = false;
        var target = new Vec3 { X = -7.089f, Y = 0.7433f, Z = -11.9714f };

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
                    case "--path":
                        pathTest = true;
                        break;
                    case "--gates":
                        gateTest = true;
                        break;
                    case "--speed":
                        speedTest = true;
                        break;
                    case "--target":
                        target = ParseVec3(NextValue(args, ref i));
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

                if (speedTest)
                    await RunSpeedTestAsync(stream, crypto);

                if (gateTest)
                    await RunGateTestAsync(stream, crypto);

                if (pathTest)
                {
                    if (!await RunPathTestAsync(stream, crypto, loginAck, target))
                        return 4;
                }
            }

            if (idleSeconds > 0)
                return await RunIdleTestAsync(stream, crypto, idleSeconds);

            for (var i = 0; i < heartbeats; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));

                var clientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                using var heartbeatCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await SendAsync(stream, crypto, new HeartBeatReq { ClientTime = clientTime }, heartbeatCts.Token);

                IMessage response;
                do
                {
                    response = await ReceiveAsync(stream, crypto, heartbeatCts.Token);
                } while (response is MoveSync);

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
    /// 请求一次移动速度并恢复，验证服务端速度设置与广播。
    /// </summary>
    private static async Task RunSpeedTestAsync(NetworkStream stream, AesCrypto crypto)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await SendAsync(stream, crypto, new SetMoveSpeedReq { Speed = 8f }, cts.Token);
        Log.Info("[client] 请求移动速度 8");

        while (true)
        {
            var message = await ReceiveAsync(stream, crypto, cts.Token);
            if (message is not SetMoveSpeedSync sync)
                continue;

            Log.Info($"[client] SetMoveSpeedSync player={sync.PlayerId} speed={sync.Speed} code={sync.Code}");
            if (sync.Code == 0)
                break;
        }

        await SendAsync(stream, crypto, new SetMoveSpeedReq { Speed = 4f }, cts.Token);
        Log.Info("[client] 恢复移动速度 4");

        while (true)
        {
            var message = await ReceiveAsync(stream, crypto, cts.Token);
            if (message is SetMoveSpeedSync sync && sync.Code == 0)
            {
                Log.Info($"[client] SetMoveSpeedSync player={sync.PlayerId} speed={sync.Speed} code={sync.Code}");
                break;
            }
        }
    }

    /// <summary>
    /// 查询关卡门状态并切换一次，验证服务端动态障碍物加载。
    /// </summary>
    private static async Task RunGateTestAsync(NetworkStream stream, AesCrypto crypto)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await SendAsync(stream, crypto, new GateStateQueryReq(), cts.Token);
        Log.Info("[client] 已发送 GateStateQueryReq");

        GateStateSync? first = null;
        while (first == null)
        {
            var message = await ReceiveAsync(stream, crypto, cts.Token);
            if (message is MoveSync)
                continue;
            if (message is not GateStateSync sync)
                continue;

            var center = sync.Center ?? new Vec3();
            var size = sync.Size ?? new Vec3();
            Log.Info($"[client] GateStateSync gateId={sync.GateId} open={sync.Open} code={sync.Code} " +
                     $"center=({center.X:F2},{center.Y:F2},{center.Z:F2}) size=({size.X:F2},{size.Y:F2},{size.Z:F2})");
            if (sync.Code == 0 && sync.GateId > 0)
                first = sync;
        }

        bool newOpen = !first.Open;
        await SendAsync(stream, crypto, new GateStateReq
        {
            GateId = first.GateId,
            Open = newOpen
        }, cts.Token);
        Log.Info($"[client] 请求关卡 {first.GateId} -> {(newOpen ? "打开" : "关闭")}");

        while (true)
        {
            var message = await ReceiveAsync(stream, crypto, cts.Token);
            if (message is not GateStateSync sync || sync.GateId != first.GateId)
                continue;

            Log.Info($"[client] 收到关卡同步 gateId={sync.GateId} open={sync.Open} code={sync.Code}");
            break;
        }

        // 恢复原状态
        await SendAsync(stream, crypto, new GateStateReq
        {
            GateId = first.GateId,
            Open = first.Open
        }, cts.Token);
        Log.Info($"[client] 恢复关卡 {first.GateId} -> {(first.Open ? "打开" : "关闭")}");
    }

    /// <summary>
    /// 登录后发起一次寻路，打印服务端返回的路径，并持续接收位置同步直到移动结束。
    /// </summary>
    private static async Task<bool> RunPathTestAsync(
        NetworkStream stream, AesCrypto crypto, LoginAck loginAck, Vec3 target)
    {
        var spawn = loginAck.SpawnPos ?? new Vec3();
        Log.Info($"[client] 出生点 ({spawn.X:F3}, {spawn.Y:F3}, {spawn.Z:F3})");
        Log.Info($"[client] 发送 PathFindReq target=({target.X:F3}, {target.Y:F3}, {target.Z:F3})");

        using var requestCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await SendAsync(stream, crypto, new PathFindReq
        {
            Seq = 1,
            Target = target,
            ClientPos = spawn
        }, requestCts.Token);

        PathFindAck? ack = null;
        while (ack == null)
        {
            var message = await ReceiveAsync(stream, crypto, requestCts.Token);
            switch (message)
            {
                case PathFindAck pathAck:
                    ack = pathAck;
                    break;
                case MoveSync sync:
                    var syncPosition = sync.Position ?? new Vec3();
                    Log.Info($"[client] 收到 MoveSync player={sync.PlayerId} pathId={sync.PathId} " +
                             $"pos=({syncPosition.X:F2},{syncPosition.Y:F2},{syncPosition.Z:F2}) " +
                             $"moving={sync.Moving}");
                    break;
            }
        }

        Log.Info($"[client] PathFindAck seq={ack.Seq} code={ack.Code} message={ack.Message} " +
                 $"pathId={ack.PathId} corners={ack.Corners.Count} speed={ack.Speed}");

        for (var i = 0; i < ack.Corners.Count; i++)
        {
            var corner = ack.Corners[i];
            Log.Info($"[client]   corner[{i}] = ({corner.X:F3}, {corner.Y:F3}, {corner.Z:F3})");
        }

        if (ack.Code != 0)
            return false;

        using var moveCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var syncCount = 0;
        while (true)
        {
            var message = await ReceiveAsync(stream, crypto, moveCts.Token);
            if (message is not MoveSync sync)
                continue;

            syncCount++;
            var position = sync.Position ?? new Vec3();
            Log.Info($"[client] MoveSync #{syncCount} player={sync.PlayerId} pathId={sync.PathId} " +
                     $"pos=({position.X:F2},{position.Y:F2},{position.Z:F2}) " +
                     $"rot={sync.RotationY:F1} moving={sync.Moving} corner={sync.CornerIndex}");

            if (!sync.Moving && sync.PathId == ack.PathId)
                break;
        }

        Log.Info("[client] 路径移动完成");
        return true;
    }

    private static Vec3 ParseVec3(string value)
    {
        var parts = value.Split(',');
        if (parts.Length != 3 ||
            !float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var x) ||
            !float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var y) ||
            !float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var z))
        {
            throw new ArgumentException($"坐标格式应为 x,y,z，实际: {value}");
        }

        return new Vec3 { X = x, Y = y, Z = z };
    }

    /// <summary>
    /// 登录后不再发送心跳，等待服务端按 heartbeat-timeout 主动断开。
    /// </summary>
    private static async Task<int> RunIdleTestAsync(NetworkStream stream, AesCrypto crypto, int idleSeconds)
    {
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
