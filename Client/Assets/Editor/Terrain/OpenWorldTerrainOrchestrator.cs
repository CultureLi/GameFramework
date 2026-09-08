using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework.EditorTools.TerrainGen
{
    public static class OpenWorldTerrainOrchestrator
    {
        public const string ScenePath = "Assets/NoBundleRes/Scene/OpenWorld/OpenWorld.unity";
        public const string TerrainDataPath = "Assets/NoBundleRes/Scene/OpenWorld/OpenWorldTerrainData.asset";

        public enum Step { All, Splatmap, Vegetation, Clear }

        public static void Run(TerrainGeneratorSettings s, Step step)
        {
            var currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var terrain = FindTerrain();
            if (terrain == null) { Debug.LogError($"[TerrainGen] Terrain GameObject not found in {ScenePath}"); return; }
            var td = terrain.terrainData;
            if (td == null) { Debug.LogError("[TerrainGen] TerrainData is missing on Terrain component."); return; }

            try
            {
                if (step == Step.Clear)
                {
                    ClearAll(terrain, td);
                    return;
                }

                EditorUtility.DisplayProgressBar("OpenWorld Terrain", "Ensuring shaders...", 0.05f);
                ShaderCodegen.EnsureShaders(s.forceRegenerateAssets);

                EditorUtility.DisplayProgressBar("OpenWorld Terrain", "Ensuring terrain layers...", 0.15f);
                var layers = TerrainLayerFactory.EnsureAll(s.forceRegenerateAssets);
                td.terrainLayers = layers;

                var main = RoadPath.BuildMain(s);
                var muds = RoadPath.BuildMudBranches(s, main);

                if (step == Step.All)
                {
                    EditorUtility.DisplayProgressBar("OpenWorld Terrain", "Sculpting heightmap...", 0.30f);
                    TerrainHeightmapGenerator.Generate(td, s, main, muds);
                }

                if (step == Step.All || step == Step.Splatmap)
                {
                    EditorUtility.DisplayProgressBar("OpenWorld Terrain", "Painting splatmap...", 0.50f);
                    TerrainSplatmapGenerator.PaintSplatmap(td, main, muds, s);
                }

                EditorUtility.DisplayProgressBar("OpenWorld Terrain", "Ensuring vegetation assets...", 0.70f);
                var veg = VegetationFactory.EnsureAll(s.forceRegenerateAssets);
                if (veg == null) return;

                if (step == Step.All || step == Step.Vegetation)
                {
                    EditorUtility.DisplayProgressBar("OpenWorld Terrain", "Placing vegetation & buildings...", 0.85f);
                    TerrainVegetationPlacer.Apply(terrain, td, veg, main, muds, s);
                }

                EditorUtility.DisplayProgressBar("OpenWorld Terrain", "Saving...", 0.98f);
                EditorUtility.SetDirty(td);
                EditorUtility.SetDirty(terrain);
                EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                Debug.Log($"[TerrainGen] Done ({step}).");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static Terrain FindTerrain()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var t = root.GetComponentInChildren<Terrain>(true);
                if (t != null) return t;
            }
            return null;
        }

        static void ClearAll(Terrain terrain, TerrainData td)
        {
            int res = td.heightmapResolution;
            td.SetHeights(0, 0, new float[res, res]);
            if (td.terrainLayers != null && td.terrainLayers.Length > 0)
            {
                int aRes = td.alphamapResolution;
                var alpha = new float[aRes, aRes, td.terrainLayers.Length];
                for (int y = 0; y < aRes; y++) for (int x = 0; x < aRes; x++) alpha[y, x, 0] = 1f;
                td.SetAlphamaps(0, 0, alpha);
            }
            td.SetTreeInstances(new TreeInstance[0], true);
            if (td.detailPrototypes != null && td.detailResolution > 0)
                for (int i = 0; i < td.detailPrototypes.Length; i++)
                    td.SetDetailLayer(0, 0, i, new int[td.detailResolution, td.detailResolution]);
            terrain.Flush();

            // Remove building instances
            var scene = terrain.gameObject.scene;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Buildings_Generated") Object.DestroyImmediate(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
        }
    }
}
