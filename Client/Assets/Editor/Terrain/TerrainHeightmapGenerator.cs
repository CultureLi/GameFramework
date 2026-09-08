using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// Layered landform synthesis. Three additive layers stack on top of each other so a single
    /// map can hold visibly different terrain types instead of one uniform noise field:
    ///
    ///   1. Plains   — gentle rolling fBm covering the whole map (the floor everything sits on).
    ///   2. Plateaus — a soft-edged region mask, terraced into flat benches with steep escarpments.
    ///   3. Mountains— ridged fBm (Perlin folded at its midline) inside its own region mask,
    ///                 squared so foothills ramp up instead of forming a wall at the mask edge.
    ///
    /// Roads then carve a wide corridor that suppresses layers 2 and 3, so the road always finds a
    /// pass through the mountains rather than climbing over them, plus a narrow flatten pass that
    /// levels the actual driving surface.
    ///
    /// Finally the outer <see cref="TerrainGeneratorSettings.edgeZeroWidth"/> metres of the map are
    /// forced to height 0 with a smooth ramp inward, giving a flat rim on all four sides.
    /// </summary>
    public static class TerrainHeightmapGenerator
    {
        public static void Generate(TerrainData td, TerrainGeneratorSettings s, RoadPath main, List<RoadPath> muds)
        {
            int res = s.heightmapResolution;
            td.heightmapResolution = res;
            td.size = s.terrainSize;

            var rng = new System.Random(s.seed);
            Vector2 oWarpA = RandOffset(rng);
            Vector2 oWarpB = RandOffset(rng);
            Vector2 oPlains = RandOffset(rng);
            Vector2 oPlateau = RandOffset(rng);
            Vector2 oMountain = RandOffset(rng);
            Vector2 oRidge = RandOffset(rng);
            Vector2 oDetail = RandOffset(rng);

            float[,] heights = new float[res, res];
            float sx = s.terrainSize.x / (res - 1);
            float sz = s.terrainSize.z / (res - 1);

            float corridorR = Mathf.Max(1f, s.roadCorridorRadius);
            float corridorR2 = corridorR * corridorR;
            float flattenR = Mathf.Max(1f, s.roadFlattenRadius);
            float carve = Mathf.Clamp01(s.roadCarveStrength);

            for (int y = 0; y < res; y++)
            {
                float wz = y * sz;
                if ((y & 63) == 0)
                    EditorUtility.DisplayProgressBar("OpenWorld Terrain",
                        $"Sculpting heightmap... {y * 100 / res}%", 0.30f + 0.18f * y / res);

                for (int x = 0; x < res; x++)
                {
                    float wx = x * sx;

                    // Domain warp — breaks up the grid-aligned look of raw Perlin so ridges and
                    // escarpments meander instead of running parallel to the terrain axes.
                    float wxw = wx + (Mathf.PerlinNoise((wx + oWarpA.x) * 0.0030f, (wz + oWarpA.y) * 0.0030f) - 0.5f) * s.domainWarp;
                    float wzw = wz + (Mathf.PerlinNoise((wx + oWarpB.x) * 0.0030f, (wz + oWarpB.y) * 0.0030f) - 0.5f) * s.domainWarp;

                    // ---- road proximity ------------------------------------------------------
                    float roadSqr = main.SqrDistanceTo(wx, wz);
                    if (muds != null)
                    {
                        for (int m = 0; m < muds.Count; m++)
                        {
                            float d = muds[m].SqrDistanceTo(wx, wz);
                            if (d < roadSqr) roadSqr = d;
                        }
                    }
                    float roadDist = Mathf.Sqrt(roadSqr);
                    float corridor = roadSqr < corridorR2
                        ? 1f - Mathf.SmoothStep(0f, 1f, roadDist / corridorR)
                        : 0f;
                    float bigFeatureGate = 1f - corridor * carve;

                    // ---- layer 1: plains ----------------------------------------------------
                    float plainsN = Fbm(wxw, wzw, s.plainsFrequency, s.octaves, s.lacunarity, s.gain, oPlains);
                    float h = s.plainsLevel + (plainsN * 2f - 1f) * s.plainsRelief;

                    // ---- region masks -------------------------------------------------------
                    // Mountains win over plateaus where they overlap. Two reasons: a terraced mesa
                    // buried under a ridged massif reads as noise, and stacking both lifts can push
                    // the total past 1.0 where Clamp01 would shear peaks into flat tops.
                    float mountainN = Fbm(wxw, wzw, s.mountainFrequency, 3, s.lacunarity, 0.5f, oMountain);
                    float mountainMask = Mask(mountainN, s.mountainThreshold, s.mountainEdgeWidth) * bigFeatureGate;

                    float plateauN = Fbm(wxw, wzw, s.plateauFrequency, 3, s.lacunarity, 0.5f, oPlateau);
                    float plateauMask = Mask(plateauN, s.plateauThreshold, s.plateauEdgeWidth) * bigFeatureGate;
                    plateauMask *= 1f - mountainMask;

                    // ---- layer 2: plateaus --------------------------------------------------
                    if (plateauMask > 0.001f)
                    {
                        float bench = Terrace(plateauMask, s.plateauTerraces, s.plateauTerraceSharpness);
                        float detail = Mathf.PerlinNoise((wxw + oDetail.x) * 0.010f, (wzw + oDetail.y) * 0.010f) * 2f - 1f;
                        h += bench * s.plateauLift + bench * detail * s.plateauDetail;
                    }

                    // ---- layer 3: mountains -------------------------------------------------
                    if (mountainMask > 0.001f)
                    {
                        float ridge = RidgedFbm(wxw, wzw, s.ridgeFrequency, s.ridgeOctaves, s.lacunarity, s.gain, oRidge);
                        ridge = Mathf.Pow(ridge, Mathf.Max(0.1f, s.ridgeSharpness));
                        // mask² -> the massif rises through foothills rather than from a cliff.
                        h += mountainMask * mountainMask * ridge * s.mountainLift;
                    }

                    // ---- road surface flatten (narrow) --------------------------------------
                    // The corridor above already removed plateau/mountain lift here, so this only
                    // has to iron out the remaining plains ripple to leave a drivable ribbon.
                    if (roadDist < flattenR)
                    {
                        float pull = 1f - Mathf.SmoothStep(0f, 1f, roadDist / flattenR);
                        float roadTarget = s.plainsLevel + (plainsN * 2f - 1f) * s.plainsRelief * 0.25f;
                        h = Mathf.Lerp(h, roadTarget, pull * 0.9f);
                    }

                    // ---- flat rim ----------------------------------------------------------
                    h *= EdgeFalloff(wx, wz, s);

                    heights[y, x] = Mathf.Clamp01(h);
                }
            }

            td.SetHeights(0, 0, heights);
        }

        // ------------------------------------------------------------------ helpers

        static Vector2 RandOffset(System.Random rng) =>
            new Vector2((float)rng.NextDouble() * 10000f, (float)rng.NextDouble() * 10000f);

        /// <summary>
        /// Soft region mask: 0 below <paramref name="threshold"/> - width/2, 1 above
        /// + width/2, smooth in between. A narrow width gives steep escarpments, a wide one
        /// gives gradual foothills.
        /// </summary>
        static float Mask(float n, float threshold, float width)
        {
            float w = Mathf.Max(1e-4f, width);
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((n - threshold) / w + 0.5f));
        }

        /// <summary>
        /// Quantizes <paramref name="t"/> into <paramref name="steps"/> benches. The fractional
        /// part is squashed toward its midpoint, so each step is a flat shelf joined by a short
        /// steep riser — the classic mesa / plateau silhouette.
        /// </summary>
        static float Terrace(float t, int steps, float sharpness)
        {
            if (steps <= 1) return t;
            float u = Mathf.Clamp01(t) * steps;
            float f = Mathf.Floor(u);
            float frac = u - f;
            if (f >= steps) { f = steps - 1f; frac = 1f; }
            float k = Mathf.Max(1f, sharpness);
            frac = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((frac - 0.5f) * k + 0.5f));
            return (f + frac) / steps;
        }

        /// <summary>Standard fBm in [0,1].</summary>
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

        /// <summary>
        /// Ridged fBm: fold each Perlin octave at its midline (1 - |2n-1|) so the former
        /// mid-value becomes a sharp crest, then square it to tighten the ridge. Produces
        /// connected mountain spines instead of the blobby hills plain fBm gives.
        /// </summary>
        static float RidgedFbm(float x, float y, float freq, int oct, float lac, float gain, Vector2 seedOffset)
        {
            float sum = 0f;
            float amp = 1f;
            float maxAmp = 0f;
            float f = freq;
            for (int i = 0; i < oct; i++)
            {
                float n = Mathf.PerlinNoise(x * f + seedOffset.x, y * f + seedOffset.y);
                float r = 1f - Mathf.Abs(n * 2f - 1f);
                sum += r * r * amp;
                maxAmp += amp;
                amp *= gain;
                f *= lac;
            }
            return Mathf.Clamp01(sum / Mathf.Max(1e-6f, maxAmp));
        }

        /// <summary>
        /// 0 inside the outer <see cref="TerrainGeneratorSettings.edgeZeroWidth"/> metres of the
        /// map (hard flat rim), ramping smoothly to 1 over the next
        /// <see cref="TerrainGeneratorSettings.edgeBlendWidth"/> metres.
        /// </summary>
        static float EdgeFalloff(float wx, float wz, TerrainGeneratorSettings s)
        {
            float zero = Mathf.Max(0f, s.edgeZeroWidth);
            float blend = Mathf.Max(0f, s.edgeBlendWidth);
            float d = Mathf.Min(
                Mathf.Min(wx, s.terrainSize.x - wx),
                Mathf.Min(wz, s.terrainSize.z - wz));
            if (d <= zero) return 0f;
            if (blend <= 0f) return 1f;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - zero) / blend));
        }
    }
}
