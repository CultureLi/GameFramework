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
        [Tooltip("Y is the full vertical range. All height values below are normalized [0,1] fractions of it.")]
        public Vector3 terrainSize = new Vector3(1000f, 200f, 1000f);
        public int heightmapResolution = 1025;
        public int alphamapResolution = 1024;
        public int detailResolution = 1024;
        public int detailResolutionPerPatch = 16;

        [Header("Noise (shared)")]
        public int seed = 20260903;
        [Range(1, 8)] public int octaves = 5;
        public float lacunarity = 2.05f;
        [Range(0f, 1f)] public float gain = 0.48f;
        [Tooltip("Metres of XZ distortion applied before sampling noise. Makes ridges and escarpments meander instead of aligning to the terrain axes.")]
        [Range(0f, 200f)] public float domainWarp = 55f;

        [Header("Flat rim (map border)")]
        [Tooltip("Outer band, in metres, forced to height exactly 0 on all four sides.")]
        [Range(0f, 200f)] public float edgeZeroWidth = 30f;
        [Tooltip("Metres of smooth ramp inward from the flat rim back up to full terrain height. Too small = a visible wall at the rim.")]
        [Range(0f, 400f)] public float edgeBlendWidth = 90f;

        [Header("Layer 1 - plains")]
        [Tooltip("Base ground level as a fraction of terrainSize.y. The floor every other layer stacks on.")]
        [Range(0f, 0.5f)] public float plainsLevel = 0.040f;
        [Tooltip("+/- rolling relief on the plains, as a fraction of terrainSize.y.")]
        [Range(0f, 0.3f)] public float plainsRelief = 0.045f;
        [Tooltip("Noise frequency for the plains. 1/f is roughly the hill wavelength in metres.")]
        public float plainsFrequency = 0.0038f;

        [Header("Layer 2 - plateaus")]
        [Tooltip("Region-mask frequency. Lower = fewer, larger plateaus.")]
        public float plateauFrequency = 0.0022f;
        [Tooltip("Mask cut-off. Higher = less of the map becomes plateau.")]
        [Range(0f, 1f)] public float plateauThreshold = 0.45f;
        [Tooltip("Mask transition width. Narrow = steep escarpment walls, wide = gradual slopes.")]
        [Range(0.01f, 0.5f)] public float plateauEdgeWidth = 0.22f;
        [Tooltip("Height added at full plateau mask, as a fraction of terrainSize.y.")]
        [Range(0f, 0.8f)] public float plateauLift = 0.28f;
        [Tooltip("Number of flat benches stacked up the plateau. 1 = no terracing.")]
        [Range(1, 8)] public int plateauTerraces = 3;
        [Tooltip("How hard each bench is flattened. Higher = flatter shelves with steeper risers.")]
        [Range(1f, 12f)] public float plateauTerraceSharpness = 4.0f;
        [Tooltip("Fine relief on top of the benches so they are not perfectly flat.")]
        [Range(0f, 0.1f)] public float plateauDetail = 0.018f;

        [Header("Layer 3 - mountains")]
        [Tooltip("Region-mask frequency. Lower = fewer, larger massifs.")]
        public float mountainFrequency = 0.0018f;
        [Tooltip("Mask cut-off. Higher = smaller mountain footprint.")]
        [Range(0f, 1f)] public float mountainThreshold = 0.44f;
        [Tooltip("Mask transition width. Wide = long gentle foothills approaching the peaks.")]
        [Range(0.01f, 0.5f)] public float mountainEdgeWidth = 0.28f;
        [Tooltip("Peak height added at full mask and full ridge, as a fraction of terrainSize.y.")]
        [Range(0f, 1f)] public float mountainLift = 0.62f;
        [Tooltip("Ridged-noise frequency. 1/f is roughly the spacing between mountain spines in metres.")]
        public float ridgeFrequency = 0.0030f;
        [Range(1, 8)] public int ridgeOctaves = 6;
        [Tooltip("Exponent on the ridge value. >1 sharpens crests and deepens valleys.")]
        [Range(0.5f, 4f)] public float ridgeSharpness = 1.4f;

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
        [Tooltip("Narrow radius (m) in which the road surface itself is levelled flat.")]
        [Range(4f, 25f)] public float roadFlattenRadius = 9.0f;
        [Tooltip("Wide radius (m) in which plateau and mountain lift are suppressed, so the road routes through a valley / mountain pass instead of climbing over the massif.")]
        [Range(20f, 400f)] public float roadCorridorRadius = 110f;
        [Tooltip("How completely the corridor removes plateau / mountain lift at the road centre. 1 = fully flattened to plains level.")]
        [Range(0f, 1f)] public float roadCarveStrength = 0.92f;

        [Header("Splat weights")]
        [Tooltip("Slope (degrees) above which grass stops growing and the surface reads as bare desert/rock.")]
        [Range(5f, 60f)] public float grassSlopeThresholdDeg = 24f;
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
