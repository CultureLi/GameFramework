using System.Collections.Generic;
using cfg;
using Framework;
using GameMain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MapEditor
{
    /// <summary>
    /// 地图编辑器窗口：
    ///   - 从 MapBuildingCfg / AssetPathCfg / MapResourceCfg 生成左侧列表；
    ///   - 通过 AssetPathCfg 解析 prefab；
    ///   - 在 Scene 视图里用真实 prefab 预览、网格吸附或自由摆放；
    ///   - 导出 BuildingData / DecorateData / ResourceData 的 ScriptableObject 和二进制文件。
    /// </summary>
    public sealed class MapEditorWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Map Editor";

        private static readonly string[] CategoryNames = { "建筑物", "装饰物", "采集资源" };

        private sealed class Choice
        {
            public int Id;
            public string Name;
            public int AssetId;
            public string AssetPath;
            public string Info;
        }

        private int _tab;
        private string _search = string.Empty;
        private int _selectedId = -1;

        private Vector2 _scroll;
        private bool _placementEnabled;
        private bool _useGrid = true;
        private float _cellSize = 1f;
        private bool _snapSelected = true;

        private bool _exportAll = true;
        private bool _exportBuilding = true;
        private bool _exportDecorate = true;
        private bool _exportResource = true;

        private ConfigMgr _configMgr;
        private TbMapBuildingCfg _buildingTable;
        private TbMapResourceCfg _resourceTable;
        private TbAssetPathCfg _assetTable;
        private bool _configError;
        private string _configMessage = "正在读取配置表...";

        private readonly List<Choice> _choices = new List<Choice>();
        private int _lastTab = -1;
        private string _lastSearch;

        private GameObject _ghost;
        private int _ghostConfigId = -1;
        private int _ghostTab = -1;

        private MapExportResult _lastExport;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<MapEditorWindow>("地图编辑器");
        }

        private void OnEnable()
        {
            ReloadConfig();
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;

            DestroyGhost();
            DisposeConfig();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("地图数据编辑器", EditorStyles.boldLabel);

            if (_configError)
                EditorGUILayout.HelpBox(_configMessage, MessageType.Error);
            else
                EditorGUILayout.HelpBox(_configMessage, MessageType.Info);

            EditorGUILayout.LabelField("数据目录", MapEditorUtils.GetDataFolderPath());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新读取配置"))
                ReloadConfig();
            if (GUILayout.Button("创建/定位节点"))
                EnsureNodes();
            if (GUILayout.Button("补全/修复 UID"))
                RepairInstanceIds();
            EditorGUILayout.EndHorizontal();

            int newTab = EditorGUILayout.Popup("类型", _tab, CategoryNames);
            if (newTab != _tab)
            {
                _tab = newTab;
                _selectedId = -1;
                _placementEnabled = false;
                DestroyGhost();
            }

            _search = EditorGUILayout.TextField("搜索", _search ?? string.Empty);

            if (_lastTab != _tab || _lastSearch != _search)
            {
                _lastTab = _tab;
                _lastSearch = _search;
                RebuildChoices();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(140f), GUILayout.MaxHeight(260f));
            if (_choices.Count == 0)
            {
                EditorGUILayout.LabelField("没有可用的配置数据");
            }
            else
            {
                for (int i = 0; i < _choices.Count; i++)
                {
                    var choice = _choices[i];
                    bool selected = choice.Id == _selectedId;
                    string label = (selected ? "● " : "○ ") + choice.Name + "   #" + choice.Id;
                    if (GUILayout.Button(label, EditorStyles.toolbarButton))
                    {
                        _selectedId = choice.Id;
                        _placementEnabled = true;
                        DestroyGhost();
                        SceneView.RepaintAll();
                    }

                    EditorGUILayout.LabelField(choice.Info, EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("放置设置", EditorStyles.boldLabel);
            _placementEnabled = EditorGUILayout.Toggle("放置模式（Scene 左键放置）", _placementEnabled);
            _useGrid = EditorGUILayout.Toggle("Grid 吸附", _useGrid);

            using (new EditorGUI.DisabledScope(!_useGrid))
            {
                _cellSize = EditorGUILayout.FloatField("格子大小", _cellSize);
            }

            if (_cellSize < 0.01f)
                _cellSize = 0.01f;

            _snapSelected = EditorGUILayout.Toggle("移动选中对象时吸附", _snapSelected);

            EditorGUILayout.HelpBox(
                "选择左侧条目后在 Scene 视图左键放置；Esc 退出放置。" +
                "Grid 模式下会吸附到格子中心，移动已放置对象时也会吸附。",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("导出", EditorStyles.boldLabel);

            bool newExportAll = EditorGUILayout.Toggle("All", _exportAll);
            if (newExportAll != _exportAll)
            {
                _exportAll = newExportAll;
                _exportBuilding = newExportAll;
                _exportDecorate = newExportAll;
                _exportResource = newExportAll;
            }

            EditorGUILayout.BeginHorizontal();
            _exportBuilding = EditorGUILayout.ToggleLeft("Building", _exportBuilding);
            _exportDecorate = EditorGUILayout.ToggleLeft("Decorate", _exportDecorate);
            _exportResource = EditorGUILayout.ToggleLeft("Resource", _exportResource);
            EditorGUILayout.EndHorizontal();

            _exportAll = _exportBuilding && _exportDecorate && _exportResource;

            MapExportModule modules = BuildModuleMask();

            using (new EditorGUI.DisabledScope(_configError || modules == MapExportModule.None))
            {
                if (GUILayout.Button("导出客户端数据"))
                    Export(modules, true, false);

                if (GUILayout.Button("导出服务端数据"))
                    Export(modules, false, true);
            }

            if (_lastExport == null)
                return;

            EditorGUILayout.HelpBox(
                $"{(_lastExport.Client ? "客户端" : "")}{(_lastExport.Client && _lastExport.Server ? "+" : "")}{(_lastExport.Server ? "服务端" : "")}  " +
                $"建筑 {_lastExport.BuildingCount}，装饰 {_lastExport.DecorateCount}，资源 {_lastExport.ResourceCount}",
                _lastExport.Success ? MessageType.Info : MessageType.Error);

            for (int i = 0; i < _lastExport.Errors.Count && i < 10; i++)
                EditorGUILayout.HelpBox(_lastExport.Errors[i], MessageType.Error);

            for (int i = 0; i < _lastExport.Warnings.Count && i < 10; i++)
                EditorGUILayout.HelpBox(_lastExport.Warnings[i], MessageType.Warning);
        }

        private void ReloadConfig()
        {
            DisposeConfig();

            _configError = false;
            _buildingTable = null;
            _resourceTable = null;
            _assetTable = null;

            try
            {
                _configMgr = new ConfigMgr();
                _buildingTable = _configMgr.GetTable<TbMapBuildingCfg>();
                _resourceTable = _configMgr.GetTable<TbMapResourceCfg>();
                _assetTable = _configMgr.GetTable<TbAssetPathCfg>();
                _configMessage = "配置表已加载";
            }
            catch (System.Exception ex)
            {
                _configError = true;
                _configMessage = "配置表加载失败: " + ex.Message;
            }

            _lastTab = _tab;
            _lastSearch = _search;
            RebuildChoices();
            Repaint();
        }

        private void DisposeConfig()
        {
            if (_configMgr == null)
                return;

            try
            {
                _configMgr.Shutdown(EShutdownType.Shutdown);
            }
            catch
            {
                // 编辑器关闭/重载时的清理失败可以忽略
            }

            _configMgr = null;
        }

        private void RebuildChoices()
        {
            _choices.Clear();

            if (!_configError)
            {
                if (_tab == (int)MapEntityCategory.Building && _buildingTable != null)
                {
                    foreach (var config in _buildingTable.DataList)
                        AddChoice(config.Id, config.Name, config.AssetId, config.Level, config.Type);
                }
                else if (_tab == (int)MapEntityCategory.Decorate && _assetTable != null)
                {
                    // V3：装饰物没有 MapDecorateCfg，直接使用 AssetPathCfg。
                    foreach (var config in _assetTable.DataList)
                        AddDecorationChoice(config);
                }
                else if (_tab == (int)MapEntityCategory.Resource && _resourceTable != null)
                {
                    foreach (var config in _resourceTable.DataList)
                        AddChoice(config.Id, config.Name, config.AssetId, config.Level, config.Type);
                }
            }

            if (GetSelectedChoice() == null)
                _selectedId = -1;
        }

        private void AddChoice(int id, string name, int assetId, int level, int type)
        {
            string search = _search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                bool nameMatch = !string.IsNullOrEmpty(name) &&
                                 name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool idMatch = id.ToString().IndexOf(search, System.StringComparison.Ordinal) >= 0;
                if (!nameMatch && !idMatch)
                    return;
            }

            string assetPath = _assetTable?.Get(assetId)?.AssetPath;
            bool exists = !string.IsNullOrEmpty(assetPath) &&
                          AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;

            string info = $"Lv.{level}  Type.{type}  AssetId.{assetId}";
            if (!exists)
                info += "  [资源缺失]";

            _choices.Add(new Choice
            {
                Id = id,
                Name = string.IsNullOrEmpty(name) ? "未命名" : name,
                AssetId = assetId,
                AssetPath = assetPath,
                Info = info,
            });
        }

        private void AddDecorationChoice(AssetPathCfg config)
        {
            if (config == null)
                return;

            string name = config.Desc;
            if (string.IsNullOrEmpty(name))
            {
                name = string.IsNullOrEmpty(config.AssetPath)
                    ? "AssetPath " + config.Id
                    : System.IO.Path.GetFileNameWithoutExtension(config.AssetPath);
            }

            string search = _search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                bool nameMatch = !string.IsNullOrEmpty(name) &&
                                 name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool idMatch = config.Id.ToString().IndexOf(search, System.StringComparison.Ordinal) >= 0;
                bool pathMatch = !string.IsNullOrEmpty(config.AssetPath) &&
                                 config.AssetPath.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (!nameMatch && !idMatch && !pathMatch)
                    return;
            }

            string assetPath = config.AssetPath;
            bool exists = !string.IsNullOrEmpty(assetPath) &&
                          AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;

            string info = $"AssetPathCfg Id.{config.Id}";
            if (!exists)
                info += "  [资源缺失]";

            _choices.Add(new Choice
            {
                Id = config.Id,
                Name = string.IsNullOrEmpty(name) ? "未命名" : name,
                AssetId = config.Id,
                AssetPath = assetPath,
                Info = info,
            });
        }

        private Choice GetSelectedChoice()
        {
            for (int i = 0; i < _choices.Count; i++)
            {
                if (_choices[i].Id == _selectedId)
                    return _choices[i];
            }

            return null;
        }

        private MapEntityCategory GetCategory()
        {
            return (MapEntityCategory)Mathf.Clamp(_tab, 0, 2);
        }

        private void EnsureNodes()
        {
            MapEditorUtils.EnsureHierarchy(true, out _);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            SceneView.RepaintAll();
            Debug.Log("[MapEditor] DataEditor/Building、Decorate、Resource 节点已就绪");
        }

        /// <summary>
        /// 补全缺失 UID，并给复制/合并产生的重复 UID 重新生成。
        /// 已上线地图如果依赖 UID 存了服务端归属数据，执行前要先确认。
        /// </summary>
        private void RepairInstanceIds()
        {
            var infos = Object.FindObjectsOfType<MapEntityInfo>(true);
            var seen = new HashSet<string>();
            int repaired = 0;

            foreach (var info in infos)
            {
                if (string.IsNullOrEmpty(info.InstanceId) || !seen.Add(info.InstanceId))
                {
                    Undo.RecordObject(info, "Repair Map Instance Id");
                    info.InstanceId = System.Guid.NewGuid().ToString("N");
                    EditorUtility.SetDirty(info);
                    seen.Add(info.InstanceId);
                    repaired++;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[MapEditor] UID 检查完成，补全/修复 {repaired} 个");
        }

        private MapExportModule BuildModuleMask()
        {
            MapExportModule modules = MapExportModule.None;
            if (_exportBuilding)
                modules |= MapExportModule.Building;
            if (_exportDecorate)
                modules |= MapExportModule.Decorate;
            if (_exportResource)
                modules |= MapExportModule.Resource;
            return modules;
        }

        private void Export(MapExportModule modules, bool exportClient, bool exportServer)
        {
            _lastExport = MapDataExporter.Export(
                _buildingTable, _resourceTable, _assetTable,
                modules, exportClient, exportServer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SceneView.RepaintAll();
            Repaint();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_placementEnabled || _configError)
                return;

            var choice = GetSelectedChoice();
            if (choice == null)
            {
                DestroyGhost();
                return;
            }

            EnsureGhost(choice);
            if (_ghost == null)
                return;

            var currentEvent = Event.current;

            if (currentEvent.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (!TryGetGroundPoint(currentEvent.mousePosition, out Vector3 point))
            {
                if (_ghost.activeSelf)
                    _ghost.SetActive(false);
                return;
            }

            Vector3 position = _useGrid
                ? MapEditorUtils.Snap(point, _cellSize)
                : point;

            if (!_ghost.activeSelf)
                _ghost.SetActive(true);

            _ghost.transform.position = new Vector3(position.x, point.y, position.z);
            _ghost.transform.rotation = Quaternion.identity;

            DrawGrid(point);

            if (currentEvent.type == EventType.MouseMove)
                SceneView.RepaintAll();

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                Place(choice, new Vector3(position.x, point.y, position.z));
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
            {
                _placementEnabled = false;
                DestroyGhost();
                currentEvent.Use();
                Repaint();
            }
        }

        private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
        {
            var ray = HandleUtility.GUIPointToWorldRay(screenPosition);

            if (Physics.Raycast(ray, out var hit, 5000f, ~0, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }

            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance) && distance >= 0f)
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = default;
            return false;
        }

        private void EnsureGhost(Choice choice)
        {
            if (_ghost != null && _ghostConfigId == choice.Id && _ghostTab == _tab)
                return;

            DestroyGhost();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(choice.AssetPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MapEditor] 预览资源不存在: " + choice.AssetPath);
                return;
            }

            _ghost = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _ghost.name = "__MapEditorGhost";
            _ghost.hideFlags = HideFlags.HideAndDontSave;

            // 预览对象只保留渲染，避免触发预制体上的逻辑/导航组件。
            foreach (var behaviour in _ghost.GetComponentsInChildren<Behaviour>(true))
                behaviour.enabled = false;

            foreach (var collider in _ghost.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            _ghostConfigId = choice.Id;
            _ghostTab = _tab;
        }

        private void DestroyGhost()
        {
            if (_ghost != null)
                Object.DestroyImmediate(_ghost);

            _ghost = null;
            _ghostConfigId = -1;
            _ghostTab = -1;
        }

        private void Place(Choice choice, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(choice.AssetPath);
            if (prefab == null)
            {
                Debug.LogError("[MapEditor] 放置失败，资源不存在: " + choice.AssetPath);
                return;
            }

            MapEditorUtils.EnsureHierarchy(true, out var dataRoot);
            var categoryRoot = MapEditorUtils.GetCategoryRoot(dataRoot, GetCategory());
            if (categoryRoot == null)
            {
                Debug.LogError("[MapEditor] 放置失败，分类目录不存在");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, categoryRoot);
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;

            var info = go.GetComponent<MapEntityInfo>();
            if (info == null)
                info = Undo.AddComponent<MapEntityInfo>(go);
            else
                Undo.RecordObject(info, "Place Map Entity");

            info.Category = GetCategory();
            info.ConfigId = choice.Id;
            info.AssetId = choice.AssetId;

            // 每个地图实例生成一次稳定 UID；导出、服务端权限都使用它。
            if (string.IsNullOrEmpty(info.InstanceId))
                info.InstanceId = System.Guid.NewGuid().ToString("N");

            Undo.RegisterCreatedObjectUndo(go, "Place Map Entity");
            EditorUtility.SetDirty(go);
            Selection.activeGameObject = go;

            EditorSceneManager.MarkSceneDirty(go.scene);
            SceneView.RepaintAll();
        }

        private void DrawGrid(Vector3 point)
        {
            if (!_useGrid)
                return;

            float cell = Mathf.Max(0.01f, _cellSize);
            float centerX = (Mathf.Floor(point.x / cell) + 0.5f) * cell;
            float centerZ = (Mathf.Floor(point.z / cell) + 0.5f) * cell;
            float y = point.y + 0.02f;

            const int radius = 10;
            Handles.color = new Color(1f, 1f, 1f, 0.18f);
            for (int i = -radius; i <= radius; i++)
            {
                // 画在格子边界上：中心点在 centerX/centerZ，边界偏移半格。
                float boundaryX = centerX + (i + 0.5f) * cell;
                Handles.DrawLine(
                    new Vector3(boundaryX, y, centerZ - (radius + 0.5f) * cell),
                    new Vector3(boundaryX, y, centerZ + (radius + 0.5f) * cell));

                float boundaryZ = centerZ + (i + 0.5f) * cell;
                Handles.DrawLine(
                    new Vector3(centerX - (radius + 0.5f) * cell, y, boundaryZ),
                    new Vector3(centerX + (radius + 0.5f) * cell, y, boundaryZ));
            }

            Handles.color = new Color(0.2f, 1f, 0.4f, 0.5f);
            Handles.DrawWireCube(new Vector3(centerX, y, centerZ), new Vector3(cell, 0.01f, cell));
        }

        private void OnEditorUpdate()
        {
            if (!_snapSelected || !_useGrid || EditorApplication.isPlaying)
                return;

            var go = Selection.activeGameObject;
            if (go == null || go.GetComponent<MapEntityInfo>() == null)
                return;

            var transform = go.transform;
            Vector3 position = transform.position;
            Vector3 snapped = MapEditorUtils.Snap(position, _cellSize);
            if ((snapped - position).sqrMagnitude < 1e-8f)
                return;

            Undo.RecordObject(transform, "Snap Map Entity");
            transform.position = new Vector3(snapped.x, position.y, snapped.z);
            EditorUtility.SetDirty(transform);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
