using UnityEngine;

namespace GameMain
{
    public class CityGridView : MonoBehaviour
    {
        [SerializeField] private int gridCellCount = 20;
        [SerializeField] private int cornerCellCount = 0;
        [SerializeField] private float cellWidth = 1f;
        [SerializeField] private Renderer groundRenderer;

        private CityGrid _grid;
        private int _halfCellCount;

        public int GridCellCount => gridCellCount;
        public float CellWidth => cellWidth;

        private void Awake()
        {
            _halfCellCount = gridCellCount / 2;
            _grid = new CityGrid(gridCellCount, cornerCellCount);

            var gridWidth = gridCellCount * cellWidth;
            var scale = transform.localScale;
            scale.x = gridWidth;
            scale.y = gridWidth;
            transform.localScale = scale;

            if (groundRenderer != null)
                groundRenderer.material.SetFloat("_CellCount", gridCellCount);
        }

        public (int x, int z) WorldToCell(Vector3 worldPos)
        {
            var local = Quaternion.Inverse(transform.rotation) * (worldPos - transform.position);
            var x = Mathf.FloorToInt(local.x / cellWidth) + _halfCellCount;
            var z = Mathf.FloorToInt(local.y / cellWidth) + _halfCellCount;
            return (x, z);
        }

        public Vector3 CellToWorld(int x, int z)
        {
            var localX = (x - _halfCellCount) * cellWidth;
            var localZ = (z - _halfCellCount) * cellWidth;
            return transform.position + transform.rotation * new Vector3(localX, localZ, 0f);
        }

        public bool IsPointAvailable(int posX, int posZ, int targetBuildingId) =>
            _grid.IsPointAvailable(posX, posZ, targetBuildingId);

        public bool IsCellAvailable(int posX, int posZ, int footprintCellCount, int targetBuildingId) =>
            _grid.IsCellAvailable(posX, posZ, footprintCellCount, targetBuildingId);

        public void SetTempBuildingId(int posX, int posZ, int footprintCellCount, int buildingId) =>
            _grid.SetTempBuildingId(posX, posZ, footprintCellCount, buildingId);

        public void ClearTempGrid() => _grid.ClearTempGrid();

        public void SetCellBuildingId(int oldPosX, int oldPosZ, int newPosX, int newPosZ, int footprintCellCount, int buildingId) =>
            _grid.SetCellBuildingId(oldPosX, oldPosZ, newPosX, newPosZ, footprintCellCount, buildingId);
    }
}
