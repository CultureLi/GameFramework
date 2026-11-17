using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 可建造区域里的运行时建筑。记录所属区域、占用格子和已提交的格子坐标，
    /// 拖动时先用临时占位检查，松手后再提交（与内城 BuildingMoveCtrl 的流程一致）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityBuilding : MonoBehaviour
    {
        public int BuildingId;
        public Vector2Int FootprintCells = new Vector2Int(2, 2);
        public BuildableArea Area;

        /// <summary>已提交的 footprint 最小角格子。</summary>
        public int CellX;
        public int CellZ;

        /// <summary>拖动过程中的 footprint 最小角格子。</summary>
        public int TempCellX;
        public int TempCellZ;

        public Renderer Body;

        public Color NormalColor = new Color(0.82f, 0.72f, 0.5f, 1f);
        public Color AvailableColor = new Color(0.35f, 1f, 0.5f, 1f);
        public Color BlockedColor = new Color(1f, 0.3f, 0.3f, 1f);

        public void SetBodyColor(Color color)
        {
            if (Body != null)
                Body.material.color = color;
        }
    }
}
