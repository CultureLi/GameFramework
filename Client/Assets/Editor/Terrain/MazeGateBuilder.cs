using GameFramework.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// 根据 MazeLayout 在 OpenWorld 场景里生成关卡门。
    /// 每个门包含 BoxCollider + NavMeshObstacle(carving) + 可视化方块 + LevelGate 脚本。
    /// 门默认打开，这样 NavMeshSurface 烘焙时山口是通的；运行时关闭会动态挖掉导航网格。
    /// </summary>
    public static class MazeGateBuilder
    {
        public const string RootName = "MazeGates_Generated";
        private const string MaterialPath = "Assets/NoBundleRes/Scene/OpenWorld/MazeGate.mat";

        public static void Rebuild(Terrain terrain, MazeLayout layout, TerrainGeneratorSettings s)
        {
            var scene = terrain.gameObject.scene;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == RootName)
                    Object.DestroyImmediate(root);
            }

            var gateRoot = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(gateRoot, scene);

            var material = EnsureGateMaterial();
            float terrainY = terrain.transform.position.y;

            for (int i = 0; i < layout.Gates.Count; i++)
            {
                var gate = layout.Gates[i];
                var gateObject = new GameObject($"Gate_{i:00}");
                gateObject.transform.SetParent(gateRoot.transform, false);

                float surfaceY = terrain.SampleHeight(new Vector3(gate.Position.x, 0f, gate.Position.y)) + terrainY;
                gateObject.transform.position = new Vector3(gate.Position.x, surfaceY, gate.Position.y);
                gateObject.transform.rotation = Quaternion.LookRotation(
                    new Vector3(gate.Normal.x, 0f, gate.Normal.y), Vector3.up);

                var size = new Vector3(gate.Width, s.mazeGateHeight, s.mazeGateThickness);
                var center = new Vector3(0f, s.mazeGateHeight * 0.5f, 0f);

                var collider = gateObject.AddComponent<BoxCollider>();
                collider.size = size;
                collider.center = center;

                var obstacle = gateObject.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = size;
                obstacle.center = center;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;

                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                visual.transform.SetParent(gateObject.transform, false);
                visual.transform.localPosition = center;
                visual.transform.localScale = size;
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                var renderer = visual.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;

                var levelGate = gateObject.AddComponent<LevelGate>();
                levelGate.gateID = i + 1;
                levelGate.blockingCollider = collider;
                levelGate.obstacle = obstacle;
                levelGate.gateRenderer = renderer;
                levelGate.SetOpen(s.mazeGateOpenByDefault);
            }

            gateRoot.AddComponent<MazeGateDebugToggle>();

            EditorUtility.SetDirty(gateRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[MazeGate] 生成 {layout.Gates.Count} 个关卡门，默认{(s.mazeGateOpenByDefault ? "打开" : "关闭")}");
        }

        private static Material EnsureGateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            material = new Material(shader) { name = "MazeGate" };
            var green = new Color(0.15f, 0.9f, 0.25f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", green);
            if (material.HasProperty("_Color")) material.SetColor("_Color", green);

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
