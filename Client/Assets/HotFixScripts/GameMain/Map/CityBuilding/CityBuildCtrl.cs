using System.Collections.Generic;
using cfg;
using Framework;
using GameEntry;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameMain
{
    /// <summary>
    /// 内城建造 / 移动控制器。
    ///
    /// 建造：主界面「建造」下拉框选择 MidgardBuildingCfg 后进入建造模式，
    /// 鼠标悬浮显示预制体预览，绿色可放置、红色越界/重叠，左键放置。
    ///
    /// 移动：未在建造模式时，鼠标左键在已有建筑上长按 2 秒进入移动状态，
    /// 移动时同样按格子吸附并显示绿/红预览，再次左键放置，Esc / 右键取消。
    ///
    /// 建造或移动状态下，可建造区域会显示小格子（BuildableArea.GridOverlay）。
    /// </summary>
    public sealed class CityBuildCtrl : MonoBehaviour
    {
        /// <summary>可放置时的预览材质。</summary>
        public const string PreviewValidMaterialPath =
            "Assets/Arts/Map/MidGard/Material/BuildingPreview_Valid.mat";

        /// <summary>不可放置时的预览材质。</summary>
        public const string PreviewInvalidMaterialPath =
            "Assets/Arts/Map/MidGard/Material/BuildingPreview_Invalid.mat";

        /// <summary>长按进入移动状态的时长（秒）。</summary>
        private const float MovePressDuration = 2f;

        /// <summary>长按期间允许的鼠标移动像素，超过则认为是在拖相机。</summary>
        private const float MovePressMoveThresholdPixels = 10f;

        /// <summary>当前场景里的建造控制器；没有建造区域时为 null。</summary>
        public static CityBuildCtrl Active { get; private set; }

        /// <summary>建造或移动模式：寻路等左键逻辑据此让出输入。</summary>
        public static bool IsEditing => Active != null && (Active.HasSelection || Active.IsMoving);

        /// <summary>兼容旧命名。</summary>
        public static bool IsBuildMode => IsEditing;

        private BuildableAreaMgr _mgr;
        private MidgardBuildingCfg _selected;

        private int _footprintX = 1;
        private int _footprintZ = 1;
        private int _nextBuildingId = 1;

        private GameObject _ghost;
        private float _ghostGroundOffset;
        private bool _ghostHasMaterial;
        private bool _ghostGreen;
        private bool _ghostVisible;

        private GameObject _prefab;
        /// <summary>
        /// 按资源路径缓存的建筑预制体句柄。已放置的建筑仍然依赖这些资源，
        /// 所以切换选择时不能释放，统一到控制器销毁（切换地图区域）时再释放。
        /// </summary>
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _prefabHandles =
            new Dictionary<string, AsyncOperationHandle<GameObject>>();
        private int _selectionVersion;

        private Material _validMaterial;
        private Material _invalidMaterial;
        private AsyncOperationHandle<Material> _validHandle;
        private AsyncOperationHandle<Material> _invalidHandle;
        private bool _hasValidHandle;
        private bool _hasInvalidHandle;

        private BuildableArea _targetArea;
        private int _targetCellX;
        private int _targetCellZ;
        private bool _targetValid;
        private bool _destroyed;

        // ---- 长按移动
        private CityBuilding _pressBuilding;
        private float _pressStartTime;
        private Vector2 _pressScreenPosition;

        private CityBuilding _moving;
        private BuildableArea _moveArea;
        private BuildableArea _moveTargetArea;
        private int _moveOldCellX;
        private int _moveOldCellZ;
        private int _moveFootprintX;
        private int _moveFootprintZ;
        private int _moveTargetCellX;
        private int _moveTargetCellZ;
        private bool _moveValid;
        private Vector3 _moveOriginalPosition;
        private Quaternion _moveOriginalRotation;
        private float _moveYOffset;
        private Renderer[] _moveRenderers;
        private Material[][] _moveOriginalMaterials;
        private Collider[] _moveColliders;
        private bool[] _moveColliderEnabled;
        private bool _moveMaterialApplied;
        private bool _moveGreen;

        /// <summary>已选择建筑（预制体可能还在加载）。</summary>
        public bool HasSelection => _selected != null;

        /// <summary>正在移动一座已有建筑。</summary>
        public bool IsMoving => _moving != null;

        public MidgardBuildingCfg SelectedBuilding => _selected;

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        private void OnDestroy()
        {
            _destroyed = true;
            ClearSelection();
            CancelPress();

            _moving = null;
            _moveArea = null;
            _moveTargetArea = null;

            if (FW.ResMgr == null)
                return;

            // 已放置建筑依赖的建筑资源随控制器一起释放。
            foreach (var pair in _prefabHandles)
                FW.ResMgr.Release(pair.Value);
            _prefabHandles.Clear();

            if (_hasValidHandle)
                FW.ResMgr.Release(_validHandle);
            if (_hasInvalidHandle)
                FW.ResMgr.Release(_invalidHandle);
        }

        public void Initialize(BuildableAreaMgr mgr)
        {
            _mgr = mgr;

            if (FW.ResMgr == null)
                return;

            // 材质异步加载：Initialize 可能运行在 Addressables 的完成回调里，
            // 这里不做同步 WaitForCompletion，避免重入资源系统。
            var valid = FW.ResMgr.LoadAssetAsync<Material>(PreviewValidMaterialPath);
            valid.Completed += operation =>
            {
                if (_destroyed)
                {
                    if (FW.ResMgr != null)
                        FW.ResMgr.Release(operation);
                    return;
                }

                if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null)
                {
                    _validMaterial = operation.Result;
                    _validHandle = operation;
                    _hasValidHandle = true;
                }
                else if (FW.ResMgr != null)
                {
                    Debug.LogWarning("[CityBuild] 预览材质加载失败: " + PreviewValidMaterialPath);
                    FW.ResMgr.Release(operation);
                }
            };

            var invalid = FW.ResMgr.LoadAssetAsync<Material>(PreviewInvalidMaterialPath);
            invalid.Completed += operation =>
            {
                if (_destroyed)
                {
                    if (FW.ResMgr != null)
                        FW.ResMgr.Release(operation);
                    return;
                }

                if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null)
                {
                    _invalidMaterial = operation.Result;
                    _invalidHandle = operation;
                    _hasInvalidHandle = true;
                }
                else if (FW.ResMgr != null)
                {
                    Debug.LogWarning("[CityBuild] 预览材质加载失败: " + PreviewInvalidMaterialPath);
                    FW.ResMgr.Release(operation);
                }
            };
        }

        // ---------------------------------------------------------------- 选择 / 取消

        /// <summary>选择要建造的建筑；传 null 等同于取消建造模式。</summary>
        public void SelectBuilding(MidgardBuildingCfg config)
        {
            // 移动中重新选择建筑时，先取消当前位置的移动。
            if (_moving != null)
                CancelMove();

            if (config == null)
            {
                ClearSelection();
                return;
            }

            ClearSelection();

            _selected = config;
            int version = ++_selectionVersion;

            _footprintX = Mathf.Max(1, Mathf.RoundToInt(config.Size.X));
            _footprintZ = Mathf.Max(1, Mathf.RoundToInt(config.Size.Y));

            if (string.IsNullOrEmpty(config.AssetPath))
            {
                Debug.LogError($"[CityBuild] MidgardBuildingCfg Id={config.Id} 没有配置 AssetPath");
                _selected = null;
                _mgr?.SetGridVisible(false);
                return;
            }

            _mgr?.SetGridVisible(true);

            if (_prefabHandles.TryGetValue(config.AssetPath, out var cached) &&
                cached.Status == AsyncOperationStatus.Succeeded && cached.Result != null)
            {
                _prefab = cached.Result;
                CreateGhost();
                return;
            }

            var handle = FW.ResMgr.LoadAssetAsync<GameObject>(config.AssetPath);
            handle.Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    Debug.LogError($"[CityBuild] 建筑预制体加载失败: {config.AssetPath}");
                    if (FW.ResMgr != null)
                        FW.ResMgr.Release(operation);

                    if (version == _selectionVersion)
                    {
                        _selected = null;
                        _prefab = null;
                        _mgr?.SetGridVisible(false);
                    }
                    return;
                }

                // 加载期间用户可能已经换了建筑或切了地图。
                if (version != _selectionVersion || _selected != config || !isActiveAndEnabled)
                {
                    FW.ResMgr.Release(operation);
                    return;
                }

                if (_prefabHandles.TryGetValue(config.AssetPath, out var existing) && existing.IsValid())
                {
                    // 同一预制体已经有缓存，释放这次多余的加载。
                    FW.ResMgr.Release(operation);
                    _prefab = existing.Result;
                }
                else
                {
                    _prefabHandles[config.AssetPath] = operation;
                    _prefab = operation.Result;
                }

                CreateGhost();
            };
        }

        public void ClearSelection()
        {
            _selectionVersion++;
            _selected = null;
            _prefab = null;
            _targetArea = null;
            _targetValid = false;

            DestroyGhost();
            _mgr?.SetGridVisible(IsMoving);
        }

        // ---------------------------------------------------------------- 预览

        private void CreateGhost()
        {
            DestroyGhost();

            if (_prefab == null)
                return;

            _ghost = Object.Instantiate(_prefab);
            _ghost.name = "__BuildingPreview";

            // 预览对象只要渲染：关掉碰撞体避免挡住地面射线，关掉脚本避免执行预制体逻辑。
            var colliders = _ghost.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            var behaviours = _ghost.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
                behaviours[i].enabled = false;

            _ghost.SetActive(true);

            // 预制体的 pivot 不一定在底部，记下当前摆放高度到包围盒最低点的差值。
            _ghostGroundOffset = 0f;
            if (TryGetRendererBounds(_ghost, out Bounds bounds))
                _ghostGroundOffset = _ghost.transform.position.y - bounds.min.y;

            _ghostHasMaterial = false;
            _ghostGreen = false;
            _ghostVisible = false;
            _ghost.SetActive(false);
        }

        private void DestroyGhost()
        {
            if (_ghost != null)
                Object.Destroy(_ghost);

            _ghost = null;
            _ghostHasMaterial = false;
            _ghostVisible = false;
        }

        private void SetGhostVisible(bool visible)
        {
            if (_ghost == null || _ghostVisible == visible)
                return;

            _ghostVisible = visible;
            _ghost.SetActive(visible);
        }

        private void ApplyGhostMaterial(bool valid)
        {
            if (_ghost == null)
                return;

            if (_ghostHasMaterial && _ghostGreen == valid)
                return;

            var material = valid ? _validMaterial : _invalidMaterial;
            if (material == null)
                return;

            var renderers = _ghost.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                int materialCount = renderer.sharedMaterials.Length;
                if (materialCount <= 0)
                    materialCount = 1;

                var materials = new Material[materialCount];
                for (int j = 0; j < materialCount; j++)
                    materials[j] = material;

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            _ghostHasMaterial = true;
            _ghostGreen = valid;
        }

        // ---------------------------------------------------------------- 主循环

        private void Update()
        {
            if (_moving != null)
            {
                UpdateMove();
                return;
            }

            if (_selected != null)
            {
                UpdateBuild();
                return;
            }

            UpdateLongPress();
        }

        // ---------------------------------------------------------------- 建造模式

        private void UpdateBuild()
        {
            // 右键 / Esc 退出建造模式，左键留给放置建筑。
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ClearSelection();
                return;
            }

            if (_prefab == null || _ghost == null)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                SetGhostVisible(false);
                _targetValid = false;
                return;
            }

            if (!TryGetGroundPoint(out Vector3 groundPoint))
            {
                SetGhostVisible(false);
                _targetValid = false;
                return;
            }

            UpdateGhostAt(groundPoint);
            SetGhostVisible(true);

            if (Input.GetMouseButtonDown(0))
                TryPlace();
        }

        private void UpdateGhostAt(Vector3 groundPoint)
        {
            float cellSize = _mgr != null ? _mgr.cellSize : 1f;

            BuildableArea area = null;
            int centerX = 0;
            int centerZ = 0;
            bool inArea = false;
            if (_mgr != null && _mgr.TryGetAreaAt(groundPoint, out area))
                inArea = area.TryWorldToCell(groundPoint, out centerX, out centerZ);

            Vector3 center;
            Quaternion rotation;
            float groundY;

            if (inArea)
            {
                _targetCellX = centerX - _footprintX / 2;
                _targetCellZ = centerZ - _footprintZ / 2;
                center = area.GetFootprintCenterWorld(_targetCellX, _targetCellZ, _footprintX, _footprintZ);
                rotation = area.transform.rotation;
                groundY = area.transform.position.y;
            }
            else
            {
                area = null;
                _targetCellX = Mathf.FloorToInt(groundPoint.x / cellSize) - _footprintX / 2;
                _targetCellZ = Mathf.FloorToInt(groundPoint.z / cellSize) - _footprintZ / 2;
                center = new Vector3(
                    (_targetCellX + _footprintX * 0.5f) * cellSize,
                    groundPoint.y,
                    (_targetCellZ + _footprintZ * 0.5f) * cellSize);
                rotation = Quaternion.identity;
                groundY = groundPoint.y;
            }

            _targetArea = area;
            _targetValid = area != null &&
                           area.IsCellAvailable(_targetCellX, _targetCellZ, _footprintX, _footprintZ, 0);

            _ghost.transform.rotation = rotation;
            _ghost.transform.position = new Vector3(center.x, groundY + _ghostGroundOffset, center.z);

            ApplyGhostMaterial(_targetValid);
        }

        private void TryPlace()
        {
            if (!_targetValid || _targetArea == null || _prefab == null || _ghost == null)
                return;

            if (!_targetArea.IsCellAvailable(_targetCellX, _targetCellZ, _footprintX, _footprintZ, 0))
                return;

            var go = Object.Instantiate(_prefab);
            go.name = (_selected.Name ?? "Building") + "_" + _nextBuildingId;
            go.transform.SetParent(transform, false);
            go.transform.position = _ghost.transform.position;
            go.transform.rotation = _ghost.transform.rotation;

            var building = go.AddComponent<CityBuilding>();
            building.BuildingId = _nextBuildingId++;
            building.FootprintCells = new Vector2Int(_footprintX, _footprintZ);
            building.Area = _targetArea;
            building.CellX = _targetCellX;
            building.CellZ = _targetCellZ;

            _targetArea.SetCellBuildingId(
                _targetCellX, _targetCellZ,
                _targetCellX, _targetCellZ,
                _footprintX, _footprintZ,
                building.BuildingId);

            // 新建筑占住了当前格子，下一帧预览会变成红色。
            _targetValid = false;
        }

        // ---------------------------------------------------------------- 长按移动

        private void UpdateLongPress()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                CancelPress();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                _pressBuilding = RaycastBuilding();
                _pressStartTime = Time.unscaledTime;
                _pressScreenPosition = Input.mousePosition;
            }

            if (_pressBuilding == null)
                return;

            if (!Input.GetMouseButton(0))
            {
                CancelPress();
                return;
            }

            Vector2 offset = (Vector2)Input.mousePosition - _pressScreenPosition;
            if (offset.sqrMagnitude > MovePressMoveThresholdPixels * MovePressMoveThresholdPixels)
            {
                CancelPress();
                return;
            }

            if (Time.unscaledTime - _pressStartTime < MovePressDuration)
                return;

            var building = _pressBuilding;
            _pressBuilding = null;
            BeginMove(building);
        }

        private void CancelPress()
        {
            _pressBuilding = null;
        }

        private void BeginMove(CityBuilding building)
        {
            if (building == null || building.Area == null)
                return;

            _moving = building;
            _moveArea = building.Area;
            _moveTargetArea = null;
            _moveOldCellX = building.CellX;
            _moveOldCellZ = building.CellZ;
            _moveFootprintX = Mathf.Max(1, building.FootprintCells.x);
            _moveFootprintZ = Mathf.Max(1, building.FootprintCells.y);
            _moveTargetCellX = _moveOldCellX;
            _moveTargetCellZ = _moveOldCellZ;
            _moveValid = true;

            _moveOriginalPosition = building.transform.position;
            _moveOriginalRotation = building.transform.rotation;
            _moveYOffset = building.transform.position.y - _moveArea.transform.position.y;

            // 先释放原格子的占位，移动过程中其他格子马上可用。
            _moveArea.RemoveCellBuildingId(
                _moveOldCellX, _moveOldCellZ,
                _moveFootprintX, _moveFootprintZ,
                building.BuildingId);

            _moveColliders = building.GetComponentsInChildren<Collider>(true);
            _moveColliderEnabled = new bool[_moveColliders.Length];
            for (int i = 0; i < _moveColliders.Length; i++)
            {
                _moveColliderEnabled[i] = _moveColliders[i].enabled;
                _moveColliders[i].enabled = false;
            }

            _moveRenderers = building.GetComponentsInChildren<Renderer>(true);
            _moveOriginalMaterials = new Material[_moveRenderers.Length][];
            for (int i = 0; i < _moveRenderers.Length; i++)
                _moveOriginalMaterials[i] = _moveRenderers[i].sharedMaterials;

            _moveMaterialApplied = false;
            _moveGreen = true;
            ApplyMoveMaterial(true);

            _mgr?.SetGridVisible(true);
        }

        private void UpdateMove()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelMove();
                return;
            }

            if (!TryGetGroundPoint(out Vector3 groundPoint))
                return;

            float cellSize = _mgr != null ? _mgr.cellSize : 1f;

            BuildableArea area = null;
            int centerX = 0;
            int centerZ = 0;
            bool inArea = false;
            if (_mgr != null && _mgr.TryGetAreaAt(groundPoint, out area))
                inArea = area.TryWorldToCell(groundPoint, out centerX, out centerZ);

            Vector3 center;
            Quaternion rotation;
            float positionY;

            if (inArea)
            {
                _moveTargetArea = area;
                _moveTargetCellX = centerX - _moveFootprintX / 2;
                _moveTargetCellZ = centerZ - _moveFootprintZ / 2;
                center = area.GetFootprintCenterWorld(
                    _moveTargetCellX, _moveTargetCellZ, _moveFootprintX, _moveFootprintZ);
                rotation = area.transform.rotation;
                positionY = area.transform.position.y + _moveYOffset;
                _moveValid = area.IsCellAvailable(
                    _moveTargetCellX, _moveTargetCellZ,
                    _moveFootprintX, _moveFootprintZ,
                    _moving.BuildingId);
            }
            else
            {
                _moveTargetArea = null;
                _moveTargetCellX = Mathf.FloorToInt(groundPoint.x / cellSize) - _moveFootprintX / 2;
                _moveTargetCellZ = Mathf.FloorToInt(groundPoint.z / cellSize) - _moveFootprintZ / 2;
                center = new Vector3(
                    (_moveTargetCellX + _moveFootprintX * 0.5f) * cellSize,
                    groundPoint.y,
                    (_moveTargetCellZ + _moveFootprintZ * 0.5f) * cellSize);
                rotation = Quaternion.identity;
                positionY = groundPoint.y + _moveYOffset;
                _moveValid = false;
            }

            _moving.transform.rotation = rotation;
            _moving.transform.position = new Vector3(center.x, positionY, center.z);

            ApplyMoveMaterial(_moveValid);

            if (Input.GetMouseButtonDown(0) && _moveValid)
                CommitMove();
        }

        private void CommitMove()
        {
            if (!_moveValid || _moveTargetArea == null || _moving == null)
                return;

            var building = _moving;
            var area = _moveTargetArea;

            area.SetCellBuildingId(
                _moveOldCellX, _moveOldCellZ,
                _moveTargetCellX, _moveTargetCellZ,
                _moveFootprintX, _moveFootprintZ,
                building.BuildingId);

            building.Area = area;
            building.CellX = _moveTargetCellX;
            building.CellZ = _moveTargetCellZ;

            RestoreMoveVisuals();
            EndMove();
        }

        private void CancelMove()
        {
            if (_moving == null)
                return;

            if (_moveArea != null)
            {
                _moveArea.SetCellBuildingId(
                    _moveOldCellX, _moveOldCellZ,
                    _moveOldCellX, _moveOldCellZ,
                    _moveFootprintX, _moveFootprintZ,
                    _moving.BuildingId);

                _moving.transform.position = _moveOriginalPosition;
                _moving.transform.rotation = _moveOriginalRotation;
                _moving.Area = _moveArea;
                _moving.CellX = _moveOldCellX;
                _moving.CellZ = _moveOldCellZ;
            }

            RestoreMoveVisuals();
            EndMove();
        }

        private void RestoreMoveVisuals()
        {
            if (_moveColliders != null && _moveColliderEnabled != null)
            {
                for (int i = 0; i < _moveColliders.Length; i++)
                    if (_moveColliders[i] != null)
                        _moveColliders[i].enabled = _moveColliderEnabled[i];
            }

            if (_moveRenderers != null && _moveOriginalMaterials != null)
            {
                for (int i = 0; i < _moveRenderers.Length; i++)
                    if (_moveRenderers[i] != null)
                        _moveRenderers[i].sharedMaterials = _moveOriginalMaterials[i];
            }

            _moveRenderers = null;
            _moveOriginalMaterials = null;
            _moveColliders = null;
            _moveColliderEnabled = null;
            _moveMaterialApplied = false;

            _moveArea?.RefreshGridColors();
        }

        private void EndMove()
        {
            _moving = null;
            _moveArea = null;
            _moveTargetArea = null;

            _mgr?.SetGridVisible(HasSelection);
        }

        private void ApplyMoveMaterial(bool valid)
        {
            if (_moveRenderers == null)
                return;

            if (_moveMaterialApplied && _moveGreen == valid)
                return;

            var material = valid ? _validMaterial : _invalidMaterial;
            if (material == null)
                return;

            for (int i = 0; i < _moveRenderers.Length; i++)
            {
                var renderer = _moveRenderers[i];
                if (renderer == null)
                    continue;

                int materialCount = renderer.sharedMaterials.Length;
                if (materialCount <= 0)
                    materialCount = 1;

                var materials = new Material[materialCount];
                for (int j = 0; j < materialCount; j++)
                    materials[j] = material;

                renderer.sharedMaterials = materials;
            }

            _moveMaterialApplied = true;
            _moveGreen = valid;
        }

        // ---------------------------------------------------------------- 射线 / 工具

        /// <summary>取地面点：跳过已放置建筑的碰撞体，避免射线打在建筑顶面。</summary>
        private static bool TryGetGroundPoint(out Vector3 point)
        {
            point = default;

            var camera = Camera.main;
            if (camera == null)
                return false;

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 4000f, ~0, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider != null &&
                        hits[i].collider.GetComponentInParent<CityBuilding>() != null)
                        continue;

                    point = hits[i].point;
                    return true;
                }
            }

            // 地形还没加载时用水平面兜底。
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            return false;
        }

        private static CityBuilding RaycastBuilding()
        {
            var camera = Camera.main;
            if (camera == null)
                return null;

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 4000f, ~0, QueryTriggerInteraction.Ignore))
                return null;

            return hit.collider != null ? hit.collider.GetComponentInParent<CityBuilding>() : null;
        }

        private static bool TryGetRendererBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

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

            return hasBounds;
        }
    }
}
