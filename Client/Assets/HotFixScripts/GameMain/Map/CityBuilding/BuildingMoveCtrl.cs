using UnityEngine;

namespace GameMain
{
    [RequireComponent(typeof(BoxCollider))]
    public class BuildingMoveCtrl : MonoBehaviour
    {
        [SerializeField] private int buildingId = 1;
        [SerializeField] private int footprintCellCount = 2;
        [SerializeField] private int startPosX;
        [SerializeField] private int startPosZ;
        [SerializeField] private CityGridView groundGrid;
        [SerializeField] private BuildingMoveGridCtrl moveGridCtrl;
        [SerializeField] private LayerMask groundLayerMask;

        private int _posX;
        private int _posZ;
        private int _committedPosX;
        private int _committedPosZ;
        private float _fixedY;
        private bool _dragging;

        private void Start()
        {
            _posX = _committedPosX = startPosX;
            _posZ = _committedPosZ = startPosZ;
            _fixedY = transform.position.y;

            groundGrid.SetCellBuildingId(_posX, _posZ, _posX, _posZ, footprintCellCount, buildingId);

            moveGridCtrl.Initialize(buildingId, _posX, _posZ, footprintCellCount, groundGrid.GridCellCount, 0);
            moveGridCtrl.UpdateGrid(groundGrid.IsPointAvailable);

            UpdateVisualPosition();
        }

        private void OnMouseDown()
        {
            _dragging = true;
            moveGridCtrl.StartDrag(1000);
        }

        private void Update()
        {
            if (!_dragging)
                return;

            if (Input.GetMouseButton(0))
            {
                if (RaycastGround(out var hitPoint))
                {
                    var (cx, cz) = groundGrid.WorldToCell(hitPoint);
                    var newPosX = cx - footprintCellCount / 2;
                    var newPosZ = cz - footprintCellCount / 2;
                    if (newPosX != _posX || newPosZ != _posZ)
                    {
                        _posX = newPosX;
                        _posZ = newPosZ;
                        UpdateVisualPosition();
                        groundGrid.SetTempBuildingId(_posX, _posZ, footprintCellCount, buildingId);
                        moveGridCtrl.SetViewGridPos(_posX, _posZ);
                        moveGridCtrl.UpdateGrid(groundGrid.IsPointAvailable);
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
                EndDrag();
        }

        private void EndDrag()
        {
            _dragging = false;
            groundGrid.ClearTempGrid();

            if (groundGrid.IsCellAvailable(_posX, _posZ, footprintCellCount, buildingId))
            {
                groundGrid.SetCellBuildingId(_committedPosX, _committedPosZ, _posX, _posZ, footprintCellCount, buildingId);
                _committedPosX = _posX;
                _committedPosZ = _posZ;
            }
            else
            {
                _posX = _committedPosX;
                _posZ = _committedPosZ;
                UpdateVisualPosition();
            }

            moveGridCtrl.SetViewGridPos(_posX, _posZ);
            moveGridCtrl.UpdateGrid(groundGrid.IsPointAvailable);
            moveGridCtrl.EndDrag();
        }

        private void UpdateVisualPosition()
        {
            var cellWidth = groundGrid.CellWidth;
            var basePos = groundGrid.CellToWorld(_posX, _posZ);
            var half = footprintCellCount * cellWidth / 2f;
            var pos = basePos + new Vector3(half, 0f, half);
            pos.y = _fixedY;
            transform.position = pos;
        }

        private bool RaycastGround(out Vector3 hitPoint)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 1000f, groundLayerMask))
                {
                    hitPoint = hit.point;
                    return true;
                }
            }
            hitPoint = default;
            return false;
        }
    }
}
