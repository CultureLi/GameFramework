using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    public static class TerrainHeightmapGenerator
    {
        public static void Generate(TerrainData td, TerrainGeneratorSettings s, RoadPath main, List<RoadPath> muds)
        {
            int res = s.heightmapResolution;
            td.heightmapResolution = res;
            td.size = s.terrainSize;

            var rng = new System.Random(s.seed);
            Vector2 offsetA = new Vector2((float)rng.NextDouble() * 10000f, (float)rng.NextDouble() * 10000f);
            Vector2 offsetB = new Vector2((float)rng.NextDouble() * 10000f, (float)rng.NextDouble() * 10000f);

            float[,] heights = new float[res, res];
            float mid = 0.5f * (s.heightMin + s.heightMax);
            float amp = 0.5f * (s.heightMax - s.heightMin);
            float sx = s.terrainSize.x / (res - 1);
            float sz = s.terrainSize.z / (res - 1);
            float flattenR2 = s.roadFlattenRadius * s.roadFlattenRadius;

            for (int y = 0; y < res; y++)
            {
                float wz = y * sz;
                for (int x = 0; x < res; x++)
                {
                    float wx = x * sx;

                    // domain warp
                    float wxw = wx + (Mathf.PerlinNoise((wx + offsetA.x) * 0.003f, (wz + offsetA.y) * 0.003f) - 0.5f) * s.domainWarp;
                    float wzw = wz + (Mathf.PerlinNoise((wx + offsetB.x) * 0.003f, (wz + offsetB.y) * 0.003f) - 0.5f) * s.domainWarp;

                    float n = Fbm(wxw, wzw, s.baseFrequency, s.octaves, s.lacunarity, s.gain, offsetA);
                    float h = mid + (n * 2f - 1f) * amp;

                    // Road flatten: pull height to `mid` within radius; smooth falloff outside up to flatten radius.
                    float roadSqr = main.SqrDistanceTo(wx, wz);
                    if (muds != null)
                    {
                        for (int m = 0; m < muds.Count; m++)
                        {
                            float d = muds[m].SqrDistanceTo(wx, wz);
                            if (d < roadSqr) roadSqr = d;
                        }
                    }
                    if (roadSqr < flattenR2)
                    {
                        float d01 = Mathf.Sqrt(roadSqr) / s.roadFlattenRadius;
                        float pull = 1f - Mathf.SmoothStep(0f, 1f, d01);
                        h = Mathf.Lerp(h, mid, pull * 0.9f);
                    }

                    // clamp to [0,1] terrain space
                    heights[y, x] = Mathf.Clamp01(h);
                }
            }

            td.SetHeights(0, 0, heights);
        }

        static float Fbm(float x, float y, float freq, int oct, float lac, float gain, Vector2 seedOffset)
        {
            float sum = 0f;
            float amp = 1f;
            float maxAmp = 0f;
            float f = freq;
            for (int i = 0; i < oct; i++)
            {
                sum += Mathf.PerlinNoise(x * f + seedOffset.x, y * f + seedOffset.y) * amp;
                maxAmp += amp;
                amp *= gain;
                f *= lac;
            }
            return sum / Mathf.Max(1e-6f, maxAmp);
        }
    }
}
