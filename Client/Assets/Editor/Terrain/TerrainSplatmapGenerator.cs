using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    public static class TerrainSplatmapGenerator
    {
        public const int Desert = 0;
        public const int Grass = 1;
        public const int Mud = 2;
        public const int Asphalt = 3;

        public static void PaintSplatmap(TerrainData td, RoadPath main, List<RoadPath> muds, TerrainGeneratorSettings s)
        {
            int res = s.alphamapResolution;
            td.alphamapResolution = res;

            float[,,] alpha = new float[res, res, 4];
            var rng = new System.Random(s.seed ^ 0x5A17);
            Vector2 dryOff = new Vector2((float)rng.NextDouble() * 5000f, (float)rng.NextDouble() * 5000f);

            float sizeX = s.terrainSize.x;
            float sizeZ = s.terrainSize.z;
            Vector2 center = new Vector2(sizeX * 0.5f, sizeZ * 0.5f);
            float maxDistFromCenter = center.magnitude;
            float slopeThresholdCos = Mathf.Cos(s.grassSlopeThresholdDeg * Mathf.Deg2Rad);

            float asphaltFar = s.asphaltHalfWidth + s.asphaltEdge;
            float asphaltFarSq = asphaltFar * asphaltFar;
            float asphaltNearSq = s.asphaltHalfWidth * s.asphaltHalfWidth;

            for (int y = 0; y < res; y++)
            {
                float wz = (y + 0.5f) / res * sizeZ;
                for (int x = 0; x < res; x++)
                {
                    float wx = (x + 0.5f) / res * sizeX;

                    // Sample terrain slope & height (use interpolated).
                    float nu = wx / sizeX;
                    float nv = wz / sizeZ;
                    float steepness = td.GetSteepness(nu, nv);
                    float slopeFactor = Mathf.Clamp01(1f - steepness / 60f); // 0 at ~60 deg, 1 at flat
                    float slopeGrassGate = Mathf.SmoothStep(0f, 1f, (s.grassSlopeThresholdDeg - steepness) / 8f);
                    slopeGrassGate = Mathf.Clamp01(slopeGrassGate);
                    _ = slopeThresholdCos;
                    float heightNorm = td.GetInterpolatedHeight(nu, nv) / Mathf.Max(0.01f, td.size.y);

                    // Road distances (squared for cheap compare, sqrt only when needed)
                    float asphaltSqr = main.SqrDistanceTo(wx, wz);
                    float mudSqr = float.PositiveInfinity;
                    for (int i = 0; i < muds.Count; i++)
                    {
                        float d = muds[i].SqrDistanceTo(wx, wz);
                        if (d < mudSqr) mudSqr = d;
                    }

                    float wAsphalt = 0f;
                    if (asphaltSqr < asphaltFarSq)
                    {
                        float d = Mathf.Sqrt(asphaltSqr);
                        wAsphalt = 1f - Mathf.SmoothStep(s.asphaltHalfWidth, asphaltFar, d);
                    }

                    float mudFar = s.mudHalfWidth + s.mudEdge;
                    float wMud = 0f;
                    if (mudSqr < mudFar * mudFar)
                    {
                        float d = Mathf.Sqrt(mudSqr);
                        wMud = 1f - Mathf.SmoothStep(s.mudHalfWidth, mudFar, d);
                    }
                    // Asphalt fringe bleeds a bit of mud so the transition looks natural.
                    if (asphaltSqr > asphaltNearSq && asphaltSqr < (asphaltFar + 2f) * (asphaltFar + 2f))
                    {
                        float d = Mathf.Sqrt(asphaltSqr);
                        float fringe = 1f - Mathf.SmoothStep(asphaltFar - 0.5f, asphaltFar + 2f, d);
                        fringe *= (1f - wAsphalt);
                        wMud = Mathf.Max(wMud, fringe * 0.55f);
                    }
                    wMud = Mathf.Clamp01(wMud * (1f - wAsphalt));

                    float remain = Mathf.Max(0f, 1f - wAsphalt - wMud);

                    // Desert bias: distance from map center + dryness noise + high slope (rocks/dune).
                    float centerDist01 = Mathf.Clamp01(Vector2.Distance(new Vector2(wx, wz), center) / maxDistFromCenter);
                    float dry = Mathf.PerlinNoise((wx + dryOff.x) * 0.0025f, (wz + dryOff.y) * 0.0025f);
                    float desertBias = Mathf.Clamp01(
                        centerDist01 * s.desertBiasFromCenter +
                        dry * s.desertNoiseWeight +
                        Mathf.Clamp01((steepness - 22f) / 30f) * 0.2f);

                    float wGrass = remain * slopeGrassGate * (1f - desertBias * 0.7f);
                    float wDesert = remain - wGrass;
                    wDesert = Mathf.Clamp01(wDesert);

                    // Extra: on very steep slopes grass falls off entirely to desert.
                    if (steepness > 34f)
                    {
                        wDesert += wGrass * 0.6f;
                        wGrass *= 0.4f;
                    }
                    _ = slopeFactor;
                    _ = heightNorm;

                    float sum = wAsphalt + wMud + wGrass + wDesert;
                    if (sum < 1e-4f) { wGrass = 1f; sum = 1f; }
                    float inv = 1f / sum;
                    alpha[y, x, Desert] = wDesert * inv;
                    alpha[y, x, Grass] = wGrass * inv;
                    alpha[y, x, Mud] = wMud * inv;
                    alpha[y, x, Asphalt] = wAsphalt * inv;
                }
            }

            td.SetAlphamaps(0, 0, alpha);
        }
    }
}
