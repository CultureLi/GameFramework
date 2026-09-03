using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework.EditorTools.TerrainGen
{
    public static class TerrainVegetationPlacer
    {
        const string BuildingsRootName = "Buildings_Generated";

        public static void Apply(Terrain terrain, TerrainData td, VegetationSet veg,
            RoadPath main, List<RoadPath> muds, TerrainGeneratorSettings s)
        {
            RegisterPrototypes(td, veg, s);
            PlaceTrees(terrain, td, veg, main, muds, s);
            PaintDetails(td, veg, s);
            PlaceBuildings(terrain, td, veg, main, muds, s);
        }

        static void RegisterPrototypes(TerrainData td, VegetationSet veg, TerrainGeneratorSettings s)
        {
            var treeProtos = new TreePrototype[veg.trees.Count];
            for (int i = 0; i < veg.trees.Count; i++)
                treeProtos[i] = new TreePrototype { prefab = veg.trees[i], bendFactor = 0.2f };
            td.treePrototypes = treeProtos;

            var detailList = new List<DetailPrototype>();
            foreach (var p in veg.grasses)
                detailList.Add(new DetailPrototype
                {
                    prototype = p,
                    usePrototypeMesh = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minWidth = 0.6f,
                    maxWidth = 1.1f,
                    minHeight = 0.5f,
                    maxHeight = 1.0f,
                    noiseSpread = 0.3f,
                    healthyColor = new Color(0.85f, 1f, 0.85f, 1f),
                    dryColor = new Color(0.90f, 0.85f, 0.55f, 1f),
                });
            foreach (var p in veg.flowers)
                detailList.Add(new DetailPrototype
                {
                    prototype = p,
                    usePrototypeMesh = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minWidth = 0.35f,
                    maxWidth = 0.55f,
                    minHeight = 0.35f,
                    maxHeight = 0.55f,
                    noiseSpread = 0.5f,
                    healthyColor = Color.white,
                    dryColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                });
            td.detailPrototypes = detailList.ToArray();
            td.SetDetailResolution(s.detailResolution, s.detailResolutionPerPatch);
            td.wavingGrassStrength = 0.4f;
            td.wavingGrassAmount = 0.4f;
            td.wavingGrassSpeed = 0.4f;
            td.wavingGrassTint = new Color(0.85f, 0.90f, 0.75f, 1f);
        }

        static void PlaceTrees(Terrain terrain, TerrainData td, VegetationSet veg,
            RoadPath main, List<RoadPath> muds, TerrainGeneratorSettings s)
        {
            var rng = new System.Random(s.seed ^ 0x7BEE);
            int count = s.treeCount;
            var list = new List<TreeInstance>(count);
            int attempts = count * 6;

            int aRes = td.alphamapResolution;
            float[,,] alpha = td.GetAlphamaps(0, 0, aRes, aRes);

            float safeAsphaltR = s.asphaltHalfWidth + s.asphaltEdge + 1.5f;
            float safeMudR = s.mudHalfWidth + s.mudEdge + 1.0f;
            float safeAsphaltR2 = safeAsphaltR * safeAsphaltR;
            float safeMudR2 = safeMudR * safeMudR;

            while (list.Count < count && attempts-- > 0)
            {
                float u = (float)rng.NextDouble();
                float v = (float)rng.NextDouble();
                float wx = u * td.size.x;
                float wz = v * td.size.z;

                if (main.SqrDistanceTo(wx, wz) < safeAsphaltR2) continue;
                bool nearMud = false;
                for (int i = 0; i < muds.Count; i++)
                    if (muds[i].SqrDistanceTo(wx, wz) < safeMudR2) { nearMud = true; break; }
                if (nearMud) continue;

                int ax = Mathf.Clamp((int)(u * (aRes - 1)), 0, aRes - 1);
                int ay = Mathf.Clamp((int)(v * (aRes - 1)), 0, aRes - 1);
                float grassW = alpha[ay, ax, TerrainSplatmapGenerator.Grass];
                if (grassW < 0.35f) continue;

                float steep = td.GetSteepness(u, v);
                if (steep > 26f) continue;

                if (rng.NextDouble() > grassW) continue;

                int protoIdx = rng.Next(veg.trees.Count);
                float scale = 0.85f + (float)rng.NextDouble() * 0.4f;
                list.Add(new TreeInstance
                {
                    position = new Vector3(u, 0f, v),
                    prototypeIndex = protoIdx,
                    widthScale = scale,
                    heightScale = scale,
                    rotation = (float)(rng.NextDouble() * Mathf.PI * 2f),
                    color = Color.white,
                    lightmapColor = Color.white,
                });
            }
            td.SetTreeInstances(list.ToArray(), true);
            if (terrain != null) terrain.Flush();
        }

        static void PaintDetails(TerrainData td, VegetationSet veg, TerrainGeneratorSettings s)
        {
            int protoCount = td.detailPrototypes.Length;
            if (protoCount == 0) return;
            int detailRes = td.detailResolution;
            int aRes = td.alphamapResolution;
            float[,,] alpha = td.GetAlphamaps(0, 0, aRes, aRes);

            int grassLayers = veg.grasses.Count;
            int flowerLayers = veg.flowers.Count;
            var rng = new System.Random(s.seed ^ 0xD0F1);
            Vector2 noiseOffset = new Vector2((float)rng.NextDouble() * 500f, (float)rng.NextDouble() * 500f);

            for (int layer = 0; layer < protoCount; layer++)
            {
                bool isFlower = layer >= grassLayers;
                int density = isFlower ? s.flowerDensityPerCell : s.grassDensityPerCell;
                var map = new int[detailRes, detailRes];
                for (int y = 0; y < detailRes; y++)
                {
                    float v = (y + 0.5f) / detailRes;
                    int ay = Mathf.Clamp((int)(v * (aRes - 1)), 0, aRes - 1);
                    for (int x = 0; x < detailRes; x++)
                    {
                        float u = (x + 0.5f) / detailRes;
                        int ax = Mathf.Clamp((int)(u * (aRes - 1)), 0, aRes - 1);
                        float grassW = alpha[ay, ax, TerrainSplatmapGenerator.Grass];
                        if (grassW < 0.30f) continue;

                        float coverNoise = Mathf.PerlinNoise(
                            (u * s.terrainSize.x + noiseOffset.x + layer * 173f) * 0.02f,
                            (v * s.terrainSize.z + noiseOffset.y + layer * 173f) * 0.02f);
                        if (isFlower && coverNoise < 0.55f) continue;

                        float d = grassW * coverNoise * density;
                        map[y, x] = Mathf.Clamp((int)d, 0, density);
                    }
                }
                td.SetDetailLayer(0, 0, layer, map);
                _ = flowerLayers;
            }
        }

        static void PlaceBuildings(Terrain terrain, TerrainData td, VegetationSet veg,
            RoadPath main, List<RoadPath> muds, TerrainGeneratorSettings s)
        {
            if (veg.buildings.Count == 0 || s.buildingCount <= 0) return;
            var scene = terrain.gameObject.scene;
            GameObject root = FindOrCreateRoot(scene, terrain.transform);

            // Clear previous instances
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            var rng = new System.Random(s.seed ^ 0x8123);
            int attempts = s.buildingCount * 12;
            int placed = 0;

            float minRoadDist = s.asphaltHalfWidth + s.asphaltEdge + 4f;
            float maxRoadDist = 32f;
            float mmnr2 = minRoadDist * minRoadDist;
            float mxr2 = maxRoadDist * maxRoadDist;

            while (placed < s.buildingCount && attempts-- > 0)
            {
                float u = (float)rng.NextDouble();
                float v = (float)rng.NextDouble();
                float wx = u * td.size.x;
                float wz = v * td.size.z;
                float roadD2 = main.SqrDistanceTo(wx, wz);
                if (roadD2 < mmnr2 || roadD2 > mxr2) continue;
                bool onMud = false;
                for (int i = 0; i < muds.Count; i++)
                    if (muds[i].SqrDistanceTo(wx, wz) < (s.mudHalfWidth + 1f) * (s.mudHalfWidth + 1f)) { onMud = true; break; }
                if (onMud) continue;

                float steep = td.GetSteepness(u, v);
                if (steep > 8f) continue;

                var prefab = veg.buildings[rng.Next(veg.buildings.Count)];
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                float y = td.GetInterpolatedHeight(u, v) + terrain.transform.position.y;
                inst.transform.position = new Vector3(wx + terrain.transform.position.x, y, wz + terrain.transform.position.z);
                inst.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                inst.isStatic = true;
                placed++;
            }
        }

        static GameObject FindOrCreateRoot(Scene scene, Transform terrainTransform)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == BuildingsRootName) return root;
            var go = new GameObject(BuildingsRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(terrainTransform.parent, true);
            EditorSceneManager.MarkSceneDirty(scene);
            return go;
        }
    }
}
