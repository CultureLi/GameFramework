using System;
using System.Linq;
using GameMain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// 根据矩形迷宫草图生成 OpenWorld 场景：
    ///   - 地形高度全部压到 0，清除树和草；
    ///   - 用 AssetPathCfg 301（Wall_1）生成红色围墙；
    ///   - 把 Building 节点下 7 个关卡移动到绿色框位置；
    ///   - 给关卡、建筑、采集物挂 NavMeshModifier 并勾选 Ignore From Build；
    ///   - 可选重新烘焙 NavMeshSurface。
    /// </summary>
    public static class RectMazeGenerator
    {
        public const string ScenePath = "Assets/NoBundleRes/Scene/OpenWorld/OpenWorld.unity";

        private const string WallPrefabPath = "Assets/BundleRes/Prefab/MapEntity/Building/Wall_1.prefab"; // AssetPathCfg 301
        private const string DataEditorName = "DataEditor";
        private const string WallsRootName = "Walls";
        private const string OldGeneratedGatesRoot = "MazeGates_Generated";
        private const float WallPrefabLength = 10f;
        private const float WallHeight = 5f;
        private const float WallY = WallHeight * 0.5f;
        private const float GatePrefabLength = 10f;

        public static bool Generate(bool bakeNavMesh)
        {
            if (!OpenTargetScene())
                return false;

            var terrain = FindTerrain();
            if (terrain == null)
            {
                Debug.LogError("[RectMaze] OpenWorld 场景里找不到 Terrain");
                return false;
            }

            try
            {
                FlattenTerrain(terrain);

                var dataEditor = FindDataEditor(terrain.gameObject.scene);
                if (dataEditor == null)
                    throw new InvalidOperationException("场景里找不到 DataEditor 根节点");

                ClearGeneratedWalls(dataEditor);
                RepositionGates(terrain.gameObject.scene);
                AddWalls(dataEditor);
                FlattenMapEntitiesAndAddModifiers();
                AddMapEntityInfoToWalls(dataEditor);

                if (bakeNavMesh)
                    BakeNavMesh(terrain);

                SaveScene(terrain);
                Debug.Log("[RectMaze] 地形、围墙、关卡位置生成完成" +
                          (bakeNavMesh ? "，NavMesh 已烘焙" : string.Empty));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Rect Maze", ex.Message, "OK");
                return false;
            }
        }

        public static bool RepositionGatesOnly()
        {
            if (!OpenTargetScene())
                return false;

            var terrain = FindTerrain();
            if (terrain == null)
            {
                Debug.LogError("[RectMaze] OpenWorld 场景里找不到 Terrain");
                return false;
            }

            try
            {
                RepositionGates(terrain.gameObject.scene);
                FlattenMapEntitiesAndAddModifiers();
                SaveScene(terrain);
                Debug.Log("[RectMaze] 7 个关卡位置已更新");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// 不重新生成地形和围墙，只给当前场景里已有的 DataEditor/Walls 墙体补挂 MapEntityInfo。
        /// InstanceId 保持为空，由 MapDataExporter 在导出时生成并回写。
        /// </summary>
        public static bool AddMapEntityInfoToWallsOnly()
        {
            var scene = SceneManager.GetActiveScene();
            var dataEditor = FindDataEditor(scene);
            if (dataEditor == null)
            {
                Debug.LogWarning("[RectMaze] 当前场景找不到 DataEditor");
                return false;
            }

            var wallsRoot = dataEditor.transform.Find(WallsRootName);
            if (wallsRoot == null || wallsRoot.childCount == 0)
            {
                Debug.LogWarning("[RectMaze] DataEditor/Walls 下没有墙体");
                return false;
            }

            int count = wallsRoot.childCount;
            AddMapEntityInfoToWalls(dataEditor);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RectMaze] 已为 {count} 个墙体添加/更新 MapEntityInfo (ConfigId/AssetId=301)");
            return true;
        }

        public static void ClearGeneratedWallsOnly()
        {
            var scene = SceneManager.GetActiveScene();
            var dataEditor = FindDataEditor(scene);
            if (dataEditor == null)
                return;

            ClearGeneratedWalls(dataEditor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[RectMaze] 已清除生成的 Walls");
        }

        private static bool OpenTargetScene()
        {
            var current = SceneManager.GetActiveScene();
            if (current.path == ScenePath)
                return true;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        private static Terrain FindTerrain()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var terrain = root.GetComponentInChildren<Terrain>(true);
                if (terrain != null)
                    return terrain;
            }

            return UnityEngine.Object.FindObjectOfType<Terrain>(true);
        }

        private static GameObject FindDataEditor(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == DataEditorName)
                    return root;
            }

            return null;
        }

        private static void FlattenTerrain(Terrain terrain)
        {
            var data = terrain.terrainData;
            if (data == null)
                throw new InvalidOperationException("TerrainData 为空");

            int heightResolution = data.heightmapResolution;
            data.SetHeights(0, 0, new float[heightResolution, heightResolution]);

            int layerCount = data.terrainLayers?.Length ?? 0;
            if (layerCount > 0)
            {
                int alphaResolution = data.alphamapResolution;
                var alpha = new float[alphaResolution, alphaResolution, layerCount];
                for (int y = 0; y < alphaResolution; y++)
                {
                    for (int x = 0; x < alphaResolution; x++)
                        alpha[y, x, 0] = 1f;
                }

                data.SetAlphamaps(0, 0, alpha);
            }

            data.SetTreeInstances(new TreeInstance[0], true);

            if (data.detailPrototypes != null &&
                data.detailPrototypes.Length > 0 &&
                data.detailResolution > 0)
            {
                var empty = new int[data.detailResolution, data.detailResolution];
                for (int i = 0; i < data.detailPrototypes.Length; i++)
                    data.SetDetailLayer(0, 0, i, empty);
            }

            terrain.drawTreesAndFoliage = false;
            terrain.Flush();

            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(terrain);
        }

        private static void ClearGeneratedWalls(GameObject dataEditor)
        {
            var oldWalls = dataEditor.transform.Find(WallsRootName);
            if (oldWalls != null)
                Undo.DestroyObjectImmediate(oldWalls.gameObject);

            // 旧版地形工具可能留下的山体关卡，避免和新围墙重复。
            var scene = dataEditor.scene;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == OldGeneratedGatesRoot)
                    Undo.DestroyObjectImmediate(root);
            }
        }

        private static void RepositionGates(Scene scene)
        {
            var gates = UnityEngine.Object.FindObjectsOfType<MapEntityInfo>(true)
                .Where(info => info.Category == MapEntityCategory.Building &&
                               info.ConfigId >= 1 && info.ConfigId <= 7)
                .OrderBy(info => info.ConfigId)
                .ToArray();

            if (gates.Length != RectMazeLayout.Gates.Length)
            {
                throw new InvalidOperationException(
                    $"Building 节点下应有 7 个关卡，实际找到 {gates.Length} 个 " +
                    "(ConfigId 1~7, Category=Building)");
            }

            for (int i = 0; i < gates.Length; i++)
            {
                var spec = RectMazeLayout.Gates[i];
                var transform = gates[i].transform;

                Undo.RecordObject(transform, "Place Rect Maze Gate");
                Vector2 worldCenter = spec.WorldCenter;
                transform.position = new Vector3(worldCenter.x, 0f, worldCenter.y);
                transform.rotation = Quaternion.Euler(0f, spec.RotationY, 0f);

                var scale = transform.localScale;
                scale.x = Mathf.Max(0.1f, spec.WorldLength / GatePrefabLength);
                transform.localScale = scale;

                EditorUtility.SetDirty(transform);
                EditorUtility.SetDirty(gates[i]);
            }
        }

        private static void AddWalls(GameObject dataEditor)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WallPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"找不到围墙 prefab: {WallPrefabPath}");

            var wallsRoot = new GameObject(WallsRootName);
            Undo.RegisterCreatedObjectUndo(wallsRoot, "Create Rect Maze Walls");
            wallsRoot.transform.SetParent(dataEditor.transform, false);

            int wallIndex = 0;
            foreach (var segment in RectMazeLayout.Walls)
            {
                Vector2 start = segment.WorldStart;
                Vector2 end = segment.WorldEnd;
                Vector2 delta = end - start;
                float length = delta.magnitude;
                if (length < 0.01f)
                    continue;

                Vector2 direction = delta / length;
                int pieces = Mathf.Max(1, Mathf.CeilToInt(length / WallPrefabLength));
                float pieceLength = length / pieces;

                // Wall_1 的长边在本地 X，绕 Y 旋转让本地 X 对齐 world XZ 方向。
                float rotationY = Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg;
                var rotation = Quaternion.Euler(0f, rotationY, 0f);

                for (int i = 0; i < pieces; i++)
                {
                    Vector2 center = start + direction * (pieceLength * (i + 0.5f));
                    var wall = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wallsRoot.transform);
                    wall.name = $"Wall_{wallIndex:000}_{i:00}";
                    wall.transform.position = new Vector3(center.x, WallY, center.y);
                    wall.transform.rotation = rotation;
                    wall.transform.localScale = new Vector3(pieceLength / WallPrefabLength, 1f, 1f);
                    Undo.RegisterCreatedObjectUndo(wall, "Create Rect Maze Wall");
                }

                wallIndex++;
            }

            EditorUtility.SetDirty(wallsRoot);
        }

        /// <summary>
        /// 围墙也作为地图实体导出：使用 AssetPathCfg 301，Category 归入 Obstacle（要挡住导航网格，
        /// 不能挂 ignoreFromBuild），InstanceId 留空，由 MapDataExporter 在实际导出时生成并保存。
        /// </summary>
        private static void AddMapEntityInfoToWalls(GameObject dataEditor)
        {
            var wallsRoot = dataEditor.transform.Find(WallsRootName);
            if (wallsRoot == null)
                return;

            for (int i = 0; i < wallsRoot.childCount; i++)
            {
                var wall = wallsRoot.GetChild(i).gameObject;
                var info = wall.GetComponent<MapEntityInfo>();
                if (info == null)
                    info = Undo.AddComponent<MapEntityInfo>(wall);
                else
                    Undo.RecordObject(info, "Configure Wall MapEntityInfo");

                info.Category = MapEntityCategory.Obstacle;
                info.ConfigId = 301;
                info.AssetId = 301;

                EditorUtility.SetDirty(info);
                EditorUtility.SetDirty(wall);
            }
        }

        private static void FlattenMapEntitiesAndAddModifiers()
        {
            var infos = UnityEngine.Object.FindObjectsOfType<MapEntityInfo>(true);

            foreach (var info in infos)
            {
                // 围墙的 pivot 在中心，需要保持 WallY 高度，不能参与地图实体的 Y=0 压平。
                if (info.transform.parent != null &&
                    info.transform.parent.name == WallsRootName)
                {
                    continue;
                }

                var transform = info.transform;
                Vector3 position = transform.position;
                if (!Mathf.Approximately(position.y, 0f))
                {
                    Undo.RecordObject(transform, "Flatten Map Entity");
                    transform.position = new Vector3(position.x, 0f, position.z);
                    EditorUtility.SetDirty(transform);
                }

                if (info.Category != MapEntityCategory.Building &&
                    info.Category != MapEntityCategory.Resource)
                {
                    continue;
                }

                var modifier = info.GetComponent<NavMeshModifier>();
                if (modifier == null)
                    modifier = Undo.AddComponent<NavMeshModifier>(info.gameObject);
                else
                    Undo.RecordObject(modifier, "Set NavMeshModifier Ignore");

                modifier.ignoreFromBuild = true;
                modifier.enabled = true;
                EditorUtility.SetDirty(modifier);
                EditorUtility.SetDirty(info);
            }
        }

        private static void BakeNavMesh(Terrain terrain)
        {
            var surface = UnityEngine.Object.FindObjectOfType<NavMeshSurface>(true);
            bool wasActive = false;

            if (surface != null)
            {
                wasActive = surface.gameObject.activeSelf;
                surface.gameObject.SetActive(true);
            }

            try
            {
                MazeNavMeshBaker.Bake(terrain);
            }
            finally
            {
                if (surface != null)
                    surface.gameObject.SetActive(wasActive);
            }
        }

        private static void SaveScene(Terrain terrain)
        {
            var scene = terrain.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
