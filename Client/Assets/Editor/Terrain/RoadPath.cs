using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    public class RoadPath
    {
        public readonly Vector2[] samples;
        public readonly float totalLength;
        public readonly float halfWidth;
        public readonly float edge;

        RoadPath(Vector2[] samples, float halfWidth, float edge)
        {
            this.samples = samples;
            this.halfWidth = halfWidth;
            this.edge = edge;
            totalLength = 0f;
            for (int i = 1; i < samples.Length; i++) totalLength += Vector2.Distance(samples[i - 1], samples[i]);
        }

        public static RoadPath BuildMain(TerrainGeneratorSettings s)
        {
            var cps = new Vector2[s.mainRoad.Count];
            for (int i = 0; i < cps.Length; i++)
                cps[i] = new Vector2(s.mainRoad[i].x * s.terrainSize.x, s.mainRoad[i].z * s.terrainSize.z);
            return new RoadPath(SampleCatmullRom(cps, 32), s.asphaltHalfWidth, s.asphaltEdge);
        }

        public static List<RoadPath> BuildMudBranches(TerrainGeneratorSettings s, RoadPath main)
        {
            var result = new List<RoadPath>();
            var rng = new System.Random(s.seed ^ 0x51EDB0B);
            for (int i = 0; i < s.mudBranches; i++)
            {
                // Anchor: sample somewhere along main road
                float t = (i + 1f) / (s.mudBranches + 1f);
                Vector2 anchor = SampleAt(main.samples, t);
                // Endpoint: random spot off the main road
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float dist = 60f + (float)rng.NextDouble() * 180f;
                Vector2 end = anchor + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                end.x = Mathf.Clamp(end.x, 0f, s.terrainSize.x);
                end.y = Mathf.Clamp(end.y, 0f, s.terrainSize.z);
                Vector2 mid = Vector2.Lerp(anchor, end, 0.5f) +
                              new Vector2((float)(rng.NextDouble() - 0.5) * 30f,
                                          (float)(rng.NextDouble() - 0.5) * 30f);
                var cps = new[] { anchor, mid, end };
                result.Add(new RoadPath(SampleCatmullRom(cps, 20), s.mudHalfWidth, s.mudEdge));
            }
            return result;
        }

        // Returns squared distance from (x,z) to nearest point on this path.
        public float SqrDistanceTo(float x, float z)
        {
            float best = float.PositiveInfinity;
            var p = new Vector2(x, z);
            for (int i = 1; i < samples.Length; i++)
            {
                float d = SqrDistancePointSegment(p, samples[i - 1], samples[i]);
                if (d < best) best = d;
            }
            return best;
        }

        static float SqrDistancePointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return (p - a).sqrMagnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            Vector2 c = a + ab * t;
            return (p - c).sqrMagnitude;
        }

        static Vector2 SampleAt(Vector2[] samples, float t)
        {
            float f = Mathf.Clamp01(t) * (samples.Length - 1);
            int i0 = Mathf.FloorToInt(f);
            int i1 = Mathf.Min(i0 + 1, samples.Length - 1);
            return Vector2.Lerp(samples[i0], samples[i1], f - i0);
        }

        static Vector2[] SampleCatmullRom(Vector2[] cps, int segmentsPerSpan)
        {
            if (cps.Length < 2) return cps;
            var padded = new Vector2[cps.Length + 2];
            padded[0] = cps[0] + (cps[0] - cps[1]);
            padded[padded.Length - 1] = cps[cps.Length - 1] + (cps[cps.Length - 1] - cps[cps.Length - 2]);
            for (int i = 0; i < cps.Length; i++) padded[i + 1] = cps[i];

            int spans = cps.Length - 1;
            var result = new List<Vector2>(spans * segmentsPerSpan + 1);
            for (int span = 0; span < spans; span++)
            {
                Vector2 p0 = padded[span];
                Vector2 p1 = padded[span + 1];
                Vector2 p2 = padded[span + 2];
                Vector2 p3 = padded[span + 3];
                int steps = (span == spans - 1) ? segmentsPerSpan + 1 : segmentsPerSpan;
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)segmentsPerSpan;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    Vector2 pos = 0.5f * (
                        (2f * p1) +
                        (-p0 + p2) * t +
                        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                        (-p0 + 3f * p1 - 3f * p2 + p3) * t3
                    );
                    result.Add(pos);
                }
            }
            return result.ToArray();
        }
    }
}
