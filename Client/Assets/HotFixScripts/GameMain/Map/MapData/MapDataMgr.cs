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
    /// Main 场景的地图数据入口：
    ///   - 按 WorldChunkStreaming.sceneName（没有则当前场景名）定位
    ///     Assets/BundleRes/MapData/&lt;场景名&gt;/ 下的四个 ScriptableObject；
    ///   - 通过 Addressables 加载；
    ///   - 创建 MapDataStreaming，由四叉树按玩家位置创建/销毁实体。
    /// </summary>
    public sealed class MapDataMgr : GameMgrBase<MapDataMgr>
    {
        private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();

        private BuildingData _building;
        private DecorateData _decorate;
        private ResourceData _resource;
        private ObstacleData _obstacle;
        private WorldChunkStreaming _chunkStreaming;
        private MapDataStreaming _streaming;

        private int _pending;
        private bool _initialized;
        private bool _disposed;

        public override void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            HideAuthoringRoot();

            _chunkStreaming = Object.FindObjectOfType<WorldChunkStreaming>();

            string mapName = _chunkStreaming != null && !string.IsNullOrEmpty(_chunkStreaming.sceneName)
                ? _chunkStreaming.sceneName
                : SceneManager.GetActiveScene().name;

            string basePath = $"Assets/BundleRes/MapData/{mapName}";
            _pending = 4;

            Load<BuildingData>($"{basePath}/BuildingData.asset",
                data => { _building = data; OnOneDone(); },
                () => OnLoadFailed("BuildingData"));

            Load<DecorateData>($"{basePath}/DecorateData.asset",
                data => { _decorate = data; OnOneDone(); },
                () => OnLoadFailed("DecorateData"));

            Load<ResourceData>($"{basePath}/ResourceData.asset",
                data => { _resource = data; OnOneDone(); },
                () => OnLoadFailed("ResourceData"));

            Load<ObstacleData>($"{basePath}/ObstacleData.asset",
                data => { _obstacle = data; OnOneDone(); },
                () => OnLoadFailed("ObstacleData"));
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

        private void Load<T>(string address, System.Action<T> onSuccess, System.Action onFail) where T : Object
        {
            var locationHandle = FW.ResMgr.LoadResourceLocationsAsync(address, typeof(T));
            locationHandle.Completed += locationOperation =>
            {
                if (_disposed)
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
                    onFail?.Invoke();
                    return;
                }

                var loadHandle = FW.ResMgr.LoadAssetAsync<T>(address);
                loadHandle.Completed += operation =>
                {
                    if (_disposed)
                    {
                        FW.ResMgr.Release(operation);
                        return;
                    }

                    if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null)
                    {
                        _handles.Add(operation);
                        onSuccess?.Invoke(operation.Result);
                    }
                    else
                    {
                        FW.ResMgr.Release(operation);
                        onFail?.Invoke();
                    }
                };
            };
        }

        private void OnLoadFailed(string module)
        {
            if (_disposed)
                return;

            // V2 支持按模块导出，客户端可能只导出了部分模块，缺失的模块跳过即可。
            Debug.Log($"[MapData] 地图数据模块不存在，跳过: {module}");
            OnOneDone();
        }

        private void OnOneDone()
        {
            if (_disposed)
                return;

            _pending--;
            if (_pending > 0)
                return;

            if (_building == null && _decorate == null && _resource == null && _obstacle == null)
            {
                Debug.LogWarning("[MapData] 四个地图数据模块都不存在，本次不创建地图实体");
                ReleaseHandles();
                return;
            }

            var go = new GameObject("MapDataStreaming");
            _streaming = go.AddComponent<MapDataStreaming>();
            _streaming.Initialize(_chunkStreaming, _building, _decorate, _resource, _obstacle);

            Debug.Log("[MapData] 地图数据加载完成，开始按玩家位置流式创建实体");
        }

        public override void Dispose()
        {
            _disposed = true;

            if (_streaming != null)
            {
                Object.Destroy(_streaming.gameObject);
                _streaming = null;
            }

            ReleaseHandles();
            _building = null;
            _decorate = null;
            _resource = null;
            _obstacle = null;
        }

        private void ReleaseHandles()
        {
            if (FW.ResMgr != null)
            {
                for (int i = 0; i < _handles.Count; i++)
                    FW.ResMgr.Release(_handles[i]);
            }

            _handles.Clear();
        }
    }
}
