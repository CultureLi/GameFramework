using System.Collections.Generic;
using Framework;
using GameEntry;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GameMain
{
    /// <summary>
    /// Main 场景的地图数据入口，管理 Midgard（内城）和 OpenWorld（世界地图）两个区域。
    ///
    /// 每个区域是 Main 场景里的一个根节点，挂着 <see cref="MapArea"/> 和自己的
    /// <see cref="WorldChunkStreaming"/>。同一时刻只激活一个区域：
    ///   - 激活时按 &lt;区域名&gt; 从地址 Assets/AssetBundles/MapData/&lt;区域名&gt;/ 加载
    ///     Building / Decorate / Resource / Obstacle / BuildableArea 五个模块；
    ///   - 创建一个 MapDataStreaming（可建造区域再额外创建一个 BuildableAreaMgr），
    ///     挂到区域节点下；
    ///   - 切换时销毁上一个区域的流式对象、释放数据句柄，并隐藏该区域节点。
    /// </summary>
    public sealed class MapDataMgr : GameMgrBase<MapDataMgr>
    {
        private sealed class AreaRuntime
        {
            public MapArea Area;
            public string Name;
            public WorldChunkStreaming ChunkStreaming;
            public readonly List<AsyncOperationHandle> Handles = new List<AsyncOperationHandle>();

            public BuildingData Building;
            public DecorateData Decorate;
            public ResourceData Resource;
            public ObstacleData Obstacle;
            public BuildableAreaData BuildableArea;

            public MapDataStreaming Streaming;
            public BuildableAreaMgr BuildableAreaMgr;

            public int Pending;
            public int Generation;
        }

        private readonly Dictionary<string, MapArea> _areas = new Dictionary<string, MapArea>();

        private AreaRuntime _active;
        private bool _initialized;
        private bool _disposed;

        /// <summary>当前显示的区域名；没有激活区域时为 null。</summary>
        public string ActiveAreaName => _active != null ? _active.Name : null;

        /// <summary>当前区域的可建造区域管理器；该区域没有可建造数据时为 null。</summary>
        public BuildableAreaMgr ActiveBuildableArea => _active != null ? _active.BuildableAreaMgr : null;

        /// <summary>当前区域的建造控制器；不能建造时为 null。</summary>
        public CityBuildCtrl ActiveBuildCtrl =>
            ActiveBuildableArea != null ? ActiveBuildableArea.BuildCtrl : null;

        public IEnumerable<string> AreaNames => _areas.Keys;

        public bool HasArea(string areaName)
        {
            return !string.IsNullOrEmpty(areaName) && _areas.ContainsKey(areaName);
        }

        public override void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            HideAuthoringRoot();
            RegisterAreas();

            // 需求：登录后默认进入内城 Midgard。
            string defaultArea = _areas.ContainsKey(MapArea.MidgardName)
                ? MapArea.MidgardName
                : null;

            if (defaultArea == null)
            {
                foreach (var pair in _areas)
                {
                    defaultArea = pair.Key;
                    break;
                }
            }

            if (defaultArea == null)
            {
                Debug.LogWarning("[MapData] Main 场景里没有找到 MapArea 区域节点，本次不加载地图");
                return;
            }

            SwitchArea(defaultArea);
        }

        /// <summary>
        /// 切换到指定区域：隐藏旧区域、激活新区域、加载它的地图数据。
        /// 返回 false 表示场景里没有这个区域节点。
        /// </summary>
        public bool SwitchArea(string areaName)
        {
            if (_disposed || string.IsNullOrEmpty(areaName))
                return false;

            if (!_areas.TryGetValue(areaName, out var area) || area == null)
            {
                Debug.LogWarning($"[MapData] 找不到地图区域: {areaName}");
                return false;
            }

            if (_active != null && _active.Area == area)
                return true;

            DeactivateActive();

            // 先激活节点，让地块流式加载和新数据在同一帧开始工作。
            if (!area.gameObject.activeSelf)
                area.gameObject.SetActive(true);

            var runtime = new AreaRuntime
            {
                Area = area,
                Name = areaName,
                ChunkStreaming = area.ChunkStreaming,
            };

            _active = runtime;
            LoadArea(runtime);

            // 单机模式切区域时同步换一份导航数据，并把玩家挪到新地图的出生点。
            PathfindingMgr.I?.OnMapAreaChanged(areaName);

            var camera = WorldCameraController.I;
            if (camera != null)
            {
                camera.mapMinXZ = area.mapMinXZ;
                camera.mapMaxXZ = area.mapMaxXZ;
                camera.MoveTo(new Vector3(area.cameraFocus.x, 0f, area.cameraFocus.y));
            }

            Debug.Log($"[MapData] 切换到地图区域: {areaName}");
            return true;
        }

        /// <summary>在 Midgard / OpenWorld 之间来回切换，供主界面按钮使用。</summary>
        public string SwitchNextArea()
        {
            string next = ActiveAreaName == MapArea.MidgardName
                ? MapArea.OpenWorldName
                : MapArea.MidgardName;

            if (!HasArea(next))
            {
                Debug.LogWarning($"[MapData] 场景里没有目标区域节点: {next}");
                return null;
            }

            return SwitchArea(next) ? ActiveAreaName : null;
        }

        // ---------------------------------------------------------------- 区域注册

        private void RegisterAreas()
        {
            var found = Object.FindObjectsOfType<MapArea>(true);
            for (int i = 0; i < found.Length; i++)
            {
                var area = found[i];
                if (area == null)
                    continue;

                string name = area.AreaName;
                if (string.IsNullOrEmpty(name))
                    continue;

                _areas[name] = area;
            }

            // 兼容没有 MapArea 的旧场景：直接给每个 WorldChunkStreaming 补一个区域节点。
            if (_areas.Count > 0)
                return;

            var streamings = Object.FindObjectsOfType<WorldChunkStreaming>(true);
            for (int i = 0; i < streamings.Length; i++)
            {
                var streaming = streamings[i];
                if (streaming == null)
                    continue;

                var area = streaming.GetComponentInParent<MapArea>();
                if (area == null)
                {
                    area = streaming.gameObject.AddComponent<MapArea>();
                    area.areaName = streaming.sceneName;
                    area.chunkStreaming = streaming;
                }

                _areas[area.AreaName] = area;
            }
        }

        // ---------------------------------------------------------------- 数据加载

        private void LoadArea(AreaRuntime runtime)
        {
            int generation = ++runtime.Generation;
            string basePath = $"Assets/AssetBundles/MapData/{runtime.Name}";

            runtime.Pending = 5;

            LoadModule<BuildingData>(runtime, generation, $"{basePath}/BuildingData.asset",
                data => runtime.Building = data);

            LoadModule<DecorateData>(runtime, generation, $"{basePath}/DecorateData.asset",
                data => runtime.Decorate = data);

            LoadModule<ResourceData>(runtime, generation, $"{basePath}/ResourceData.asset",
                data => runtime.Resource = data);

            LoadModule<ObstacleData>(runtime, generation, $"{basePath}/ObstacleData.asset",
                data => runtime.Obstacle = data);

            LoadModule<BuildableAreaData>(runtime, generation, $"{basePath}/BuildableAreaData.asset",
                data => runtime.BuildableArea = data);
        }

        private void LoadModule<T>(AreaRuntime runtime, int generation, string address,
            System.Action<T> onSuccess) where T : Object
        {
            var locationHandle = FW.ResMgr.LoadResourceLocationsAsync(address, typeof(T));
            locationHandle.Completed += locationOperation =>
            {
                if (_disposed || !IsCurrent(runtime, generation))
                {
                    FW.ResMgr.Release(locationOperation);
                    return;
                }

                bool exists = locationOperation.Status == AsyncOperationStatus.Succeeded
                              && locationOperation.Result != null
                              && locationOperation.Result.Count > 0;

                FW.ResMgr.Release(locationOperation);

                if (!exists)
                {
                    // 支持按模块导出，缺失的模块跳过即可。
                    Debug.Log($"[MapData] 地图数据模块不存在，跳过: {address}");
                    OnOneDone(runtime, generation);
                    return;
                }

                var loadHandle = FW.ResMgr.LoadAssetAsync<T>(address);
                loadHandle.Completed += operation =>
                {
                    if (_disposed || !IsCurrent(runtime, generation))
                    {
                        FW.ResMgr.Release(operation);
                        return;
                    }

                    if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null)
                    {
                        runtime.Handles.Add(operation);
                        onSuccess(operation.Result);
                    }
                    else
                    {
                        FW.ResMgr.Release(operation);
                        Debug.Log($"[MapData] 地图数据加载失败，跳过: {address}");
                    }

                    OnOneDone(runtime, generation);
                };
            };
        }

        private bool IsCurrent(AreaRuntime runtime, int generation)
        {
            return !_disposed && _active == runtime && runtime.Generation == generation;
        }

        private void OnOneDone(AreaRuntime runtime, int generation)
        {
            if (!IsCurrent(runtime, generation))
                return;

            runtime.Pending--;
            if (runtime.Pending > 0)
                return;

            if (runtime.Building == null && runtime.Decorate == null && runtime.Resource == null &&
                runtime.Obstacle == null && runtime.BuildableArea == null)
            {
                Debug.LogWarning($"[MapData] {runtime.Name} 的五个地图数据模块都不存在，本次不创建地图实体");
                return;
            }

            var areaTransform = runtime.Area != null ? runtime.Area.transform : null;

            var streamingObject = new GameObject("MapDataStreaming");
            if (areaTransform != null)
                streamingObject.transform.SetParent(areaTransform, false);

            runtime.Streaming = streamingObject.AddComponent<MapDataStreaming>();
            runtime.Streaming.Initialize(runtime.ChunkStreaming,
                runtime.Building, runtime.Decorate, runtime.Resource, runtime.Obstacle);

            if (runtime.BuildableArea != null && runtime.BuildableArea.Items.Count > 0)
            {
                var buildableObject = new GameObject("BuildableAreaMgr");
                if (areaTransform != null)
                    buildableObject.transform.SetParent(areaTransform, false);

                runtime.BuildableAreaMgr = buildableObject.AddComponent<BuildableAreaMgr>();
                runtime.BuildableAreaMgr.Initialize(runtime.ChunkStreaming, runtime.BuildableArea);
            }

            PathfindingMgr.I?.SetLocalBuildingData(runtime.Building);

            Debug.Log($"[MapData] {runtime.Name} 地图数据加载完成，开始流式创建实体");
        }

        // ---------------------------------------------------------------- 卸载

        private void DeactivateActive()
        {
            var runtime = _active;
            if (runtime == null)
                return;

            _active = null;

            // 让还在路上的异步回调失效。
            runtime.Generation++;

            if (runtime.Streaming != null)
            {
                Object.Destroy(runtime.Streaming.gameObject);
                runtime.Streaming = null;
            }

            if (runtime.BuildableAreaMgr != null)
            {
                Object.Destroy(runtime.BuildableAreaMgr.gameObject);
                runtime.BuildableAreaMgr = null;
            }

            ReleaseHandles(runtime);
            runtime.Building = null;
            runtime.Decorate = null;
            runtime.Resource = null;
            runtime.Obstacle = null;
            runtime.BuildableArea = null;

            if (runtime.Area != null && runtime.Area.gameObject.activeSelf)
                runtime.Area.gameObject.SetActive(false);
        }

        private static void ReleaseHandles(AreaRuntime runtime)
        {
            if (FW.ResMgr == null)
            {
                runtime.Handles.Clear();
                return;
            }

            for (int i = 0; i < runtime.Handles.Count; i++)
                FW.ResMgr.Release(runtime.Handles[i]);

            runtime.Handles.Clear();
        }

        public override void Dispose()
        {
            _disposed = true;
            DeactivateActive();
            _areas.Clear();
        }

        /// <summary>
        /// 如果作者场景和运行场景是同一个，隐藏带 MapAuthoringRoot 标记的 DataEditor 根节点，
        /// 避免场景里的预览对象和按 MapData 创建的实体同时出现。
        /// </summary>
        private static void HideAuthoringRoot()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null)
                    continue;

                if (root.GetComponent<MapAuthoringRoot>() != null)
                    root.SetActive(false);
            }
        }
    }
}
