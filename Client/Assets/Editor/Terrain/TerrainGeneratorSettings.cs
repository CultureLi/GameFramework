using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    [Serializable]
    public class RoadControlPoint
    {
        [Range(0f, 1f)] public float x = 0f;
        [Range(0f, 1f)] public float z = 0f;
    }

    public class TerrainGeneratorSettings : ScriptableObject
    {
        [Header("Terrain")]
        public Vector3 terrainSize = new Vector3(1000f, 60f, 1000f);
        public int heightmapResolution = 1025;
        public int alphamapResolution = 1024;
        public int detailResolution = 1024;
        public int detailResolutionPerPatch = 16;

        [Header("Noise")]
        public int seed = 20260903;
        [Range(1, 8)] public int octaves = 5;
        public float baseFrequency = 0.0018f;
        public float lacunarity = 2.05f;
        [Range(0f, 1f)] public float gain = 0.48f;
        [Range(0f, 200f)] public float domainWarp = 55f;
        [Range(0f, 1f)] public float heightMin = 0.18f;
        [Range(0f, 1f)] public float heightMax = 0.62f;

        [Header("Road")]
        public List<RoadControlPoint> mainRoad = new List<RoadControlPoint>
        {
            new RoadControlPoint { x = 0.02f, z = 0.10f },
            new RoadControlPoint { x = 0.28f, z = 0.35f },
            new RoadControlPoint { x = 0.55f, z = 0.55f },
            new RoadControlPoint { x = 0.78f, z = 0.72f },
            new RoadControlPoint { x = 0.98f, z = 0.92f },
        };
        [Range(2f, 20f)] public float asphaltHalfWidth = 5.0f;
        [Range(0.5f, 8f)] public float asphaltEdge = 2.2f;
        [Range(1, 4)] public int mudBranches = 2;
        [Range(1.5f, 10f)] public float mudHalfWidth = 2.4f;
        [Range(0.5f, 6f)] public float mudEdge = 1.8f;
        [Range(4f, 25f)] public float roadFlattenRadius = 9.0f;

        [Header("Splat weights")]
        [Range(0.05f, 1f)] public float grassSlopeThresholdDeg = 24f;
        [Range(0f, 1f)] public float desertBiasFromCenter = 0.55f;
        [Range(0f, 1f)] public float desertNoiseWeight = 0.45f;

        [Header("Vegetation")]
        [Range(0, 4000)] public int treeCount = 420;
        [Range(0, 32)] public int grassDensityPerCell = 6;
        [Range(0, 32)] public int flowerDensityPerCell = 2;
        [Range(0, 200)] public int buildingCount = 32;

        [Header("Regenerate switches")]
        public bool forceRegenerateAssets = false;
    }
}
