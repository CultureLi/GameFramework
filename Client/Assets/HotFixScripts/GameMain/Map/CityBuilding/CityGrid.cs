using System;

namespace GameMain
{
    public class CityGrid
    {
        private readonly int _cellCount;
        private readonly int _cornerCellCount;
        private readonly int _halfCellCount;

        private readonly int[,] _cellBuildingIds;
        private readonly int[,] _tempBuildingIds;

        public int CellCount => _cellCount;

        public CityGrid(int cellCount, int cornerCellCount)
        {
            _cellCount = cellCount;
            _cornerCellCount = cornerCellCount;
            _halfCellCount = cellCount / 2;
            _cellBuildingIds = new int[cellCount, cellCount];
            _tempBuildingIds = new int[cellCount, cellCount];
        }

        public bool IsCellInCorner(int x, int y)
        {
            if (x > _halfCellCount)
                x = _cellCount - x - 1;
            if (y > _halfCellCount)
                y = _cellCount - y - 1;
            return x + y < _cornerCellCount;
        }

        private bool IsInRange(int x, int y)
        {
            return x >= 0 && x < _cellCount && y >= 0 && y < _cellCount;
        }

        public bool IsPointAvailable(int posX, int posZ, int targetBuildingId)
        {
            if (!IsInRange(posX, posZ))
                return false;
            if (IsCellInCorner(posX, posZ))
                return false;

            var occupantId = _cellBuildingIds[posX, posZ];
            if (occupantId != 0 && occupantId != targetBuildingId)
                return false;

            var tempId = _tempBuildingIds[posX, posZ];
            if (tempId != 0 && tempId != targetBuildingId)
                return false;

            return true;
        }

        public bool IsCellAvailable(int posX, int posZ, int footprintCellCount, int targetBuildingId)
        {
            var xMax = posX + footprintCellCount;
            var yMax = posZ + footprintCellCount;
            for (var x = posX; x < xMax; x++)
            {
                for (var y = posZ; y < yMax; y++)
                {
                    if (!IsPointAvailable(x, y, targetBuildingId))
                        return false;
                }
            }
            return true;
        }

        public void SetTempBuildingId(int posX, int posZ, int footprintCellCount, int buildingId)
        {
            var xMax = posX + footprintCellCount;
            var yMax = posZ + footprintCellCount;
            for (var x = posX; x < xMax; x++)
            {
                for (var y = posZ; y < yMax; y++)
                {
                    if (IsInRange(x, y))
                        _tempBuildingIds[x, y] = buildingId;
                }
            }
        }

        public void ClearTempGrid()
        {
            Array.Clear(_tempBuildingIds, 0, _tempBuildingIds.Length);
        }

        public void SetCellBuildingId(int oldPosX, int oldPosZ, int newPosX, int newPosZ, int footprintCellCount, int buildingId)
        {
            for (var x = 0; x < footprintCellCount; x++)
            {
                for (var y = 0; y < footprintCellCount; y++)
                {
                    var ox = oldPosX + x;
                    var oy = oldPosZ + y;
                    if (IsInRange(ox, oy) && _cellBuildingIds[ox, oy] == buildingId)
                        _cellBuildingIds[ox, oy] = 0;
                }
            }

            for (var x = 0; x < footprintCellCount; x++)
            {
                for (var y = 0; y < footprintCellCount; y++)
                {
                    var nx = newPosX + x;
                    var ny = newPosZ + y;
                    if (IsInRange(nx, ny))
                        _cellBuildingIds[nx, ny] = buildingId;
                }
            }
        }
    }
}
