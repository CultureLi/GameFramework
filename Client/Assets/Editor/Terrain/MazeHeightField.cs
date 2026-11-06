using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// 把 MazeLayout 的折线墙画成高度场。为了让 1025² 的循环不被数百条线段拖慢，
    /// 这里先按线段包围盒把墙直接栅格化到 float[res,res]，再在主高度循环里取 max。
    ///
    /// 返回的是相对 plainsLevel 的归一化高度（0~1）。
    /// </summary>
    public static class MazeHeightField
    {
        public static float[,] Build(TerrainGeneratorSettings s, MazeLayout layout, int res)
        {
            var field = new float[res, res];
            float sx = s.terrainSize.x / Mathf.Max(1, res - 1);
            float sz = s.terrainSize.z / Mathf.Max(1, res - 1);

            float halfWidth = Mathf.Max(0.5f, s.mazeWallHalfWidth);
            float falloff = Mathf.Max(0.5f, s.mazeWallFalloff);
            float outer = halfWidth + falloff;
            float height = Mathf.Clamp01(s.mazeWallHeight);
            float noiseAmount = Mathf.Clamp01(s.mazeWallNoise);
            float seedX = (s.seed % 10000) * 0.017f;
            float seedY = (s.seed % 10000) * 0.031f;

            foreach (var wall in layout.Walls)
            {
                for (int i = 1; i < wall.Points.Count; i++)
                {
                    Vector2 a = wall.Points[i - 1];
                    Vector2 b = wall.Points[i];

                    int x0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, b.x) - outer) / sx), 0, res - 1);
                    int x1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x, b.x) + outer) / sx), 0, res - 1);
                    int y0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.y, b.y) - outer) / sz), 0, res - 1);
                    int y1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.y, b.y) + outer) / sz), 0, res - 1);

                    for (int y = y0; y <= y1; y++)
                    {
                        float wz = y * sz;
                        for (int x = x0; x <= x1; x++)
                        {
                            float wx = x * sx;
                            float distance = DistancePointSegment(wx, wz, a, b);
                            if (distance >= outer) continue;

                            float profile = Mathf.Pow(1f - distance / outer, 1.6f);
                            float noise = Mathf.PerlinNoise(wx * 0.035f + seedX, wz * 0.035f + seedY);
                            float scale = Mathf.Clamp(1f + (noise * 2f - 1f) * noiseAmount, 0.15f, 1.6f);
                            float value = profile * height * scale;
                            if (value > field[y, x]) field[y, x] = value;
                        }
                    }
                }
            }

            // 关卡门所在位置把山体压回地面，形成山口；门框长度与山口宽度一致。
            foreach (var gate in layout.Gates)
            {
                float radius = gate.Width * 0.5f;
                if (radius <= 0.01f) continue;

                float blend = Mathf.Min(1.5f, radius * 0.25f);
                float maxDistance = radius + blend;

                int x0 = Mathf.Clamp(Mathf.FloorToInt((gate.Position.x - maxDistance) / sx), 0, res - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((gate.Position.x + maxDistance) / sx), 0, res - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt((gate.Position.y - maxDistance) / sz), 0, res - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt((gate.Position.y + maxDistance) / sz), 0, res - 1);

                for (int y = y0; y <= y1; y++)
                {
                    float wz = y * sz;
                    for (int x = x0; x <= x1; x++)
                    {
                        float wx = x * sx;
                        float dx = wx - gate.Position.x;
                        float dz = wz - gate.Position.y;
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);
                        if (distance >= maxDistance) continue;

                        float keep = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distance - radius) / blend));
                        field[y, x] *= keep;
                    }
                }
            }

            return field;
        }

        private static float DistancePointSegment(float px, float pz, Vector2 a, Vector2 b)
        {
            float abx = b.x - a.x;
            float abz = b.y - a.y;
            float apx = px - a.x;
            float apz = pz - a.y;
            float lengthSq = abx * abx + abz * abz;

            if (lengthSq < 1e-6f)
                return Mathf.Sqrt(apx * apx + apz * apz);

            float t = Mathf.Clamp01((apx * abx + apz * abz) / lengthSq);
            float cx = a.x + abx * t;
            float cz = a.y + abz * t;
            float dx = px - cx;
            float dz = pz - cz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
