using GameMain;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MapEditor
{
    /// <summary>
    /// 地图编辑器共用的场景结构、网格和测量工具。
    /// 当前节点结构：场景根节点 DataEditor，下面挂 Building、Decorate、Resource。
    /// 同时兼容旧版本的 Scene/DataRoot、Scene/DataEditor 等布局，定位时自动迁移。
    /// </summary>
    public static class MapEditorUtils
    {
        public const string DataEditorName = "DataEditor";
        public const string LegacySceneRootName = "Scene";
        public const string LegacyDataRootName = "DataRoot";
        public const string BuildingRootName = "Building";
        public const string DecorateRootName = "Decorate";
        public const string ResourceRootName = "Resource";

        private static readonly string[] CategoryRootNames =
        {
            BuildingRootName,
            DecorateRootName,
            ResourceRootName,
        };

        /// <summary>
        /// 地图名：优先用 Main 场景里 WorldChunkStreaming 配置的地块场景名（通常是 OpenWorld），
        /// 这样作者场景和运行时都会读写同一个 MapData 目录。
        /// </summary>
        public static string GetMapName()
        {
            var streaming = Object.FindObjectOfType<WorldChunkStreaming>();
            if (streaming != null && !string.IsNullOrEmpty(streaming.sceneName))
                return streaming.sceneName;

            var scene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
        }

        public static string GetDataFolderPath()
        {
            return "Assets/BundleRes/MapData/" + SanitizeFileName(GetMapName());
        }

        /// <summary>
        /// 查找/创建 DataEditor 根节点，并保证 Building、Decorate、Resource 是它的子节点。
        /// create=false 时只查找，不修改场景。
        /// </summary>
        public static Transform EnsureHierarchy(bool create, out Transform dataRoot)
        {
            dataRoot = null;

            var scene = SceneManager.GetActiveScene();

            if (!create)
            {
                var existing = FindExistingDataEditor(scene);
                dataRoot = existing;
                return existing;
            }

            var dataEditor = ResolveDataEditor(scene);
            if (dataEditor == null)
            {
                dataEditor = new GameObject(DataEditorName);
                Undo.RegisterCreatedObjectUndo(dataEditor, "Create Map Data Editor Root");
            }

            // DataEditor 必须是场景根节点；旧布局下它可能挂在 Scene 下面。
            if (dataEditor.transform.parent != null)
            {
                Undo.RecordObject(dataEditor.transform, "Move Map Data Editor Root");
                dataEditor.transform.SetParent(null, true);
            }

            if (dataEditor.name != DataEditorName)
            {
                Undo.RecordObject(dataEditor, "Rename Map Data Editor Root");
                dataEditor.name = DataEditorName;
                EditorUtility.SetDirty(dataEditor);
            }

            if (dataEditor.GetComponent<MapAuthoringRoot>() == null)
                Undo.AddComponent<MapAuthoringRoot>(dataEditor);

            // 把旧布局里的分类节点统一收拢到 DataEditor 下。
            MigrateCategoryRoots(scene, dataEditor.transform);

            EnsureCategoryRoot(dataEditor.transform, BuildingRootName);
            EnsureCategoryRoot(dataEditor.transform, DecorateRootName);
            EnsureCategoryRoot(dataEditor.transform, ResourceRootName);

            DeleteLegacySceneRoot(scene, dataEditor);

            dataRoot = dataEditor.transform;
            return dataEditor.transform;
        }

        private static Transform FindExistingDataEditor(Scene scene)
        {
            var root = FindRoot(scene, DataEditorName);
            if (root != null)
                return root.transform;

            root = FindRoot(scene, LegacyDataRootName);
            if (root != null)
                return root.transform;

            var sceneRoot = FindRoot(scene, LegacySceneRootName);
            if (sceneRoot == null)
                return null;

            var child = FindChild(sceneRoot.transform, DataEditorName);
            if (child != null)
                return child;

            child = FindChild(sceneRoot.transform, LegacyDataRootName);
            return child;
        }

        private static GameObject ResolveDataEditor(Scene scene)
        {
            var root = FindRoot(scene, DataEditorName);
            if (root != null)
                return root;

            root = FindRoot(scene, LegacyDataRootName);
            if (root != null)
                return root;

            var sceneRoot = FindRoot(scene, LegacySceneRootName);
            if (sceneRoot == null)
                return null;

            var child = FindChild(sceneRoot.transform, DataEditorName);
            if (child != null)
                return child.gameObject;

            child = FindChild(sceneRoot.transform, LegacyDataRootName);
            return child != null ? child.gameObject : null;
        }

        private static void MigrateCategoryRoots(Scene scene, Transform dataEditor)
        {
            var sceneRoot = FindRoot(scene, LegacySceneRootName);
            if (sceneRoot != null)
            {
                // Scene 下的分类节点（V2 上一版布局）。
                for (int i = 0; i < CategoryRootNames.Length; i++)
                    MoveCategoryTo(dataEditor, FindChild(sceneRoot.transform, CategoryRootNames[i]));

                // Scene 下可能还残留一个旧的 DataEditor / DataRoot。
                MoveCategoriesFromContainer(dataEditor, FindChild(sceneRoot.transform, DataEditorName));
                MoveCategoriesFromContainer(dataEditor, FindChild(sceneRoot.transform, LegacyDataRootName));
            }

            // 场景根目录下可能还有一个旧的 DataRoot。
            var legacyRoot = FindRoot(scene, LegacyDataRootName);
            if (legacyRoot != null && legacyRoot.transform != dataEditor)
                MoveCategoriesFromContainer(dataEditor, legacyRoot.transform);
        }

        private static void MoveCategoriesFromContainer(Transform dataEditor, Transform container)
        {
            if (container == null || container == dataEditor)
                return;

            for (int i = 0; i < CategoryRootNames.Length; i++)
                MoveCategoryTo(dataEditor, FindChild(container, CategoryRootNames[i]));

            if (container.childCount == 0)
                Undo.DestroyObjectImmediate(container.gameObject);
        }

        private static void MoveCategoryTo(Transform dataEditor, Transform category)
        {
            if (category == null || dataEditor == null || category == dataEditor)
                return;

            if (category.parent == dataEditor)
                return;

            var existing = FindChild(dataEditor, category.name);
            if (existing != null && existing != category)
            {
                // 已经有同名分类节点：把子对象并进去，保留世界坐标。
                while (category.childCount > 0)
                {
                    var child = category.GetChild(0);
                    Undo.RecordObject(child, "Merge Map Category");
                    child.SetParent(existing, true);
                }

                if (category.childCount == 0)
                    Undo.DestroyObjectImmediate(category.gameObject);
                return;
            }

            Undo.RecordObject(category, "Move Map Category Root");
            category.SetParent(dataEditor, true);
        }

        private static void EnsureCategoryRoot(Transform dataEditor, string name)
        {
            if (FindChild(dataEditor, name) != null)
                return;

            var obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "Create Map Category Root");
            obj.transform.SetParent(dataEditor, false);
        }

        private static void DeleteLegacySceneRoot(Scene scene, GameObject dataEditor)
        {
            var sceneRoot = FindRoot(scene, LegacySceneRootName);
            if (sceneRoot == null || sceneRoot == dataEditor)
                return;

            // 如果 Scene 里还有别的内容，不要误删用户对象；至少去掉作者根标记，
            // 避免运行时把整个 Scene 节点隐藏掉。
            var marker = sceneRoot.GetComponent<MapAuthoringRoot>();
            if (marker != null && sceneRoot.transform.childCount > 0)
                Undo.DestroyObjectImmediate(marker);

            if (sceneRoot.transform.childCount == 0)
            {
                Undo.DestroyObjectImmediate(sceneRoot);
                return;
            }

            Debug.LogWarning("[MapEditor] Scene 节点还有未迁移的子对象，已保留；请手动检查该节点。");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }

            return null;
        }

        public static Transform GetCategoryRoot(Transform dataRoot, MapEntityCategory category)
        {
            if (dataRoot == null)
                return null;

            string name = GetCategoryRootName(category);
            var nested = FindChild(dataRoot, name);
            if (nested != null && nested.childCount > 0)
                return nested;

            // 兼容尚未迁移的旧布局：分类节点可能挂在 DataEditor 的父节点下。
            if (dataRoot.parent != null)
            {
                var sibling = FindChild(dataRoot.parent, name);
                if (sibling != null)
                    return sibling;
            }

            return nested;
        }

        public static string GetCategoryRootName(MapEntityCategory category)
        {
            switch (category)
            {
                case MapEntityCategory.Building: return BuildingRootName;
                case MapEntityCategory.Decorate: return DecorateRootName;
                case MapEntityCategory.Resource: return ResourceRootName;
                default: return null;
            }
        }

        public static Transform FindChild(Transform parent, string name)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }

        /// <summary>把世界 XZ 吸附到以世界原点为基准的格子中心，Y 保持不变。</summary>
        public static Vector3 Snap(Vector3 position, float cellSize)
        {
            if (cellSize <= 0.0001f)
                return position;

            float x = (Mathf.Floor(position.x / cellSize) + 0.5f) * cellSize;
            float z = (Mathf.Floor(position.z / cellSize) + 0.5f) * cellSize;
            return new Vector3(x, position.y, z);
        }

        /// <summary>取对象所有 Collider（没有则 Renderer）的世界包围盒 XZ 大小。</summary>
        public static Vector2 MeasureSize(GameObject go)
        {
            if (go == null)
                return Vector2.zero;

            bool hasBounds = false;
            Bounds bounds = default;

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

            return hasBounds ? new Vector2(bounds.size.x, bounds.size.z) : Vector2.zero;
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Map";

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (chars[i] != invalid[j])
                        continue;

                    chars[i] = '_';
                    break;
                }
            }

            return new string(chars);
        }
    }
}
