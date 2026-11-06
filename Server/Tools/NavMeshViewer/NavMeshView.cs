using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using cfg;
using DotRecast.Core.Numerics;
using GameMain;
using GameServer.Configuration;
using GameServer.Pathfinding;

namespace NavMeshViewer;

/// <summary>
/// 服务器 NavMesh 可视化面板。
/// 左侧/右键设置起点/终点，中键拖动，滚轮缩放；三角形按连通块着色，关卡和建筑以矩形叠加。
/// </summary>
public sealed class NavMeshView : Control
{
    private sealed class BuildingOverlay
    {
        public required MapBinaryEntry Entry;
        public bool IsGate;
    }

    private static readonly Color[] ComponentPalette =
    {
        Color.FromArgb(72, 132, 214),
        Color.FromArgb(236, 163, 66),
        Color.FromArgb(98, 190, 112),
        Color.FromArgb(207, 96, 168),
        Color.FromArgb(112, 207, 214),
        Color.FromArgb(213, 104, 92),
        Color.FromArgb(166, 132, 219),
        Color.FromArgb(176, 190, 82),
        Color.FromArgb(217, 141, 194),
        Color.FromArgb(110, 178, 154),
    };

    private NavMeshRawData? _raw;
    private NavMeshWorld? _nav;
    private GraphicsPath[] _componentPaths = Array.Empty<GraphicsPath>();
    private Brush[] _componentBrushes = Array.Empty<Brush>();
    private int _componentCount;
    private string _navMeshPath = string.Empty;

    private readonly List<BuildingOverlay> _buildings = new();
    private ServerConfigMgr? _config;
    private string _mapText = string.Empty;

    private bool _fitPending = true;
    private float _scale = 1f;
    private float _offsetX;
    private float _offsetY;

    private bool _hasStart;
    private bool _hasTarget;
    private PointF _start;
    private PointF _target;
    private readonly List<PointF> _path = new();
    private string _pathText = string.Empty;

    private bool _panning;
    private Point _lastPan;

    private string _meshText = string.Empty;

    public event Action<string>? StatusChanged;

    public NavMeshView()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = Color.FromArgb(18, 20, 24);
        TabStop = true;
    }

    public void LoadNavMesh(string path)
    {
        try
        {
            var raw = NavMeshFileReader.Load(path);
            var nav = NavMeshWorld.Load(path);

            _raw = raw;
            _nav = nav;
            _navMeshPath = path;

            BuildMeshComponents();
            _hasStart = false;
            _hasTarget = false;
            _path.Clear();
            _pathText = string.Empty;
            _fitPending = true;

            _meshText = $"NavMesh: 顶点 {raw.VertexCount}，三角形 {raw.TriangleCount}，" +
                        $"连通块 {_componentCount}，.rnav 关卡 {raw.Gates.Count}";

            ReportStatus();
            Invalidate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "加载 .rnav 失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void LoadMapData(string path)
    {
        try
        {
            var entries = MapDataBinary.Read(path);

            _config?.Dispose();
            _config = null;

            TbMapBuildingCfg? buildingTable = null;
            TbAssetPathCfg? assetTable = null;
            bool configLoaded = false;

            string mapDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
            string configDirectory = Path.GetFullPath(Path.Combine(
                mapDirectory, "..", "..", "..", "StreamingAssets", "Config"));

            if (Directory.Exists(configDirectory))
            {
                try
                {
                    _config = new ServerConfigMgr(configDirectory);
                    buildingTable = _config.GetTable<TbMapBuildingCfg>();
                    assetTable = _config.GetTable<TbAssetPathCfg>();
                    configLoaded = true;
                }
                catch
                {
                    _config?.Dispose();
                    _config = null;
                }
            }

            _buildings.Clear();
            int gateCount = 0;

            foreach (var entry in entries)
            {
                bool isGate = false;
                if (buildingTable != null && assetTable != null)
                {
                    var config = buildingTable.Get(entry.Id);
                    isGate = config != null && MapBuildingTypeHelper.IsGate(config, assetTable);
                }

                _buildings.Add(new BuildingOverlay
                {
                    Entry = entry,
                    IsGate = isGate,
                });

                if (isGate)
                    gateCount++;
            }

            _mapText = $"建筑 {_buildings.Count}，关卡 {gateCount}" +
                       (configLoaded ? string.Empty : "（未找到 BuildingCfg，未区分关卡，仍显示全部建筑）");

            if (_raw == null && _buildings.Count > 0)
                _fitPending = true;

            ReportStatus();
            Invalidate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "加载建筑数据失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void Fit()
    {
        FitCore();
        Invalidate();
    }

    public void ClearPath()
    {
        _hasStart = false;
        _hasTarget = false;
        _path.Clear();
        _pathText = string.Empty;
        ReportStatus();
        Invalidate();
    }

    private void FitCore()
    {
        float minX = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxZ = float.MinValue;
        bool hasBounds = false;

        if (_raw != null && _raw.VertexCount > 0)
        {
            for (int i = 0; i < _raw.VertexCount; i++)
            {
                float x = _raw.Vertices[i * 3];
                float z = _raw.Vertices[i * 3 + 2];
                minX = MathF.Min(minX, x);
                maxX = MathF.Max(maxX, x);
                minZ = MathF.Min(minZ, z);
                maxZ = MathF.Max(maxZ, z);
                hasBounds = true;
            }
        }

        if (!hasBounds)
        {
            foreach (var building in _buildings)
            {
                float x = building.Entry.PosX;
                float z = building.Entry.PosZ;
                minX = MathF.Min(minX, x - building.Entry.SizeX * 0.5f);
                maxX = MathF.Max(maxX, x + building.Entry.SizeX * 0.5f);
                minZ = MathF.Min(minZ, z - building.Entry.SizeZ * 0.5f);
                maxZ = MathF.Max(maxZ, z + building.Entry.SizeZ * 0.5f);
                hasBounds = true;
            }
        }

        if (!hasBounds)
            return;

        float rangeX = MathF.Max(1f, maxX - minX);
        float rangeZ = MathF.Max(1f, maxZ - minZ);
        const float margin = 48f;

        float availableWidth = MathF.Max(32f, ClientSize.Width - margin * 2f);
        float availableHeight = MathF.Max(32f, ClientSize.Height - margin * 2f);

        _scale = MathF.Min(availableWidth / rangeX, availableHeight / rangeZ);
        _scale = Math.Clamp(_scale, 0.01f, 200f);

        _offsetX = (ClientSize.Width - (minX + maxX) * _scale) * 0.5f;
        _offsetY = (ClientSize.Height - (minZ + maxZ) * _scale) * 0.5f;

        _fitPending = false;
    }

    private void BuildMeshComponents()
    {
        ClearMeshComponents();

        var raw = _raw;
        if (raw == null || raw.VertexCount == 0 || raw.TriangleCount == 0)
            return;

        int vertexCount = raw.VertexCount;
        int triangleCount = raw.TriangleCount;

        // 按位置量化后焊接顶点，避免 Unity 导出时同一位置出现多个顶点导致连通性误判。
        var canonical = new int[vertexCount];
        var positionIds = new Dictionary<(long, long, long), int>();

        for (int i = 0; i < vertexCount; i++)
        {
            var key = (
                (long)MathF.Round(raw.Vertices[i * 3] * 1000f),
                (long)MathF.Round(raw.Vertices[i * 3 + 1] * 1000f),
                (long)MathF.Round(raw.Vertices[i * 3 + 2] * 1000f));

            if (!positionIds.TryGetValue(key, out int id))
            {
                id = positionIds.Count;
                positionIds[key] = id;
            }

            canonical[i] = id;
        }

        var parent = new int[triangleCount];
        for (int i = 0; i < parent.Length; i++)
            parent[i] = i;

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        void Union(int a, int b)
        {
            a = Find(a);
            b = Find(b);
            if (a != b)
                parent[b] = a;
        }

        var edgeOwners = new Dictionary<(int, int), int>();

        void AddEdge(int a, int b, int triangle)
        {
            if (a == b)
                return;

            var edge = a < b ? (a, b) : (b, a);
            if (edgeOwners.TryGetValue(edge, out int other))
                Union(triangle, other);
            else
                edgeOwners[edge] = triangle;
        }

        for (int t = 0; t < triangleCount; t++)
        {
            int i0 = raw.Indices[t * 3];
            int i1 = raw.Indices[t * 3 + 1];
            int i2 = raw.Indices[t * 3 + 2];

            int a = canonical[i0];
            int b = canonical[i1];
            int c = canonical[i2];

            AddEdge(a, b, t);
            AddEdge(b, c, t);
            AddEdge(c, a, t);
        }

        var componentIds = new Dictionary<int, int>();
        var triangleComponents = new int[triangleCount];

        for (int t = 0; t < triangleCount; t++)
        {
            int root = Find(t);
            if (!componentIds.TryGetValue(root, out int component))
            {
                component = componentIds.Count;
                componentIds[root] = component;
            }

            triangleComponents[t] = component;
        }

        _componentCount = componentIds.Count;
        _componentPaths = new GraphicsPath[Math.Max(1, _componentCount)];
        for (int i = 0; i < _componentPaths.Length; i++)
            _componentPaths[i] = new GraphicsPath();

        for (int t = 0; t < triangleCount; t++)
        {
            int i0 = raw.Indices[t * 3];
            int i1 = raw.Indices[t * 3 + 1];
            int i2 = raw.Indices[t * 3 + 2];

            var points = new[]
            {
                new PointF(raw.Vertices[i0 * 3], raw.Vertices[i0 * 3 + 2]),
                new PointF(raw.Vertices[i1 * 3], raw.Vertices[i1 * 3 + 2]),
                new PointF(raw.Vertices[i2 * 3], raw.Vertices[i2 * 3 + 2]),
            };

            _componentPaths[triangleComponents[t]].AddPolygon(points);
        }

        _componentBrushes = new Brush[_componentPaths.Length];
        for (int i = 0; i < _componentBrushes.Length; i++)
        {
            Color color = ComponentPalette[i % ComponentPalette.Length];
            _componentBrushes[i] = new SolidBrush(Color.FromArgb(145, color.R, color.G, color.B));
        }
    }

    private void ClearMeshComponents()
    {
        foreach (var path in _componentPaths)
            path.Dispose();
        _componentPaths = Array.Empty<GraphicsPath>();

        foreach (var brush in _componentBrushes)
            brush.Dispose();
        _componentBrushes = Array.Empty<Brush>();

        _componentCount = 0;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_fitPending)
            FitCore();

        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.None;

        if (_raw == null)
        {
            DrawCenteredHint(g, "打开 .rnav 后显示导航网格\n左键起点，右键终点，中键拖动，滚轮缩放");
            return;
        }

        using (var matrix = new Matrix(_scale, 0f, 0f, _scale, _offsetX, _offsetY))
        {
            g.Transform = matrix;

            for (int i = 0; i < _componentPaths.Length; i++)
            {
                if (i >= _componentBrushes.Length)
                    break;

                g.FillPath(_componentBrushes[i], _componentPaths[i]);
            }

            DrawRnavGates(g);
            DrawBuildingOverlays(g);

            g.ResetTransform();
        }

        DrawLabels(g);
        DrawPath(g);
        DrawHud(g);
    }

    private void DrawRnavGates(Graphics g)
    {
        if (_raw == null)
            return;

        foreach (var gate in _raw.Gates)
        {
            var points = CreateRectangle(
                new PointF(gate.Center[0], gate.Center[2]),
                gate.Size[0], gate.Size[2], gate.RotationY);

            using var brush = new SolidBrush(gate.Open
                ? Color.FromArgb(90, 50, 220, 50)
                : Color.FromArgb(90, 220, 60, 60));
            using var pen = new Pen(gate.Open ? Color.LimeGreen : Color.OrangeRed, 0f);

            g.FillPolygon(brush, points);
            g.DrawPolygon(pen, points);
        }
    }

    private void DrawBuildingOverlays(Graphics g)
    {
        foreach (var building in _buildings)
        {
            var entry = building.Entry;
            var points = CreateRectangle(
                new PointF(entry.PosX, entry.PosZ),
                entry.SizeX, entry.SizeZ, entry.RotationY);

            using var brush = new SolidBrush(building.IsGate
                ? Color.FromArgb(140, 230, 60, 60)
                : Color.FromArgb(80, 160, 160, 160));
            using var pen = new Pen(building.IsGate ? Color.Red : Color.FromArgb(180, 190, 190, 190), 0f);

            g.FillPolygon(brush, points);
            g.DrawPolygon(pen, points);
        }
    }

    private void DrawLabels(Graphics g)
    {
        using var font = new Font("Segoe UI", 8.5f);
        using var brush = new SolidBrush(Color.White);
        using var gateBrush = new SolidBrush(Color.FromArgb(255, 255, 160));

        if (_raw != null)
        {
            foreach (var gate in _raw.Gates)
            {
                var p = WorldToScreen(new PointF(gate.Center[0], gate.Center[2]));
                g.DrawString($"G{gate.GateId}", font, gateBrush, p.X + 2f, p.Y + 2f);
            }
        }

        foreach (var building in _buildings.Where(b => b.IsGate))
        {
            var p = WorldToScreen(new PointF(building.Entry.PosX, building.Entry.PosZ));
            string uid = building.Entry.InstanceId;
            if (uid.Length > 8)
                uid = uid[..8];

            g.DrawString($"Id {building.Entry.Id}\n{uid}", font, brush, p.X + 2f, p.Y + 2f);
        }

        if (_hasStart)
        {
            var p = WorldToScreen(_start);
            g.DrawString($"起点 ({_start.X:F1}, {_start.Y:F1})", font, Brushes.LightGreen, p.X + 8f, p.Y - 20f);
        }

        if (_hasTarget)
        {
            var p = WorldToScreen(_target);
            g.DrawString($"终点 ({_target.X:F1}, {_target.Y:F1})", font, Brushes.OrangeRed, p.X + 8f, p.Y - 20f);
        }
    }

    private void DrawPath(Graphics g)
    {
        if (_hasStart)
        {
            var p = WorldToScreen(_start);
            using var brush = new SolidBrush(Color.LimeGreen);
            g.FillEllipse(brush, p.X - 5f, p.Y - 5f, 10f, 10f);
        }

        if (_hasTarget)
        {
            var p = WorldToScreen(_target);
            using var brush = new SolidBrush(Color.OrangeRed);
            g.FillEllipse(brush, p.X - 5f, p.Y - 5f, 10f, 10f);
        }

        if (_path.Count == 0)
            return;

        var screenPath = _path.Select(WorldToScreen).ToArray();
        using var pen = new Pen(Color.Yellow, 2f);
        using var pointBrush = new SolidBrush(Color.Yellow);

        if (screenPath.Length > 1)
            g.DrawLines(pen, screenPath);

        foreach (var p in screenPath)
            g.FillEllipse(pointBrush, p.X - 2.5f, p.Y - 2.5f, 5f, 5f);
    }

    private void DrawHud(Graphics g)
    {
        using var font = new Font("Segoe UI", 9f);
        using var brush = new SolidBrush(Color.FromArgb(230, 235, 240));
        using var dimBrush = new SolidBrush(Color.FromArgb(200, 150, 160, 175));

        g.DrawString("左键起点  |  右键终点  |  中键拖动  |  滚轮缩放", font, dimBrush, 12f, 10f);
        g.DrawString(_meshText, font, brush, 12f, 32f);

        if (!string.IsNullOrEmpty(_mapText))
            g.DrawString(_mapText, font, brush, 12f, 52f);

        if (!string.IsNullOrEmpty(_pathText))
            g.DrawString(_pathText, font, brush, 12f, 72f);

        g.DrawString("路径查询只基于 .rnav，不考虑建筑关卡权限", font, dimBrush, 12f, 92f);

        if (!string.IsNullOrEmpty(_navMeshPath))
            g.DrawString(_navMeshPath, font, dimBrush, 12f, ClientSize.Height - 26f);
    }

    private void DrawCenteredHint(Graphics g, string text)
    {
        using var font = new Font("Segoe UI", 12f);
        using var brush = new SolidBrush(Color.FromArgb(220, 220, 225));
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush,
            (ClientSize.Width - size.Width) * 0.5f,
            (ClientSize.Height - size.Height) * 0.5f);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button == MouseButtons.Middle)
        {
            _panning = true;
            _lastPan = e.Location;
            Cursor = Cursors.SizeAll;
            return;
        }

        if (_raw == null)
            return;

        var world = ScreenToWorld(e.Location);

        if (e.Button == MouseButtons.Left)
        {
            _hasStart = true;
            _start = world;
            ComputePath();
        }
        else if (e.Button == MouseButtons.Right)
        {
            _hasTarget = true;
            _target = world;
            ComputePath();
        }

        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_panning)
            return;

        _offsetX += e.X - _lastPan.X;
        _offsetY += e.Y - _lastPan.Y;
        _lastPan = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Middle)
        {
            _panning = false;
            Cursor = Cursors.Default;
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_raw == null)
            return;

        var before = ScreenToWorld(e.Location);
        float factor = e.Delta > 0 ? 1.15f : 1f / 1.15f;
        float newScale = Math.Clamp(_scale * factor, 0.01f, 200f);
        if (MathF.Abs(newScale - _scale) < 1e-6f)
            return;

        _scale = newScale;
        _offsetX = e.X - before.X * _scale;
        _offsetY = e.Y - before.Y * _scale;
        Invalidate();
    }

    private void ComputePath()
    {
        _path.Clear();
        _pathText = string.Empty;

        if (_nav == null || !_hasStart || !_hasTarget)
        {
            ReportStatus();
            return;
        }

        var start = new RcVec3f(_start.X, 0f, _start.Y);
        var target = new RcVec3f(_target.X, 0f, _target.Y);

        bool ok = _nav.FindPath(start, target, out var corners, out var error);

        foreach (var corner in corners)
            _path.Add(new PointF(corner.X, corner.Z));

        _pathText = ok
            ? $"Path OK: {corners.Count} 个拐点"
            : $"Path FAIL: {error}";

        ReportStatus();
    }

    private void ReportStatus()
    {
        string text = string.Join(" | ", new[] { _meshText, _mapText, _pathText }
            .Where(s => !string.IsNullOrEmpty(s)));

        StatusChanged?.Invoke(text);
    }

    private PointF ScreenToWorld(Point point)
    {
        return new PointF(
            (point.X - _offsetX) / _scale,
            (point.Y - _offsetY) / _scale);
    }

    private PointF WorldToScreen(PointF point)
    {
        return new PointF(
            point.X * _scale + _offsetX,
            point.Y * _scale + _offsetY);
    }

    private static PointF[] CreateRectangle(PointF center, float sizeX, float sizeZ, float rotationY)
    {
        float halfX = MathF.Max(0.01f, sizeX) * 0.5f;
        float halfZ = MathF.Max(0.01f, sizeZ) * 0.5f;
        float radians = rotationY * (MathF.PI / 180f);
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        PointF Transform(float x, float z)
        {
            return new PointF(
                center.X + x * cos + z * sin,
                center.Y - x * sin + z * cos);
        }

        return new[]
        {
            Transform(-halfX, -halfZ),
            Transform(halfX, -halfZ),
            Transform(halfX, halfZ),
            Transform(-halfX, halfZ),
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _config?.Dispose();
            _config = null;
            ClearMeshComponents();
        }

        base.Dispose(disposing);
    }
}
