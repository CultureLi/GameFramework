using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 运行时的一件地图实体。Index 是合并后的列表下标，用来在四叉树里存放对象本身。
    /// </summary>
    public sealed class MapRuntimeItem
    {
        public int Index;
        public int ConfigId;
        public MapEntityCategory Category;
        public Vector2 Pos;
        public Vector2 Size;
        public float RotationY;
        public string AssetPath;
    }
}
