using cfg;
using GameMain;
using GameServer.Configuration;
using GameServer.Utility;

namespace GameServer.Map;

/// <summary>
/// 地图上的一个关卡建筑实例。InstanceId 来自编辑器导出的建筑数据。
/// </summary>
public sealed class MapGateDefinition
{
    public required string InstanceId;
    public int ConfigId;
    public int GateIndex;
    public float PosX;
    public float PosZ;
    public float SizeX;
    public float SizeZ;
    public float RotationY;
}

/// <summary>
/// 读取客户端导出的 BuildingData.bytes，找出所有 BuildingType=Gate 的实例。
/// 服务端关卡权限、导航 poly 映射都以这里的 InstanceId 为准。
/// </summary>
public sealed class MapDataStore
{
    private readonly List<MapGateDefinition> _gates = new();

    public IReadOnlyList<MapGateDefinition> Gates => _gates;

    public static MapDataStore Load(string? directory, ServerConfigMgr configManager)
    {
        var store = new MapDataStore();

        if (string.IsNullOrWhiteSpace(directory))
        {
            Log.Warn("[mapdata] 未配置地图数据目录，跳过关卡建筑加载");
            return store;
        }

        string fullDirectory = Path.GetFullPath(directory);
        string path = Path.Combine(fullDirectory, "BuildingData.bytes");
        if (!File.Exists(path))
        {
            Log.Warn($"[mapdata] 找不到建筑数据: {path}，本次不加载关卡建筑");
            return store;
        }

        var buildingTable = configManager.GetTable<TbMapBuildingCfg>();
        var assetTable = configManager.GetTable<TbAssetPathCfg>();

        List<MapBinaryEntry> entries;
        try
        {
            entries = MapDataBinary.Read(path);
        }
        catch (Exception ex)
        {
            Log.Error($"[mapdata] 读取建筑数据失败: {path}", ex);
            return store;
        }

        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.InstanceId))
            {
                Log.Warn($"[mapdata] 建筑缺少 InstanceId，跳过: configId={entry.Id} pos=({entry.PosX},{entry.PosZ})");
                continue;
            }

            var config = buildingTable.Get(entry.Id);
            if (config == null)
            {
                Log.Warn($"[mapdata] 找不到 MapBuildingCfg Id={entry.Id}");
                continue;
            }

            if (!MapBuildingTypeHelper.IsGate(config, assetTable))
                continue;

            store._gates.Add(new MapGateDefinition
            {
                InstanceId = entry.InstanceId,
                ConfigId = entry.Id,
                PosX = entry.PosX,
                PosZ = entry.PosZ,
                SizeX = entry.SizeX,
                SizeZ = entry.SizeZ,
                RotationY = entry.RotationY,
            });
        }

        Log.Info($"[mapdata] {path} 加载完成，关卡建筑 {store._gates.Count} 个");
        return store;
    }
}
