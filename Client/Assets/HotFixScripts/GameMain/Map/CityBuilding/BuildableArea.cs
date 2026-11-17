using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameMain
{
    /// <summary>
    /// 运行时的可建造区域：由 BuildableAreaData 里的一个矩形条目创建。
    ///
    /// 负责：
    ///   - 在场景里画出绿色半透明矩形（不挂 Collider，不影响寻路）；
    ///   - 把矩形切成 CellSize 的格子，记录建筑占位；
    ///   - 提供与内城 CityGrid 相同的规则：格子内可放置、临时占位、提交/回滚。
    /// </summary>
    public sealed class BuildableArea : MonoBehaviour
    {
        /// <summary>所有区域共用的绿色材质。</summary>
        private static Material s_fillMaterial;
        private static Material s_lineMaterial;

        private static readonly Color FreeCellColor = new Color(0.2f, 1f, 0.4f, 1f);
        private static readonly Color OccupiedCellColor = new Color(1f, 0.3f, 0.25f, 1f);

        public string InstanceId { get; private set; }

        /// <summary>矩形尺寸（本地 X/Z，单位米），会被对齐到格子尺寸。</summary>
        public Vector2 Size { get; private set; }

        public float CellSize { get; private set; }
        public int CellsX { get; private set; }
        public int CellsZ { get; private set; }

        private int[,] _occupants;
        private int[,] _tempOccupants;

        // 建造 / 移动状态下显示的小格子：每个格子一张 quad，顶点色表示是否被占用。
        private GameObject _gridObject;
        private Mesh _gridMesh;
        private MeshRenderer _gridRenderer;
        private Color[] _gridColors;
        private bool _gridVisible;

        public void Initialize(MapItemData data, float cellSize)
        {
            InstanceId = data.InstanceId;
            CellSize = Mathf.Max(0.25f, cellSize);

            float width = Mathf.Max(CellSize, data.Size.x);
            float depth = Mathf.Max(CellSize, data.Size.y);

            CellsX = Mathf.Max(1, Mathf.RoundToInt(width / CellSize));
            CellsZ = Mathf.Max(1, Mathf.RoundToInt(depth / CellSize));
            Size = new Vector2(CellsX * CellSize, CellsZ * CellSize);

            _occupants = new int[CellsX, CellsZ];
            _tempOccupants = new int[CellsX, CellsZ];

            BuildVisual();
            BuildGrid();
        }

        // ---------------------------------------------------------------- 格子显示（建造 / 移动状态）

        /// <summary>设置格子材质；材质由 BuildableAreaMgr 异步加载后传入。</summary>
        public void SetGridMaterial(Material material)
        {
            if (_gridRenderer == null)
                return;

            _gridRenderer.sharedMaterial = material;
            _gridRenderer.enabled = material != null;
        }

        /// <summary>建造 / 移动状态下显示可建造区域的小格子。</summary>
        public void SetGridVisible(bool visible)
        {
            _gridVisible = visible;

            if (_gridObject != null && _gridObject.activeSelf != visible)
                _gridObject.SetActive(visible);

            if (visible)
                RefreshGridColors();
        }

        /// <summary>按当前占位刷新每个格子的颜色：空闲绿色，被建筑占用红色。</summary>
        public void RefreshGridColors()
        {
            if (_gridMesh == null || _gridColors == null || _occupants == null)
                return;

            int cellCount = CellsX * CellsZ;
            for (int index = 0; index < cellCount; index++)
            {
                int x = index % CellsX;
                int z = index / CellsX;
                Color color = _occupants[x, z] != 0 ? OccupiedCellColor : FreeCellColor;

                int vertex = index * 4;
                _gridColors[vertex] = color;
                _gridColors[vertex + 1] = color;
                _gridColors[vertex + 2] = color;
                _gridColors[vertex + 3] = color;
            }

            _gridMesh.SetColors(_gridColors);
        }

        private void BuildGrid()
        {
            _gridObject = new GameObject("GridOverlay");
            _gridObject.transform.SetParent(transform, false);
            _gridObject.layer = 2; // Ignore Raycast

            var filter = _gridObject.AddComponent<MeshFilter>();
            _gridMesh = new Mesh { name = "BuildableAreaGrid" };

            int cellCount = CellsX * CellsZ;
            var vertices = new Vector3[cellCount * 4];
            var uvs = new Vector2[cellCount * 4];
            var triangles = new int[cellCount * 6];
            _gridColors = new Color[cellCount * 4];

            float halfX = Size.x * 0.5f;
            float halfZ = Size.y * 0.5f;
            const float y = 0.03f;

            int vi = 0;
            int ti = 0;
            for (int z = 0; z < CellsZ; z++)
            {
                for (int x = 0; x < CellsX; x++)
                {
                    float x0 = -halfX + x * CellSize;
                    float z0 = -halfZ + z * CellSize;
                    float x1 = x0 + CellSize;
                    float z1 = z0 + CellSize;

                    vertices[vi + 0] = new Vector3(x0, y, z0);
                    vertices[vi + 1] = new Vector3(x0, y, z1);
                    vertices[vi + 2] = new Vector3(x1, y, z1);
                    vertices[vi + 3] = new Vector3(x1, y, z0);

                    uvs[vi + 0] = new Vector2(0f, 0f);
                    uvs[vi + 1] = new Vector2(0f, 1f);
                    uvs[vi + 2] = new Vector2(1f, 1f);
                    uvs[vi + 3] = new Vector2(1f, 0f);

                    triangles[ti++] = vi + 0;
                    triangles[ti++] = vi + 1;
                    triangles[ti++] = vi + 2;
                    triangles[ti++] = vi + 0;
                    triangles[ti++] = vi + 2;
                    triangles[ti++] = vi + 3;

                    vi += 4;
                }
            }

            _gridMesh.vertices = vertices;
            _gridMesh.uv = uvs;
            _gridMesh.triangles = triangles;
            _gridMesh.RecalculateBounds();
            filter.sharedMesh = _gridMesh;

            _gridRenderer = _gridObject.AddComponent<MeshRenderer>();
            _gridRenderer.enabled = false;
            _gridRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _gridRenderer.receiveShadows = false;
            _gridRenderer.lightProbeUsage = LightProbeUsage.Off;
            _gridRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            RefreshGridColors();
            _gridObject.SetActive(false);
        }

        // ---------------------------------------------------------------- 坐标 / 格子

        /// <summary>世界坐标 → 格子下标；点不在矩形内时返回 false。</summary>
        public bool TryWorldToCell(Vector3 world, out int x, out int z)
        {
            Vector3 local = Quaternion.Inverse(transform.rotation) * (world - transform.position);
            float cellX = (local.x + Size.x * 0.5f) / CellSize;
            float cellZ = (local.z + Size.y * 0.5f) / CellSize;

            x = Mathf.FloorToInt(cellX);
            z = Mathf.FloorToInt(cellZ);
            return x >= 0 && x < CellsX && z >= 0 && z < CellsZ;
        }

        public bool Contains(Vector3 world)
        {
            return TryWorldToCell(world, out _, out _);
        }

        /// <summary>格子最小角（本地 X/Z 的负边界）对应的世界坐标。</summary>
        public Vector3 GetCellMinWorld(int x, int z)
        {
            Vector3 local = new Vector3(
                -Size.x * 0.5f + x * CellSize,
                0f,
                -Size.y * 0.5f + z * CellSize);
            return transform.position + transform.rotation * local;
        }

        /// <summary>一组格子（footprint 的最小角 + 尺寸）中心对应的世界坐标。</summary>
        public Vector3 GetFootprintCenterWorld(int x, int z, int cellsX, int cellsZ)
        {
            Vector3 local = new Vector3(
                -Size.x * 0.5f + (x + cellsX * 0.5f) * CellSize,
                0f,
                -Size.y * 0.5f + (z + cellsZ * 0.5f) * CellSize);
            return transform.position + transform.rotation * local;
        }

        public bool IsPointAvailable(int x, int z, int buildingId)
        {
            if (x < 0 || x >= CellsX || z < 0 || z >= CellsZ)
                return false;

            int occupant = _occupants[x, z];
            if (occupant != 0 && occupant != buildingId)
                return false;

            int temp = _tempOccupants[x, z];
            return temp == 0 || temp == buildingId;
        }

        public bool IsCellAvailable(int x, int z, int cellsX, int cellsZ, int buildingId)
        {
            for (int ix = x; ix < x + cellsX; ix++)
                for (int iz = z; iz < z + cellsZ; iz++)
                    if (!IsPointAvailable(ix, iz, buildingId))
                        return false;

            return true;
        }

        public void SetTempBuildingId(int x, int z, int cellsX, int cellsZ, int buildingId)
        {
            for (int ix = x; ix < x + cellsX; ix++)
                for (int iz = z; iz < z + cellsZ; iz++)
                    if (ix >= 0 && ix < CellsX && iz >= 0 && iz < CellsZ)
                        _tempOccupants[ix, iz] = buildingId;
        }

        public void ClearTempGrid()
        {
            Array.Clear(_tempOccupants, 0, _tempOccupants.Length);
        }

        public void SetCellBuildingId(int oldX, int oldZ, int newX, int newZ,
            int cellsX, int cellsZ, int buildingId)
        {
            for (int ix = 0; ix < cellsX; ix++)
                for (int iz = 0; iz < cellsZ; iz++)
                {
                    int ox = oldX + ix;
                    int oz = oldZ + iz;
                    if (ox >= 0 && ox < CellsX && oz >= 0 && oz < CellsZ && _occupants[ox, oz] == buildingId)
                        _occupants[ox, oz] = 0;
                }

            for (int ix = 0; ix < cellsX; ix++)
                for (int iz = 0; iz < cellsZ; iz++)
                {
                    int nx = newX + ix;
                    int nz = newZ + iz;
                    if (nx >= 0 && nx < CellsX && nz >= 0 && nz < CellsZ)
                        _occupants[nx, nz] = buildingId;
                }

            if (_gridVisible)
                RefreshGridColors();
        }

        /// <summary>把一组格子上属于某个建筑的占位清掉（进入移动状态时使用）。</summary>
        public void RemoveCellBuildingId(int x, int z, int cellsX, int cellsZ, int buildingId)
        {
            for (int ix = 0; ix < cellsX; ix++)
                for (int iz = 0; iz < cellsZ; iz++)
                {
                    int cx = x + ix;
                    int cz = z + iz;
                    if (cx >= 0 && cx < CellsX && cz >= 0 && cz < CellsZ && _occupants[cx, cz] == buildingId)
                        _occupants[cx, cz] = 0;
                }

            if (_gridVisible)
                RefreshGridColors();
        }

        // ---------------------------------------------------------------- 绿色矩形表现

        private void BuildVisual()
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            visual.layer = 2; // Ignore Raycast

            var filter = visual.AddComponent<MeshFilter>();
            var mesh = new Mesh { name = "BuildableAreaRect" };

            float halfX = Size.x * 0.5f;
            float halfZ = Size.y * 0.5f;

            mesh.vertices = new[]
            {
                new Vector3(-halfX, 0f, -halfZ),
                new Vector3(-halfX, 0f, halfZ),
                new Vector3(halfX, 0f, halfZ),
                new Vector3(halfX, 0f, -halfZ),
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;

            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetFillMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // 边框，让绿色区域在任何地面上都看得清。
            var border = new GameObject("Border");
            border.transform.SetParent(transform, false);
            border.layer = 2;

            var line = border.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 4;
            line.widthMultiplier = 0.15f;
            line.sharedMaterial = GetLineMaterial();
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.SetPosition(0, new Vector3(-halfX, 0.02f, -halfZ));
            line.SetPosition(1, new Vector3(-halfX, 0.02f, halfZ));
            line.SetPosition(2, new Vector3(halfX, 0.02f, halfZ));
            line.SetPosition(3, new Vector3(halfX, 0.02f, -halfZ));
        }

        private static Material GetFillMaterial()
        {
            if (s_fillMaterial != null)
                return s_fillMaterial;

            s_fillMaterial = CreateTransparentMaterial(new Color(0.2f, 1f, 0.35f, 0.25f));
            return s_fillMaterial;
        }

        private static Material GetLineMaterial()
        {
            if (s_lineMaterial != null)
                return s_lineMaterial;

            s_lineMaterial = CreateTransparentMaterial(new Color(0.2f, 1f, 0.35f, 1f));
            return s_lineMaterial;
        }

        private static Material CreateTransparentMaterial(Color color)
        {
            // Sprites/Default 在内置管线和 URP 下都可用，并且支持透明与顶点色。
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

            var material = new Material(shader) { name = "BuildableAreaGreen" };
            material.color = color;
            return material;
        }
    }
}
