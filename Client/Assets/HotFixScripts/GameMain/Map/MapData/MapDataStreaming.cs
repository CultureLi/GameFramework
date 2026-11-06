using System.Collections.Generic;
using cfg;
using Framework;
using GameEntry;
using NativeTrees;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace GameMain
{
    /// <summary>
    /// 根据相机在地面上的投射框，用 NativeTrees 的 NativeQuadtree 管理导出数据对应的 GameObject：
    /// 在框内（AABB 有重叠）的实体创建，离开框后释放。
    /// 高度不在导出数据里，实例化时从地形向下射线采样。
    /// </summary>
    public sealed class MapDataStreaming : MonoBehaviour
    {
        [Header("刷新")]
        [Tooltip("重新计算加载集合的间隔（秒）。")]
        public float refreshInterval = 0.25f;

        [Header("贴地")]
        [Tooltip("从地形高度再往上偏移多少米。")]
        public float heightOffset = 0f;
        public LayerMask groundMask = ~0;

        private WorldChunkStreaming _chunkStreaming;
        private BuildingData _buildingData;
        private DecorateData _decorateData;
        private ResourceData _resourceData;

        private readonly List<MapRuntimeItem> _items = new List<MapRuntimeItem>();
        private readonly List<int> _toUnload = new List<int>();
        private readonly Dictionary<int, GameObject> _loaded = new Dictionary<int, GameObject>();
        private readonly HashSet<int> _loading = new HashSet<int>();

        private NativeQuadtree<int> _quadtree;
        private NativeList<int> _query;
        private bool _hasQuadtree;

        private Transform _mapRoot;
        private AABB2D _currentFootprint;
        private bool _hasFootprint;
        private float _nextRefresh;
        private bool _built;
        private bool _destroyed;

        public void Initialize(WorldChunkStreaming chunkStreaming, BuildingData buildingData,
            DecorateData decorateData, ResourceData resourceData)
        {
            _chunkStreaming = chunkStreaming;
            _buildingData = buildingData;
            _decorateData = decorateData;
            _resourceData = resourceData;
        }

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            if (_built)
                return;

            _built = true;

            var rootObject = new GameObject("MapEntities");
            rootObject.transform.SetParent(transform, false);
            _mapRoot = rootObject.transform;

            if (FW.CfgMgr == null)
            {
                Debug.LogWarning("[MapData] 配置管理器未初始化，地图实体不会创建");
                return;
            }

            try
            {
                var assetTable = FW.CfgMgr.GetTable<TbAssetPathCfg>();
                var buildingTable = FW.CfgMgr.GetTable<TbMapBuildingCfg>();
                var resourceTable = FW.CfgMgr.GetTable<TbMapResourceCfg>();

                AddBuildingItems(buildingTable, assetTable);
                AddDecorateItems(assetTable);
                AddResourceItems(resourceTable, assetTable);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[MapData] 读取地图配置失败: " + ex.Message);
                return;
            }

            if (_items.Count > 0)
                BuildQuadtree();

            Debug.Log($"[MapData] 地图实体数据加载完成，建筑/装饰/资源共 {_items.Count} 个");
        }

        private void BuildQuadtree()
        {
            float2 min = new float2(float.MaxValue, float.MaxValue);
            float2 max = new float2(float.MinValue, float.MinValue);

            for (int i = 0; i < _items.Count; i++)
            {
                AABB2D bounds = GetBounds(_items[i]);
                min = math.min(min, bounds.min);
                max = math.max(max, bounds.max);
            }

            // 稍微外扩，避免数据正好落在根节点边界上。
            min -= new float2(0.01f, 0.01f);
            max += new float2(0.01f, 0.01f);

            _quadtree = new NativeQuadtree<int>(new AABB2D(min, max), Allocator.Persistent);
            _hasQuadtree = true;
            _query = new NativeList<int>(Mathf.Max(16, _items.Count), Allocator.Persistent);

            for (int i = 0; i < _items.Count; i++)
                _quadtree.Insert(_items[i].Index, GetBounds(_items[i]));
        }

        private static AABB2D GetBounds(MapRuntimeItem item)
        {
            float halfX = Mathf.Max(0.01f, item.Size.x) * 0.5f;
            float halfZ = Mathf.Max(0.01f, item.Size.y) * 0.5f;
            return new AABB2D(
                new float2(item.Pos.x - halfX, item.Pos.y - halfZ),
                new float2(item.Pos.x + halfX, item.Pos.y + halfZ));
        }

        private void AddBuildingItems(TbMapBuildingCfg table, TbAssetPathCfg assetTable)
        {
            if (_buildingData == null || table == null)
                return;

            for (int i = 0; i < _buildingData.Items.Count; i++)
            {
                var data = _buildingData.Items[i];
                var config = table.Get(data.Id);
                if (config == null)
                {
                    Debug.LogWarning($"[MapData] 找不到建筑配置 Id={data.Id}");
                    continue;
                }

                AddItem(data, MapEntityCategory.Building, config.AssetId, assetTable,
                    MapBuildingTypeHelper.IsGate(config, assetTable));
            }
        }

        /// <summary>
        /// V3：装饰物不再经过 MapDecorateCfg，导出数据里的 Id 直接就是 AssetPathCfg.Id。
        /// </summary>
        private void AddDecorateItems(TbAssetPathCfg assetTable)
        {
            if (_decorateData == null || assetTable == null)
                return;

            for (int i = 0; i < _decorateData.Items.Count; i++)
            {
                var data = _decorateData.Items[i];
                var asset = assetTable.Get(data.Id);
                if (asset == null || string.IsNullOrEmpty(asset.AssetPath))
                {
                    Debug.LogWarning($"[MapData] 找不到装饰资源 AssetPathCfg Id={data.Id}");
                    continue;
                }

                AddItem(data, MapEntityCategory.Decorate, data.Id, assetTable);
            }
        }
        private void AddResourceItems(TbMapResourceCfg table, TbAssetPathCfg assetTable)
        {
            if (_resourceData == null || table == null)
                return;

            for (int i = 0; i < _resourceData.Items.Count; i++)
            {
                var data = _resourceData.Items[i];
                var config = table.Get(data.Id);
                if (config == null)
                {
                    Debug.LogWarning($"[MapData] 找不到资源配置 Id={data.Id}");
                    continue;
                }

                AddItem(data, MapEntityCategory.Resource, config.AssetId, assetTable);
            }
        }

        private void AddItem(MapItemData data, MapEntityCategory category, int assetId,
            TbAssetPathCfg assetTable, bool isGate = false)
        {
            var asset = assetTable?.Get(assetId);
            if (asset == null || string.IsNullOrEmpty(asset.AssetPath))
            {
                Debug.LogWarning($"[MapData] AssetPathCfg 缺失 AssetId={assetId}，跳过配置 Id={data.Id}");
                return;
            }

            var size = data.Size;
            if (size.x < 0.01f) size.x = 0.01f;
            if (size.y < 0.01f) size.y = 0.01f;

            _items.Add(new MapRuntimeItem
            {
                Index = _items.Count,
                ConfigId = data.Id,
                InstanceId = data.InstanceId,
                Category = category,
                Pos = data.Pos,
                Size = size,
                RotationY = data.RotationY,
                AssetPath = asset.AssetPath,
                IsGate = isGate,
            });
        }

        private void Update()
        {
            if (!_built || _destroyed || !_hasQuadtree)
                return;

            // 需求：等地块流式加载初始化出地块后再开始创建地图实体，
            // 这样射线能打到地形，实体高度才是对的。
            if (_chunkStreaming != null && _chunkStreaming.LoadedChunkCount <= 0)
                return;

            if (!TryGetFootprintRect(out Rect footprint))
            {
                _hasFootprint = false;
                return;
            }

            _currentFootprint = ToAabb(footprint);
            _hasFootprint = true;

            if (Time.unscaledTime < _nextRefresh)
                return;

            _nextRefresh = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
            UpdateStreaming(_currentFootprint);
        }

        /// <summary>
        /// 复用 WorldChunkStreaming / WorldCameraController 已经算好的相机地面投射框。
        /// </summary>
        private bool TryGetFootprintRect(out Rect rect)
        {
            if (_chunkStreaming != null && _chunkStreaming.TryGetCameraFootprintRect(out rect))
                return true;

            var cameraController = WorldCameraController.I;
            if (cameraController != null && cameraController.TryGetFootprintRect(out rect))
                return true;

            rect = default;
            return false;
        }

        private static AABB2D ToAabb(Rect rect)
        {
            return new AABB2D(
                new float2(rect.xMin, rect.yMin),
                new float2(rect.xMax, rect.yMax));
        }

        private void UpdateStreaming(AABB2D footprint)
        {
            _query.Clear();
            _quadtree.RangeAABB(footprint, _query);

            // RangeAABB 对跨越多个叶子的对象可能返回多次，_loaded/_loading 会挡住重复处理。
            for (int i = 0; i < _query.Length; i++)
            {
                var item = GetItem(_query[i]);
                if (item == null)
                    continue;

                if (_loaded.ContainsKey(item.Index) || _loading.Contains(item.Index))
                    continue;

                if (!Overlaps(item, footprint))
                    continue;

                StartLoad(item);
            }

            _toUnload.Clear();
            foreach (var pair in _loaded)
            {
                var item = GetItem(pair.Key);
                if (item == null || !Overlaps(item, footprint))
                    _toUnload.Add(pair.Key);
            }

            for (int i = 0; i < _toUnload.Count; i++)
            {
                int index = _toUnload[i];
                if (!_loaded.TryGetValue(index, out var go))
                    continue;

                _loaded.Remove(index);
                Release(go);
            }
        }

        private MapRuntimeItem GetItem(int index)
        {
            return index >= 0 && index < _items.Count ? _items[index] : null;
        }

        private static bool Overlaps(MapRuntimeItem item, AABB2D footprint)
        {
            return GetBounds(item).Overlaps(footprint);
        }

        private void StartLoad(MapRuntimeItem item)
        {
            _loading.Add(item.Index);

            try
            {
                var handle = FW.ResMgr.InstantiateAsync(item.AssetPath);
                handle.Completed += operation => OnLoaded(item, operation);
            }
            catch (System.Exception ex)
            {
                _loading.Remove(item.Index);
                Debug.LogError($"[MapData] 实例化失败 {item.AssetPath}: {ex.Message}");
            }
        }

        private void OnLoaded(MapRuntimeItem item, AsyncOperationHandle<GameObject> operation)
        {
            _loading.Remove(item.Index);

            if (_destroyed)
            {
                if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null)
                    Release(operation.Result);
                else if (FW.ResMgr != null)
                    FW.ResMgr.Release(operation);
                return;
            }

            if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
            {
                if (FW.ResMgr != null)
                    FW.ResMgr.Release(operation);
                Debug.LogWarning($"[MapData] 地图实体加载失败: {item.AssetPath}");
                return;
            }

            var go = operation.Result;

            // 异步加载期间相机可能已经移开，或者同一个实体已经被别的回调创建。
            if (_loaded.ContainsKey(item.Index) || !_hasFootprint || !Overlaps(item, _currentFootprint))
            {
                Release(go);
                return;
            }

            go.transform.SetParent(_mapRoot, false);

            // 编辑器里可能对 prefab 做了 XZ 缩放（例如墙体按长度拉伸）。
            // 导出数据只有 Size 和 RotationY，这里在 0/90/180/270 度情况下根据 AABB 反推缩放。
            ApplySizeScale(go, item);

            // 不同 prefab 的 pivot 不一定在底部。用 collider/renderer 的最低点贴地，
            // Wall_1 这类中心 pivot 的墙体也能正确站在地面上。
            float groundOffset = GetPrefabGroundOffset(go);
            go.transform.position = new Vector3(
                item.Pos.x,
                SampleGroundY(item.Pos) + groundOffset,
                item.Pos.y);
            go.transform.rotation = Quaternion.Euler(0f, item.RotationY, 0f);

            if (item.IsGate)
            {
                var gate = go.GetComponent<GateBuilding>();
                if (gate == null)
                    gate = go.AddComponent<GateBuilding>();

                gate.Initialize(item.InstanceId, item.ConfigId);
            }

            _loaded[item.Index] = go;
        }

        private float SampleGroundY(Vector2 position)
        {
            var origin = new Vector3(position.x, 2000f, position.y);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 4000f, groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.point.y + heightOffset;
            }

            // 地形块还没加载到该点时退回地面高度，至少不会把实体放到 2000 米高空。
            return (_chunkStreaming != null ? _chunkStreaming.groundY : 0f) + heightOffset;
        }

        private static float GetPrefabGroundOffset(GameObject go)
        {
            if (!TryGetWorldBounds(go, out var bounds))
                return 0f;

            return go.transform.position.y - bounds.min.y;
        }

        private static void ApplySizeScale(GameObject go, MapRuntimeItem item)
        {
            if (item.Size.x <= 0.01f || item.Size.y <= 0.01f)
                return;

            if (!TryGetWorldBounds(go, out var bounds))
                return;

            if (bounds.size.x <= 0.0001f || bounds.size.z <= 0.0001f)
                return;

            float rotation = Mathf.Repeat(item.RotationY, 360f);

            // 只有接近 0/90/180/270 时，AABB 才能和 prefab 本地轴一一对应。
            // 任意角度旋转时 AABB 已经混入旋转后的投影，无法反推缩放，保持 prefab 默认缩放。
            float toCardinal = Mathf.Min(
                Mathf.Abs(Mathf.DeltaAngle(rotation, 0f)),
                Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(rotation, 90f)),
                    Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(rotation, 180f)),
                        Mathf.Abs(Mathf.DeltaAngle(rotation, 270f)))));
            if (toCardinal > 5f)
                return;

            bool alignedZ = Mathf.Abs(Mathf.DeltaAngle(rotation, 90f)) <= 5f ||
                            Mathf.Abs(Mathf.DeltaAngle(rotation, 270f)) <= 5f;

            var scale = go.transform.localScale;

            if (!alignedZ)
            {
                scale.x *= item.Size.x / bounds.size.x;
                scale.z *= item.Size.y / bounds.size.z;
            }
            else
            {
                // 绕 Y 旋转 90/270：prefab 本地 X 对应世界 Z，本地 Z 对应世界 X。
                scale.x *= item.Size.y / bounds.size.x;
                scale.z *= item.Size.x / bounds.size.z;
            }

            go.transform.localScale = scale;
        }

        private static bool TryGetWorldBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            var colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (!hasBounds)
            {
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    if (renderer == null)
                        continue;

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return hasBounds;
        }

        private static void Release(GameObject go)
        {
            if (go == null)
                return;

            if (FW.ResMgr == null || !FW.ResMgr.ReleaseInstance(go))
                Object.Destroy(go);
        }

        private void DisposeQuadtree()
        {
            if (_query.IsCreated)
                _query.Dispose();

            if (_hasQuadtree)
            {
                _quadtree.Dispose();
                _hasQuadtree = false;
            }
        }

        private void OnDestroy()
        {
            _destroyed = true;
            DisposeQuadtree();

            if (FrameworkMgr.ShutdownType == EShutdownType.Shutdown)
            {
                _loaded.Clear();
                _loading.Clear();
                return;
            }

            foreach (var pair in _loaded)
                Release(pair.Value);

            _loaded.Clear();
            _loading.Clear();
        }
    }
}
