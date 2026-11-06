using System.Linq;
using GameFramework.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// 生成完地形和关卡门后，用场景里的 NavMeshSurface 烘焙导航网格。
    /// 烘焙前临时打开所有门（禁用碰撞体/Obstacle），保证山口在静态导航网格里是可通行的；
    /// 运行时关门由 NavMeshObstacle carving 动态挖洞。
    /// </summary>
    public static class MazeNavMeshBaker
    {
        public static void Bake(Terrain terrain)
        {
            BakeScene(terrain.gameObject.scene);
        }

        /// <summary>
        /// 直接烘焙当前打开场景，不依赖 Terrain 参数；MapEditor 窗口用这个入口。
        /// </summary>
        public static void BakeActiveScene()
        {
            BakeScene(SceneManager.GetActiveScene());
        }

        private static void BakeScene(Scene scene)
        {
            var surface = Object.FindObjectOfType<NavMeshSurface>();
            if (surface == null)
            {
                var surfaceObject = new GameObject("NavMeshSurface");
                SceneManager.MoveGameObjectToScene(surfaceObject, scene);
                surface = surfaceObject.AddComponent<NavMeshSurface>();
            }

            // 现有场景里的 NavMeshSurface 位置是历史遗留的偏移值，必须归零，
            // 否则 CollectObjects.All 烘出来的导航数据会整体偏移。
            surface.transform.position = Vector3.zero;
            surface.transform.rotation = Quaternion.identity;
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;

            var gates = Object.FindObjectsOfType<LevelGate>(true);
            var colliderStates = gates.Select(g => g.blockingCollider != null && g.blockingCollider.enabled).ToArray();
            var obstacleStates = gates.Select(g => g.obstacle != null && g.obstacle.enabled).ToArray();

            foreach (var gate in gates)
            {
                if (gate.blockingCollider != null) gate.blockingCollider.enabled = false;
                if (gate.obstacle != null) gate.obstacle.enabled = false;
            }

            try
            {
                surface.BuildNavMesh();
            }
            finally
            {
                for (int i = 0; i < gates.Length; i++)
                {
                    if (gates[i].blockingCollider != null)
                        gates[i].blockingCollider.enabled = colliderStates[i];
                    if (gates[i].obstacle != null)
                        gates[i].obstacle.enabled = obstacleStates[i];
                }
            }

            EditorUtility.SetDirty(surface);
            if (surface.navMeshData != null)
                EditorUtility.SetDirty(surface.navMeshData);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[MazeNavMesh] NavMeshSurface 烘焙完成");
        }
    }
}
