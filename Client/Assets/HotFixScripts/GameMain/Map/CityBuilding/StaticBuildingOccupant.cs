using UnityEngine;

namespace GameMain
{
    public class StaticBuildingOccupant : MonoBehaviour
    {
        [SerializeField] private int buildingId = 2;
        [SerializeField] private int posX;
        [SerializeField] private int posZ;
        [SerializeField] private int footprintCellCount = 2;
        [SerializeField] private CityGridView groundGrid;

        private void Start()
        {
            groundGrid.SetCellBuildingId(posX, posZ, posX, posZ, footprintCellCount, buildingId);
        }
    }
}
