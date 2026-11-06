using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    public sealed class MazeWall
    {
        public readonly List<Vector2> Points = new List<Vector2>();
    }

    public sealed class MazeGate
    {
        public Vector2 Position;
        /// <summary>关卡门朝向（墙的外法线）。门框本地 +Z 指向这个方向。</summary>
        public Vector2 Normal;
        public float Width;
    }

    /// <summary>
    /// 根据第二版手绘草图生成的螺旋迷宫：
    ///   - 一条从顶部外圈盘旋到中心左侧的连续螺旋墙；
    ///   - 螺旋上的 7 个绿色关卡门；
    ///   - 贯穿左右的中线墙、顶部/底部竖墙；
    ///   - 外围几条不规则山脊。
    ///
    /// 红色曲线 -> MountainWall（山体墙）；绿色方框 -> MazeGate（关卡门）。
    /// 坐标全部是地形世界坐标（米）。
    /// </summary>
    public sealed class MazeLayout
    {
        public Vector2 Center;
        public float Radius;
        public readonly List<MazeWall> Walls = new List<MazeWall>();
        public readonly List<MazeGate> Gates = new List<MazeGate>();

        // 关卡门在螺旋上的位置比例。对应草图里从外到内的 7 个绿色方框。
        private static readonly float[] SpiralGateTs = { 0.01f, 0.32f, 0.37f, 0.44f, 0.60f, 0.67f, 0.985f };

        public static MazeLayout Build(TerrainGeneratorSettings s)
        {
            var layout = new MazeLayout();
            float sx = s.terrainSize.x;
            float sz = s.terrainSize.z;
            var center = new Vector2(s.mazeCenter.x * sx, s.mazeCenter.y * sz);
            float radius = s.mazeRadius * Mathf.Min(sx, sz);
            layout.Center = center;
            layout.Radius = radius;

            // ---- 连续螺旋：从顶部外圈盘旋到中心左侧 ----
            var spiral = new MazeWall();
            float turns = Mathf.Max(1f, s.mazeSpiralTurns);
            int steps = Mathf.Max(64, Mathf.RoundToInt(48f * turns));
            float startAngle = -Mathf.PI * 0.5f;

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float r = Mathf.Lerp(radius, radius * 0.18f, t);
                float a = startAngle + turns * Mathf.PI * 2f * t;
                spiral.Points.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            layout.Walls.Add(spiral);

            // ---- 螺旋上的关卡门 ----
            foreach (float t in SpiralGateTs)
                AddGateOnPath(layout, spiral, t);

            // ---- 草图中贯穿左右的中线墙 ----
            // 端点取螺旋的最外侧交点，和草图一样正好接在外圈上，不穿进走廊。
            var leftCross = FindOuterCrossing(spiral, center, horizontal: true, negativeSide: true,
                fallback: new Vector2(center.x - radius * 0.90f, center.y));
            var rightCross = FindOuterCrossing(spiral, center, horizontal: true, negativeSide: false,
                fallback: new Vector2(center.x + radius * 0.90f, center.y));

            AddWall(layout, new List<Vector2> { new Vector2(0f, center.y), leftCross });
            AddWall(layout, new List<Vector2> { rightCross, new Vector2(sx, center.y) });

            // ---- 顶部/底部竖墙 ----
            var topCross = FindOuterCrossing(spiral, center, horizontal: false, negativeSide: true,
                fallback: new Vector2(center.x, center.y - radius * 0.90f));
            var bottomCross = FindOuterCrossing(spiral, center, horizontal: false, negativeSide: false,
                fallback: new Vector2(center.x, center.y + radius * 0.90f));

            AddWall(layout, new List<Vector2> { new Vector2(center.x, 0f), topCross });
            AddWall(layout, new List<Vector2> { bottomCross, new Vector2(center.x, sz) });

            // ---- 外围不规则山脊（草图边缘的红色曲线）----
            AddNormalizedPolyline(layout, sx, sz, new[]
            {
                new Vector2(0.27f, 0.11f), new Vector2(0.18f, 0.11f),
                new Vector2(0.09f, 0.16f), new Vector2(0.04f, 0.26f),
                new Vector2(0.05f, 0.36f)
            });
            AddNormalizedPolyline(layout, sx, sz, new[]
            {
                new Vector2(0.78f, 0.03f), new Vector2(0.86f, 0.06f),
                new Vector2(0.92f, 0.12f), new Vector2(0.97f, 0.22f),
                new Vector2(0.99f, 0.32f)
            });
            AddNormalizedPolyline(layout, sx, sz, new[]
            {
                new Vector2(0.08f, 0.55f), new Vector2(0.09f, 0.68f),
                new Vector2(0.15f, 0.80f), new Vector2(0.25f, 0.89f),
                new Vector2(0.35f, 0.97f)
            });
            AddNormalizedPolyline(layout, sx, sz, new[]
            {
                new Vector2(0.17f, 0.94f), new Vector2(0.20f, 0.98f),
                new Vector2(0.23f, 0.94f), new Vector2(0.27f, 0.98f),
                new Vector2(0.31f, 0.94f)
            });
            AddNormalizedPolyline(layout, sx, sz, new[]
            {
                new Vector2(0.78f, 0.95f), new Vector2(0.88f, 0.92f),
                new Vector2(0.94f, 0.86f), new Vector2(0.98f, 0.78f)
            });

            layout.FillGateWidths(s.mazeGateWidth);
            return layout;
        }

        private static void AddWall(MazeLayout layout, List<Vector2> points)
        {
            var wall = new MazeWall();
            wall.Points.AddRange(points);
            layout.Walls.Add(wall);
        }

        /// <summary>
        /// 找螺旋与中线/竖线的最外侧交点。草图里的横线、竖线都是停在外圈上，
        /// 如果直接用固定半径，墙会往走廊里多伸一截。
        /// 用线段求交，避免受螺旋采样密度影响。
        /// </summary>
        private static Vector2 FindOuterCrossing(
            MazeWall spiral, Vector2 center, bool horizontal, bool negativeSide, Vector2 fallback)
        {
            var best = fallback;
            float bestCoord = negativeSide ? float.MaxValue : float.MinValue;
            bool found = false;

            for (int i = 1; i < spiral.Points.Count; i++)
            {
                Vector2 a = spiral.Points[i - 1];
                Vector2 b = spiral.Points[i];

                if (horizontal)
                {
                    float da = a.y - center.y;
                    float db = b.y - center.y;
                    if (Mathf.Abs(db - da) < 1e-5f) continue;
                    if (da * db > 0f) continue;

                    float t = -da / (db - da);
                    float x = Mathf.Lerp(a.x, b.x, t);
                    if (negativeSide ? x >= center.x : x <= center.x) continue;
                    if (negativeSide ? x < bestCoord : x > bestCoord)
                    {
                        bestCoord = x;
                        best = new Vector2(x, center.y);
                        found = true;
                    }
                }
                else
                {
                    float da = a.x - center.x;
                    float db = b.x - center.x;
                    if (Mathf.Abs(db - da) < 1e-5f) continue;
                    if (da * db > 0f) continue;

                    float t = -da / (db - da);
                    float y = Mathf.Lerp(a.y, b.y, t);
                    if (negativeSide ? y >= center.y : y <= center.y) continue;
                    if (negativeSide ? y < bestCoord : y > bestCoord)
                    {
                        bestCoord = y;
                        best = new Vector2(center.x, y);
                        found = true;
                    }
                }
            }

            return found ? best : fallback;
        }

        private static void AddNormalizedPolyline(
            MazeLayout layout, float sx, float sz, Vector2[] normalized)
        {
            var wall = new MazeWall();
            foreach (var p in normalized)
                wall.Points.Add(new Vector2(p.x * sx, p.y * sz));
            layout.Walls.Add(wall);
        }

        private static void AddGate(MazeLayout layout, Vector2 position, Vector2 normal)
        {
            layout.Gates.Add(new MazeGate
            {
                Position = position,
                Normal = normal.sqrMagnitude > 1e-6f ? normal.normalized : Vector2.up,
                Width = 0f // Build 结束时统一填
            });
        }

        private static void AddGateOnPath(MazeLayout layout, MazeWall wall, float t)
        {
            int count = wall.Points.Count;
            if (count < 2) return;

            // 按采样索引取门位置（采样点本身就是等参数间隔），
            // 和草图里按角度/半径选的位置一致，不受螺旋弧长分布影响。
            float f = Mathf.Clamp01(t) * (count - 1);
            int i0 = Mathf.FloorToInt(f);
            int i1 = Mathf.Min(i0 + 1, count - 1);
            float u = f - i0;

            Vector2 position = Vector2.Lerp(wall.Points[i0], wall.Points[i1], u);
            Vector2 a = wall.Points[Mathf.Max(0, i0 - 1)];
            Vector2 b = wall.Points[Mathf.Min(count - 1, i1 + 1)];
            Vector2 tangent = (b - a).normalized;
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector2.right;

            AddGate(layout, position, new Vector2(-tangent.y, tangent.x));
        }

        /// <summary>在 Build 结束前按 settings 填门宽。</summary>
        public void FillGateWidths(float width)
        {
            foreach (var gate in Gates)
                gate.Width = width;
        }
    }
}
