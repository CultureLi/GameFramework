using System.Collections.Generic;
using System.IO;
using cfg;
using GameMain;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace MapEditor
{
    [System.Flags]
    public enum MapExportModule
    {
        None = 0,
        Building = 1,
        Decorate = 2,
        Resource = 4,
        All = Building | Decorate | Resource,
    }

    public sealed class MapExportResult
    {
        public bool Success;
        public bool Client;
        public bool Server;
        public MapExportModule Modules;

        public int BuildingCount;
        public int DecorateCount;
        public int ResourceCount;

        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly HashSet<string> PrefabPaths = new HashSet<string>();
        public readonly HashSet<string> InstanceIds = new HashSet<string>();
    }

    /// <summary>
    /// 把 DataEditor 下的 Building/Decorate/Resource 分类目录导出为：
    ///   客户端：Assets/BundleRes/MapData/&lt;场景名&gt;/BuildingData.asset 等 ScriptableObject
    ///   服务端：同目录下的 BuildingData.bytes 等二进制文件
    /// 支持按模块多选，All 由编辑器窗口换算成三个模块的位掩码。
    /// </summary>
    public static class MapDataExporter
    {
        private const string MapDataRoot = "Assets/BundleRes/MapData";
        private const string WallsRootName = "Walls";

        public static MapExportResult Export(
            TbMapBuildingCfg buildingTable,
            TbMapResourceCfg resourceTable,
            TbAssetPathCfg assetTable,
            MapExportModule modules,
            bool exportClient,
            bool exportServer)
        {
            var result = new MapExportResult
            {
                Client = exportClient,
                Server = exportServer,
                Modules = modules,
            };

            if (modules == MapExportModule.None)
            {
                result.Errors.Add("请至少选择一个导出模块");
                return result;
            }

            if (!exportClient && !exportServer)
            {
                result.Errors.Add("请选择导出客户端数据或服务端数据");
                return result;
            }

            // 导出时确保 DataEditor 作为 Building/Decorate/Resource 的父节点；旧布局会被迁移。
            MapEditorUtils.EnsureHierarchy(true, out var dataRoot);
            if (dataRoot == null)
            {
                result.Errors.Add("找不到 DataEditor，请先点击“创建/定位节点”。");
                return result;
            }

            var buildings = HasModule(modules, MapExportModule.Building)
                ? Collect(dataRoot, MapEntityCategory.Building,
                    buildingTable, resourceTable, assetTable, exportClient, result)
                : new List<MapItemData>();

            var decorates = HasModule(modules, MapExportModule.Decorate)
                ? Collect(dataRoot, MapEntityCategory.Decorate,
                    buildingTable, resourceTable, assetTable, exportClient, result)
                : new List<MapItemData>();

            // Walls 节点下的墙体也是可以直接用 AssetPathCfg 表达的装饰数据，
            // 导出时并入 DecorateData，运行时按 AssetPathCfg 301 实例化。
            if (HasModule(modules, MapExportModule.Decorate))
            {
                var wallsRoot = MapEditorUtils.FindChild(dataRoot, WallsRootName);
                if (wallsRoot != null)
                {
                    decorates.AddRange(CollectFromRoot(wallsRoot, MapEntityCategory.Decorate,
                        buildingTable, resourceTable, assetTable, exportClient, result));
                }
            }

            var resources = HasModule(modules, MapExportModule.Resource)
                ? Collect(dataRoot, MapEntityCategory.Resource,
                    buildingTable, resourceTable, assetTable, exportClient, result)
                : new List<MapItemData>();

            if (result.Errors.Count > 0)
                return result;

            string mapName = MapEditorUtils.SanitizeFileName(MapEditorUtils.GetMapName());
            string folder = $"{MapDataRoot}/{mapName}";

            if (!EnsureFolder(folder, mapName, result))
                return result;

            try
            {
                if (exportClient)
                {
                    if (HasModule(modules, MapExportModule.Building))
                        WriteModule<BuildingData>(folder, "BuildingData", buildings);

                    if (HasModule(modules, MapExportModule.Decorate))
                        WriteModule<DecorateData>(folder, "DecorateData", decorates);

                    if (HasModule(modules, MapExportModule.Resource))
                        WriteModule<ResourceData>(folder, "ResourceData", resources);
                }

                if (exportServer)
                {
                    if (HasModule(modules, MapExportModule.Building))
                        WriteBinary(folder, "BuildingData.bytes", buildings);

                    if (HasModule(modules, MapExportModule.Decorate))
                        WriteBinary(folder, "DecorateData.bytes", decorates);

                    if (HasModule(modules, MapExportModule.Resource))
                        WriteBinary(folder, "ResourceData.bytes", resources);
                }
            }
            catch (System.Exception ex)
            {
                result.Errors.Add("写入地图数据失败: " + ex.Message);
                return result;
            }

            // 服务端二进制不需要进 Addressables；客户端 prefab 和 SO 才需要。
            if (exportClient)
            {
                foreach (string prefabPath in result.PrefabPaths)
                    MakeAddressable(prefabPath, "Prefab", prefabPath, result);

                if (HasModule(modules, MapExportModule.Building))
                    MakeAddressable($"{folder}/BuildingData.asset", "CommonSOData",
                        $"{folder}/BuildingData.asset", result);

                if (HasModule(modules, MapExportModule.Decorate))
                    MakeAddressable($"{folder}/DecorateData.asset", "CommonSOData",
                        $"{folder}/DecorateData.asset", result);

                if (HasModule(modules, MapExportModule.Resource))
                    MakeAddressable($"{folder}/ResourceData.asset", "CommonSOData",
                        $"{folder}/ResourceData.asset", result);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            result.BuildingCount = buildings.Count;
            result.DecorateCount = decorates.Count;
            result.ResourceCount = resources.Count;
            result.Success = true;

            string target = exportClient && exportServer
                ? "客户端+服务端"
                : exportClient ? "客户端" : "服务端";

            Debug.Log($"[MapEditor] 导出完成({target}) -> {folder} " +
                      $"建筑={buildings.Count} 装饰={decorates.Count} 资源={resources.Count}");
            return result;
        }

        private static bool HasModule(MapExportModule mask, MapExportModule module)
        {
            return (mask & module) != 0;
        }

        private static List<MapItemData> Collect(
            Transform dataRoot,
            MapEntityCategory category,
            TbMapBuildingCfg buildingTable,
            TbMapResourceCfg resourceTable,
            TbAssetPathCfg assetTable,
            bool requirePrefab,
            MapExportResult result)
        {
            var root = MapEditorUtils.GetCategoryRoot(dataRoot, category);
            if (root == null)
            {
                result.Warnings.Add($"找不到分类目录: {MapEditorUtils.GetCategoryRootName(category)}");
                return new List<MapItemData>();
            }

            return CollectFromRoot(root, category,
                buildingTable, resourceTable, assetTable, requirePrefab, result);
        }

        private static List<MapItemData> CollectFromRoot(
            Transform root,
            MapEntityCategory category,
            TbMapBuildingCfg buildingTable,
            TbMapResourceCfg resourceTable,
            TbAssetPathCfg assetTable,
            bool requirePrefab,
            MapExportResult result)
        {
            var items = new List<MapItemData>();

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var info = child.GetComponent<MapEntityInfo>();
                if (info == null)
                {
                    result.Warnings.Add($"跳过没有 MapEntityInfo 的对象: {child.name}");
                    continue;
                }

                if (info.Category != category)
                {
                    result.Warnings.Add($"{child.name} 的 Category 与所在目录不一致，按目录导出为 {category}");
                }

                string instanceId = info.InstanceId;
                if (string.IsNullOrEmpty(instanceId))
                {
                    instanceId = System.Guid.NewGuid().ToString("N");
                    Undo.RecordObject(info, "Assign Map Instance Id");
                    info.InstanceId = instanceId;
                    EditorUtility.SetDirty(info);
                }

                if (!result.InstanceIds.Add(instanceId))
                {
                    result.Errors.Add($"{child.name} 的 InstanceId 重复: {instanceId}");
                    continue;
                }

                // 装饰物在 V3 直接使用 AssetPathCfg.Id；旧数据里 ConfigId 还是 MapDecorateCfg.Id，
                // AssetId 已经保存了对应的 AssetPathCfg.Id，这里优先用它做迁移。
                int exportId = info.ConfigId;
                if (category == MapEntityCategory.Decorate && info.AssetId > 0)
                    exportId = info.AssetId;

                if (exportId <= 0)
                {
                    result.Errors.Add($"{child.name} 的 ConfigId 无效: {exportId}");
                    continue;
                }

                if (!TryGetAssetId(category, exportId, buildingTable, resourceTable, assetTable, result, out int assetId))
                    continue;

                // 服务端二进制只需要 Id/Pos/Size/RotationY，不依赖客户端 prefab 是否存在。
                if (requirePrefab)
                {
                    var asset = assetTable?.Get(assetId);
                    if (asset == null || string.IsNullOrEmpty(asset.AssetPath))
                    {
                        result.Errors.Add($"{child.name} 找不到 AssetPathCfg: AssetId={assetId} (配置 Id={exportId})");
                        continue;
                    }

                    if (AssetDatabase.LoadAssetAtPath<GameObject>(asset.AssetPath) == null)
                    {
                        result.Errors.Add($"{child.name} 的资源不存在: {asset.AssetPath}");
                        continue;
                    }

                    result.PrefabPaths.Add(asset.AssetPath);
                }

                Vector2 size = MapEditorUtils.MeasureSize(child.gameObject);
                if (size.x <= 0.01f || size.y <= 0.01f)
                    result.Warnings.Add($"{child.name} 没有 Collider/Renderer，Size 为 0");

                items.Add(new MapItemData
                {
                    InstanceId = instanceId,
                    Id = exportId,
                    Pos = new Vector2(child.position.x, child.position.z),
                    Size = size,
                    RotationY = Mathf.Repeat(child.eulerAngles.y, 360f),
                });
            }

            return items;
        }

        private static bool TryGetAssetId(
            MapEntityCategory category,
            int configId,
            TbMapBuildingCfg buildingTable,
            TbMapResourceCfg resourceTable,
            TbAssetPathCfg assetTable,
            MapExportResult result,
            out int assetId)
        {
            assetId = 0;

            if (category == MapEntityCategory.Building)
            {
                var config = buildingTable?.Get(configId);
                if (config == null)
                {
                    result.Errors.Add($"找不到 MapBuildingCfg Id={configId}");
                    return false;
                }

                assetId = config.AssetId;
                return true;
            }

            if (category == MapEntityCategory.Decorate)
            {
                // V3：装饰物直接使用 AssetPathCfg.Id 作为导出 Id。
                var asset = assetTable?.Get(configId);
                if (asset == null)
                {
                    result.Errors.Add($"找不到 AssetPathCfg Id={configId}");
                    return false;
                }

                assetId = configId;
                return true;
            }

            if (category == MapEntityCategory.Resource)
            {
                var config = resourceTable?.Get(configId);
                if (config == null)
                {
                    result.Errors.Add($"找不到 MapResourceCfg Id={configId}");
                    return false;
                }

                assetId = config.AssetId;
                return true;
            }

            result.Errors.Add($"未知地图实体类别: {category}");
            return false;
        }

        private static void WriteModule<T>(string folder, string fileName, List<MapItemData> items)
            where T : MapModuleData
        {
            string path = $"{folder}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Items = items;
            EditorUtility.SetDirty(asset);
        }

        private static void WriteBinary(string folder, string fileName, List<MapItemData> items)
        {
            var entries = new List<MapBinaryEntry>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                entries.Add(new MapBinaryEntry
                {
                    InstanceId = item.InstanceId,
                    Id = item.Id,
                    PosX = item.Pos.x,
                    PosZ = item.Pos.y,
                    SizeX = item.Size.x,
                    SizeZ = item.Size.y,
                    RotationY = item.RotationY,
                });
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                                 ?? Directory.GetCurrentDirectory();
            string path = Path.Combine(projectRoot, folder, fileName);
            MapDataBinary.Write(path, entries);
        }

        private static bool EnsureFolder(string folder, string mapName, MapExportResult result)
        {
            if (!AssetDatabase.IsValidFolder("Assets/BundleRes"))
            {
                result.Errors.Add("找不到 Assets/BundleRes 目录");
                return false;
            }

            if (!AssetDatabase.IsValidFolder(MapDataRoot))
                AssetDatabase.CreateFolder("Assets/BundleRes", "MapData");

            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(MapDataRoot, mapName);

            return true;
        }

        private static void MakeAddressable(string assetPath, string groupName, string address, MapExportResult result)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                result.Warnings.Add("Addressables 未初始化，地图资源需要手动加入 Addressables 组");
                return;
            }

            var group = settings.FindGroup(groupName) ?? settings.DefaultGroup;
            if (group == null)
            {
                result.Warnings.Add($"找不到 Addressables 组 {groupName}，也没有默认组");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                result.Warnings.Add($"资源没有 GUID，无法加入 Addressables: {assetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                result.Warnings.Add($"加入 Addressables 失败: {assetPath}");
                return;
            }

            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, false, true);
        }
    }
}
