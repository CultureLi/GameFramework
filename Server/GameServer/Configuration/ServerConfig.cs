using System.Globalization;
using System.Net;

namespace GameServer.Configuration;

/// <summary>
/// 服务器配置，支持命令行参数：
///   --addr 0.0.0.0:19000
///   --handshake-timeout 10
///   --heartbeat-timeout 30
///   --navmesh NavData/navmesh.rnav
///   --config ../Client/Assets/StreamingAssets/Config
/// </summary>
public sealed class ServerConfig
{
    public IPAddress Host { get; init; } = IPAddress.Any;
    public int Port { get; init; } = 19000;
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public string NavMeshPath { get; init; } =
        Path.Combine(AppContext.BaseDirectory, "NavData", "navmesh.rnav");
    public string ConfigDirectory { get; init; } =
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Client", "Assets", "StreamingAssets", "Config"));

    public const string Usage =
        "用法: GameServer [--addr 0.0.0.0:19000] [--handshake-timeout 10] " +
        "[--heartbeat-timeout 30] [--navmesh NavData/navmesh.rnav] " +
        "[--config ../Client/Assets/StreamingAssets/Config]";

    public static ServerConfig Parse(string[] args)
    {
        var host = "0.0.0.0";
        var port = 19000;
        var handshakeTimeout = 10d;
        var heartbeatTimeout = 30d;
        var navMeshPath = Path.Combine(AppContext.BaseDirectory, "NavData", "navmesh.rnav");
        var configDirectory = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..", "Client", "Assets", "StreamingAssets", "Config"));

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--addr":
                    (host, port) = ParseAddress(NextValue(args, ref i));
                    break;
                case "--handshake-timeout":
                    handshakeTimeout = ParseSeconds(NextValue(args, ref i));
                    break;
                case "--heartbeat-timeout":
                    heartbeatTimeout = ParseSeconds(NextValue(args, ref i));
                    break;
                case "--navmesh":
                    navMeshPath = NextValue(args, ref i);
                    break;
                case "--config":
                    configDirectory = NextValue(args, ref i);
                    break;
                case "-h":
                case "--help":
                    throw new ArgumentException(Usage);
                default:
                    throw new ArgumentException($"未知参数: {args[i]}\n{Usage}");
            }
        }

        if (port is < 1 or > 65535)
            throw new ArgumentException($"端口非法: {port}");

        var address = host switch
        {
            "" or "*" or "0.0.0.0" => IPAddress.Any,
            _ => IPAddress.TryParse(host, out var parsed)
                ? parsed
                : throw new ArgumentException($"地址非法: {host}")
        };

        return new ServerConfig
        {
            Host = address,
            Port = port,
            HandshakeTimeout = TimeSpan.FromSeconds(handshakeTimeout),
            HeartbeatTimeout = TimeSpan.FromSeconds(heartbeatTimeout),
            NavMeshPath = navMeshPath,
            ConfigDirectory = configDirectory
        };
    }

    private static string NextValue(string[] args, ref int index)
    {
        if (++index >= args.Length)
            throw new ArgumentException($"参数 {args[index - 1]} 缺少值\n{Usage}");

        return args[index];
    }

    private static (string Host, int Port) ParseAddress(string value)
    {
        var separator = value.LastIndexOf(':');
        if (separator < 0)
            throw new ArgumentException($"地址格式应为 host:port，实际: {value}");

        var host = value[..separator];
        var portText = value[(separator + 1)..];

        if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port))
            throw new ArgumentException($"端口非法: {portText}");

        return (host, port);
    }

    private static double ParseSeconds(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds <= 0)
            throw new ArgumentException($"时间非法: {value}");

        return seconds;
    }
}
