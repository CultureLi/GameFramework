using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// 新矩形迷宫草图的布局数据。
    ///
    /// 草图尺寸 1182×806 像素，映射到 1000×1000 世界：
    ///   世界 X = 像素 X * (1000 / 1182)
    ///   世界 Z = 像素 Y * (1000 / 1182) + 垂直居中偏移
    ///
    /// 红色线是围墙（AssetPathCfg 301 / Wall_1），绿色框是关卡。
    /// </summary>
    public static class RectMazeLayout
    {
        public const float ImageWidth = 1182f;
        public const float ImageHeight = 806f;
        public const float WorldSize = 1000f;
        public const float PixelToWorld = WorldSize / ImageWidth;
        public static readonly float WorldOffsetZ = (WorldSize - ImageHeight * PixelToWorld) * 0.5f;

        /// <summary>
        /// 绿色关卡顺序：外圈左上、右上、左下、右下，内圈左、右，中心下方。
        /// 场景里 ConfigId 1~7 的关卡按这个顺序落位。
        /// </summary>
        public static readonly RectMazeGateSpec[] Gates =
        {
            new RectMazeGateSpec(294.685f, 90.581f, 93f, 37f, false),
            new RectMazeGateSpec(921.500f, 86.500f, 93f, 47f, false),
            new RectMazeGateSpec(323.228f, 679.519f, 93f, 41f, false),
            new RectMazeGateSpec(892.000f, 677.500f, 88f, 35f, false),
            new RectMazeGateSpec(367.500f, 461.500f, 35f, 83f, true),
            new RectMazeGateSpec(798.000f, 366.000f, 42f, 98f, true),
            new RectMazeGateSpec(572.500f, 436.000f, 79f, 34f, false),
        };

        /// <summary>
        /// 红色围墙线段，坐标是草图像素。绿色框覆盖的位置已经留空。
        /// </summary>
        public static readonly RectMazeWallSegment[] Walls =
        {
            // 外圈上边，给左上/右上关卡留口
            new RectMazeWallSegment(184f, 85f, 247f, 85f),
            new RectMazeWallSegment(340f, 85f, 875f, 85f),
            new RectMazeWallSegment(968f, 85f, 973f, 85f),

            // 外圈下边，给左下/右下关卡留口
            new RectMazeWallSegment(184f, 675f, 277f, 675f),
            new RectMazeWallSegment(370f, 675f, 848f, 675f),
            new RectMazeWallSegment(936f, 675f, 973f, 675f),

            // 外圈左右
            new RectMazeWallSegment(184f, 85f, 184f, 675f),
            new RectMazeWallSegment(973f, 85f, 973f, 675f),

            // 外围延伸墙
            new RectMazeWallSegment(0f, 367f, 184f, 367f),
            new RectMazeWallSegment(973f, 375f, 1182f, 375f),
            new RectMazeWallSegment(590f, 0f, 590f, 85f),
            new RectMazeWallSegment(566f, 675f, 566f, 806f),

            // 内圈上下
            new RectMazeWallSegment(362f, 198f, 795f, 198f),
            new RectMazeWallSegment(362f, 549f, 795f, 549f),

            // 内圈左右，给内圈左/右关卡留口
            new RectMazeWallSegment(362f, 198f, 362f, 420f),
            new RectMazeWallSegment(362f, 503f, 362f, 549f),
            new RectMazeWallSegment(795f, 198f, 795f, 317f),
            new RectMazeWallSegment(795f, 415f, 795f, 549f),

            // 中心小圈
            new RectMazeWallSegment(484f, 290f, 655f, 290f),
            new RectMazeWallSegment(484f, 434f, 533f, 434f),
            new RectMazeWallSegment(612f, 434f, 655f, 434f),
            new RectMazeWallSegment(484f, 290f, 484f, 434f),
            new RectMazeWallSegment(655f, 290f, 655f, 434f),
        };

        public static Vector2 ToWorld(float pixelX, float pixelY)
        {
            return new Vector2(
                pixelX * PixelToWorld,
                pixelY * PixelToWorld + WorldOffsetZ);
        }
    }

    public sealed class RectMazeGateSpec
    {
        public readonly float CenterX;
        public readonly float CenterY;
        public readonly float SizeX;
        public readonly float SizeY;
        public readonly bool Vertical;

        public RectMazeGateSpec(float centerX, float centerY, float sizeX, float sizeY, bool vertical)
        {
            CenterX = centerX;
            CenterY = centerY;
            SizeX = sizeX;
            SizeY = sizeY;
            Vertical = vertical;
        }

        public Vector2 WorldCenter => RectMazeLayout.ToWorld(CenterX, CenterY);

        public float WorldLength =>
            (Vertical ? SizeY : SizeX) * RectMazeLayout.PixelToWorld;

        public float RotationY => Vertical ? 90f : 0f;
    }

    public readonly struct RectMazeWallSegment
    {
        public readonly float X0;
        public readonly float Y0;
        public readonly float X1;
        public readonly float Y1;

        public RectMazeWallSegment(float x0, float y0, float x1, float y1)
        {
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
        }

        public Vector2 WorldStart => RectMazeLayout.ToWorld(X0, Y0);
        public Vector2 WorldEnd => RectMazeLayout.ToWorld(X1, Y1);
        public bool IsHorizontal => Mathf.Abs(Y1 - Y0) < 0.001f;
    }
}
