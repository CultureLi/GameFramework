using System.Collections.Generic;
using Framework;
using GameEntry;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameMain
{
    /// <summary>
    /// 当前地图区域的可建造区域管理器（挂在区域节点下，由 <see cref="MapDataMgr"/> 创建）。
    ///
    /// 等首批地形块加载完成后，按 BuildableAreaData 的矩形条目生成 <see cref="BuildableArea"/>，
    /// 画出绿色区域并建立格子占位。同时创建 <see cref="CityBuildCtrl"/>，
    /// 由主界面「建造」下拉框选择建筑后负责预览与放置。
    /// </summary>
    public sealed class BuildableAreaMgr : MonoBehaviour
    {
        /// <summary>建造 / 移动状态下小格子的材质。</summary>
        public const string GridMaterialPath = "Assets/Arts/Map/MidGard/Material/BuildingGrid.mat";

        [Header("格子")]
        [Tooltip("可建造区域的格子边长（米）。")]
        public float cellSize = 1f;

        [Header("表现")]
        [Tooltip("绿色矩形离地高度（米）。")]
        public float heightOffset = 0.05f;

        [Header("演示")]
        [Tooltip("挂上 CityBuildCtrl，配合主界面「建造」下拉框显示建筑预览并放置建筑。")]
        public bool enableBuildingPlacement = true;

        private WorldChunkStreaming _chunkStreaming;
        private BuildableAreaData _data;
        private readonly List<BuildableArea> _areas = new List<BuildableArea>();
        private bool _built;

        private Material _gridMaterial;
        private AsyncOperationHandle<Material> _gridMaterialHandle;
        private bool _hasGridMaterialHandle;
        private bool _gridVisible;
        private bool _destroyed;

        public IReadOnlyList<BuildableArea> Areas => _areas;

        /// <summary>建造控制器；主界面下拉框选中建筑后由它负责预览和放置。</summary>
        public CityBuildCtrl BuildCtrl { get; private set; }

        public void Initialize(WorldChunkStreaming chunkStreaming, BuildableAreaData data)
        {
            _chunkStreaming = chunkStreaming;
            _data = data;

            LoadGridMaterial();

            // 建造控制器不依赖地块加载，先建出来，界面选中建筑后就能开始预览。
            if (enableBuildingPlacement)
            {
                BuildCtrl = gameObject.AddComponent<CityBuildCtrl>();
                BuildCtrl.Initialize(this);
            }
        }

        private void LoadGridMaterial()
        {
            if (FW.ResMgr == null)
                return;

            var handle = FW.ResMgr.LoadAssetAsync<Material>(GridMaterialPath);
            handle.Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    Debug.LogWarning("[BuildableArea] 格子材质加载失败: " + GridMaterialPath);
                    if (FW.ResMgr != null)
                        FW.ResMgr.Release(operation);
                    return;
                }

                if (_destroyed)
                {
                    if (FW.ResMgr != null)
                        FW.ResMgr.Release(operation);
                    return;
                }

                _gridMaterial = operation.Result;
                _gridMaterialHandle = operation;
                _hasGridMaterialHandle = true;

                for (int i = 0; i < _areas.Count; i++)
                    _areas[i].SetGridMaterial(_gridMaterial);
            };
        }

        private void OnDestroy()
        {
            _destroyed = true;

            if (_hasGridMaterialHandle && FW.ResMgr != null)
                FW.ResMgr.Release(_gridMaterialHandle);

            _hasGridMaterialHandle = false;
        }

        /// <summary>建造 / 移动状态切换时显示或隐藏所有可建造区域的格子。</summary>
        public void SetGridVisible(bool visible)
        {
            _gridVisible = visible;

            for (int i = 0; i < _areas.Count; i++)
                _areas[i].SetGridVisible(visible);
        }

        private void Update()
        {
            if (_built || _data == null)
                return;

            // 和地图实体一样，等地块加载出来再贴地生成区域。
            if (_chunkStreaming != null && _chunkStreaming.LoadedChunkCount <= 0)
                return;

            Build();
        }

        private void Build()
        {
            _built = true;

            for (int i = 0; i < _data.Items.Count; i++)
            {
                var item = _data.Items[i];

                var areaObject = new GameObject($"BuildableArea_{i}");
                areaObject.transform.SetParent(transform, false);
                areaObject.transform.position = new Vector3(
                    item.Pos.x,
                    SampleGroundY(item.Pos) + heightOffset,
                    item.Pos.y);
                areaObject.transform.rotation = Quaternion.Euler(0f, item.RotationY, 0f);

                var area = areaObject.AddComponent<BuildableArea>();
                area.Initialize(item, cellSize);
                area.SetGridMaterial(_gridMaterial);
                area.SetGridVisible(_gridVisible);
                _areas.Add(area);
            }

            Debug.Log($"[BuildableArea] 生成可建造区域 {_areas.Count} 个");
        }

        /// <summary>找出包含该世界坐标点的可建造区域。</summary>
        public bool TryGetAreaAt(Vector3 world, out BuildableArea area)
        {
            for (int i = 0; i < _areas.Count; i++)
            {
                if (_areas[i] != null && _areas[i].Contains(world))
                {
                    area = _areas[i];
                    return true;
                }
            }

            area = null;
            return false;
        }

        private float SampleGroundY(Vector2 position)
        {
            var origin = new Vector3(position.x, 2000f, position.y);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 4000f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point.y;

            return _chunkStreaming != null ? _chunkStreaming.groundY : 0f;
        }
    }
}
