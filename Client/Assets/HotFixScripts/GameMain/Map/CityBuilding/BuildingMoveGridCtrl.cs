using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameMain
{
    public class BuildingMoveGridCtrl : MonoBehaviour
    {
        [SerializeField] private Renderer gridRenderer;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color notAvailableColor = Color.red;
        [SerializeField] private Color dragColor = Color.green;

        private static readonly Color EmptyColor = new Color(0f, 0f, 0f, 0f);

        private int _buildingId;
        private int _cellCount;
        private int _posX;
        private int _posZ;

        private int _gridCellCount;
        private int _cornerCellCount;
        private int _halfGridCellCount;

        private Mesh _mesh;
        private bool[,] _gridAvailableMark;

        private int _originalSortOrder;
        private bool _dragging;

        public void Initialize(int buildingId, int posX, int posZ, int footprintCellCount, int gridCellCount, int cornerCellCount)
        {
            _buildingId = buildingId;
            _gridCellCount = gridCellCount;
            _cornerCellCount = cornerCellCount;
            _halfGridCellCount = gridCellCount / 2;
            _posX = posX;
            _posZ = posZ;

            _originalSortOrder = gridRenderer.sortingOrder;
            gridRenderer.material.SetFloat("_CellCount", footprintCellCount);

            if (_mesh == null)
            {
                _mesh = new Mesh();
                gridRenderer.GetComponent<MeshFilter>().mesh = _mesh;
            }

            if (footprintCellCount != _cellCount)
            {
                _cellCount = footprintCellCount;
                _gridAvailableMark = new bool[_cellCount, _cellCount];
                RebuildMesh();
            }
        }

        public void SetViewGridPos(int posX, int posZ)
        {
            _posX = posX;
            _posZ = posZ;
        }

        public void StartDrag(int order)
        {
            _dragging = true;
            gridRenderer.sortingOrder = order;
            UpdateMeshColor();
        }

        public void EndDrag()
        {
            _dragging = false;
            gridRenderer.sortingOrder = _originalSortOrder;
            UpdateMeshColor();
        }

        public void UpdateGrid(Func<int, int, int, bool> checker)
        {
            var update = false;
            var xMax = _posX + _cellCount;
            var yMax = _posZ + _cellCount;
            for (var x = _posX; x < xMax; x++)
            {
                for (var y = _posZ; y < yMax; y++)
                {
                    var result = checker.Invoke(x, y, _buildingId);
                    if (_gridAvailableMark[x - _posX, y - _posZ] != result)
                        update = true;
                    _gridAvailableMark[x - _posX, y - _posZ] = result;
                }
            }
            if (update)
                UpdateMeshColor();
        }

        private void RebuildMesh()
        {
            var seg = 1f / _cellCount;
            var uvStep = 1f / _cellCount;

            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();

            void AddVertex(int i, int j)
            {
                vertices.Add(new Vector3((j - _cellCount / 2f) * seg, (i - _cellCount / 2f) * seg, 0f));
                uv.Add(new Vector2(j * uvStep, i * uvStep));
            }

            for (var i = 0; i < _cellCount; i++)
            {
                for (var j = 0; j < _cellCount; j++)
                {
                    AddVertex(i, j);
                    AddVertex(i + 1, j);
                    AddVertex(i + 1, j + 1);
                    AddVertex(i, j + 1);
                }
            }

            var triangles = new int[_cellCount * _cellCount * 6];
            var triIndex = 0;
            for (var i = 0; i < _cellCount; i++)
            {
                for (var j = 0; j < _cellCount; j++)
                {
                    var start = (i * _cellCount + j) * 4;
                    triangles[triIndex++] = start;
                    triangles[triIndex++] = start + 1;
                    triangles[triIndex++] = start + 3;
                    triangles[triIndex++] = start + 1;
                    triangles[triIndex++] = start + 2;
                    triangles[triIndex++] = start + 3;
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetUVs(0, uv);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateBounds();
        }

        private bool OutOfBounds(int posX, int posZ)
        {
            if (posX < 0 || posX >= _gridCellCount || posZ < 0 || posZ >= _gridCellCount)
                return true;

            var x = posX;
            var y = posZ;
            if (x > _halfGridCellCount)
                x = _gridCellCount - x - 1;
            if (y > _halfGridCellCount)
                y = _gridCellCount - y - 1;
            return x + y < _cornerCellCount;
        }

        private void UpdateMeshColor()
        {
            var colors = new Color[_mesh.vertexCount];
            for (var i = 0; i < _cellCount; i++)
            {
                for (var j = 0; j < _cellCount; j++)
                {
                    Color c;
                    if (OutOfBounds(_posX + j, _posZ + i))
                    {
                        c = EmptyColor;
                    }
                    else
                    {
                        var available = _gridAvailableMark[j, i];
                        c = _dragging
                            ? (available ? dragColor : notAvailableColor)
                            : (available ? availableColor : notAvailableColor);
                    }

                    var idx = (i * _cellCount + j) * 4;
                    colors[idx] = c;
                    colors[idx + 1] = c;
                    colors[idx + 2] = c;
                    colors[idx + 3] = c;
                }
            }
            _mesh.SetColors(colors);
        }
    }
}
