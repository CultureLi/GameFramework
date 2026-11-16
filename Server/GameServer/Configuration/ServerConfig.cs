using System.Globalization;
using System.Net;

namespace GameServer.Configuration;

/// <summary>
/// 服务器配置，支持命令行参数：
///   --addr 0.0.0.0:19000
///   --handshake-timeout 10
///   --heartbeat-timeout 30
///   --scene OpenWorld
///   --mapdata MapData/OpenWorld
///   --navmesh MapData/OpenWorld/navmesh.rnav
///   --config ../Client/Assets/StreamingAssets/Config
///
/// 默认按场景名 OpenWorld 读取：
///   &lt;当前工作目录&gt;/MapData/OpenWorld/BuildingData.bytes
///   &lt;当前工作目录&gt;/MapData/OpenWorld/navmesh.rnav
/// </summary>
public sealed class ServerConfig
{
    public const string DefaultSceneName = "OpenWorld";

    public IPAddress Host { get; init; } = IPAddress.Any;
    public int Port { get; init; } = 19000;
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>当前地图场景名，默认 OpenWorld。</summary>
    public string SceneName { get; init; } = DefaultSceneName;

    /// <summary>地图数据目录，默认 MapData/&lt;SceneName&gt;。</summary>
    public string MapDataDirectory { get; init; } =
        ResolveMapDataDirectory(DefaultSceneName);

    /// <summary>导航数据文件，默认 MapData/&lt;SceneName&gt;/navmesh.rnav。</summary>
    public string NavMeshPath { get; init; } =
        ResolveNavMeshPath(ResolveMapDataDirectory(DefaultSceneName));

    public string ConfigDirectory { get; init; } =
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Client", "Assets", "StreamingAssets", "Config"));

    public const string Usage =
        "用法: GameServer [--addr 0.0.0.0:19000] [--handshake-timeout 10] " +
        "[--heartbeat-timeout 30] [--scene OpenWorld] " +
        "[--mapdata MapData/OpenWorld] " +
        "[--navmesh MapData/OpenWorld/navmesh.rnav] " +
        "[--config ../Client/Assets/StreamingAssets/Config]";

    public static ServerConfig Parse(string[] args)
    {
        var host = "0.0.0.0";
        var port = 19000;
        var handshakeTimeout = 10d;
        var heartbeatTimeout = 30d;
        var sceneName = DefaultSceneName;
        string? mapDataDirectory = null;
        string? navMeshPath = null;
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
                case "--scene":
                    sceneName = NextValue(args, ref i).Trim();
                    break;
                case "--mapdata":
                    mapDataDirectory = Path.GetFullPath(NextValue(args, ref i));
                    break;
                case "--navmesh":
                    navMeshPath = Path.GetFullPath(NextValue(args, ref i));
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

        if (string.IsNullOrWhiteSpace(sceneName))
            throw new ArgumentException("场景名不能为空");

        // 没有显式传 --mapdata 时按场景名推导；没有显式传 --navmesh 时跟随地图数据目录。
        mapDataDirectory ??= ResolveMapDataDirectory(sceneName);
        navMeshPath ??= ResolveNavMeshPath(mapDataDirectory);

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
            SceneName = sceneName,
            MapDataDirectory = mapDataDirectory,
            NavMeshPath = navMeshPath,
            ConfigDirectory = configDirectory
        };
    }

    /// <summary>
    /// 优先使用当前工作目录下的 MapData/&lt;scene&gt;（run.bat 会先 cd 到 Server）；
    /// 找不到时再尝试程序输出目录，方便直接运行 publish 里的 exe。
    /// </summary>
    private static string ResolveMapDataDirectory(string sceneName)
    {
        string currentDirectoryPath = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "MapData", sceneName));
        if (Directory.Exists(currentDirectoryPath))
            return currentDirectoryPath;

        string baseDirectoryPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "MapData", sceneName));
        if (Directory.Exists(baseDirectoryPath))
            return baseDirectoryPath;

        return currentDirectoryPath;
    }

    /// <summary>
    /// 导航数据优先使用 MapData/&lt;scene&gt;/navmesh.rnav；
    /// 没有时兼容旧布局 NavData/navmesh.rnav。
    /// </summary>
    private static string ResolveNavMeshPath(string mapDataDirectory)
    {
        string preferred = Path.Combine(mapDataDirectory, "navmesh.rnav");
        if (File.Exists(preferred))
            return preferred;

        string legacy = Path.Combine(AppContext.BaseDirectory, "NavData", "navmesh.rnav");
        if (File.Exists(legacy))
            return legacy;

        return preferred;
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
