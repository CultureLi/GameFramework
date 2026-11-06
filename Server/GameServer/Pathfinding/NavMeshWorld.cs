using DotRecast.Core.Numerics;
using DotRecast.Detour;
using GameServer.Game;
using GameServer.Map;
using GameServer.Utility;

namespace GameServer.Pathfinding;

public sealed class NavMeshGate
{
    public int GateId;
    public RcVec3f Center;
    public RcVec3f Size;
    public float RotationY;
    public bool Open;
    public readonly List<long> PolyRefs = new();
}

/// <summary>
/// 导航网格运行时封装：加载 .rnav、构建 Detour 导航网格、执行寻路查询、动态开关关卡门。
/// DtNavMeshQuery 不是线程安全的，这里用锁串行化所有查询和关卡门开关。
/// </summary>
public sealed class NavMeshWorld
{
    private static readonly RcVec3f QueryExtents = new(2f, 4f, 2f);
    private const int MaxPath = 256;
    private const int MaxStraightPath = 256;
    private const int WalkableFlag = 1;
    private const int GatePolyFlag = 1 << 1;

    private readonly DtNavMesh _navMesh;
    private readonly DtNavMeshQuery _query;
    private readonly DtQueryDefaultFilter _filter;
    private readonly DtQueryDefaultFilter _gateQueryFilter;
    private readonly object _queryLock = new();
    private readonly Dictionary<int, NavMeshGate> _gates = new();
    private readonly Dictionary<long, int[]> _polyGateIndices = new();
    private readonly List<MapGateDefinition> _mapGates = new();

    private NavMeshWorld(DtNavMesh navMesh, RcVec3f fallbackSpawnPoint, NavMeshBuildStats stats)
    {
        _navMesh = navMesh;
        _query = new DtNavMeshQuery(navMesh);
        _filter = new DtQueryDefaultFilter();
        _filter.SetIncludeFlags(WalkableFlag | GatePolyFlag);
        _gateQueryFilter = new DtQueryDefaultFilter();
        _gateQueryFilter.SetIncludeFlags(0xffff);
        SpawnPoint = fallbackSpawnPoint;
        Stats = stats;
    }

    public RcVec3f SpawnPoint { get; private set; }

    public NavMeshBuildStats Stats { get; }

    public IReadOnlyCollection<NavMeshGate> Gates => _gates.Values;

    public static NavMeshWorld Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"导航数据文件不存在: {path}", path);

        var raw = NavMeshFileReader.Load(path);
        var result = NavMeshBuilder.Build(raw);

        var world = new NavMeshWorld(result.NavMesh, result.SpawnPoint, result.Stats);
        world.InitializeGates(raw.Gates);
        world.ComputeSpawnPointNear(new RcVec3f(0f, 0f, 0f));

        Log.Info($"[navmesh] 加载完成 {path}");
        Log.Info($"[navmesh] 顶点={raw.VertexCount} 三角形={raw.TriangleCount} Link={raw.Links.Count} " +
                 $"关卡门={raw.Gates.Count} poly={result.Stats.PolyCount} " +
                 $"边界边={result.Stats.BoundaryEdgeCount} 内部边={result.Stats.InternalEdgeCount} " +
                 $"非流形边={result.Stats.NonManifoldEdgeCount}");
        Log.Info($"[navmesh] 出生点 ({world.SpawnPoint.X:F3}, {world.SpawnPoint.Y:F3}, {world.SpawnPoint.Z:F3})");

        return world;
    }

    /// <summary>
    /// 求从 start 到 end 的直线路径。成功返回 true，corners 至少包含起点和终点。
    /// 关卡门关闭时，其覆盖的导航多边形 flag 被置 0，查询会自动绕开或返回不可达。
    /// </summary>
    public bool FindPath(RcVec3f start, RcVec3f end, out List<RcVec3f> corners, out string error)
    {
        return FindPath(start, end, null, out corners, out error);
    }

    /// <summary>
    /// 按玩家的关卡通行位图寻路。gateMask 为 null 时不检查建筑关卡权限。
    /// </summary>
    public bool FindPath(RcVec3f start, RcVec3f end, GateAccessMask? gateMask,
        out List<RcVec3f> corners, out string error)
    {
        corners = new List<RcVec3f>();
        error = string.Empty;

        IDtQueryFilter filter = gateMask == null
            ? _filter
            : new GateAccessFilter(_filter, _polyGateIndices, gateMask);

        lock (_queryLock)
        {
            var startStatus = _query.FindNearestPoly(
                start, QueryExtents, filter, out var startRef, out var startNearest, out _);
            if (startStatus.Failed() || startRef == 0)
            {
                error = "起点不在导航网格上";
                return false;
            }

            var endStatus = _query.FindNearestPoly(
                end, QueryExtents, filter, out var endRef, out var endNearest, out _);
            if (endStatus.Failed() || endRef == 0)
            {
                error = "目标点不在导航网格上";
                return false;
            }

            var path = new long[MaxPath];
            var pathStatus = _query.FindPath(
                startRef, endRef, startNearest, endNearest, filter, path, out var pathCount, path.Length);
            if (pathStatus.Failed() || pathCount == 0)
            {
                error = $"寻路失败: {pathStatus.Value}";
                return false;
            }

            if (pathStatus.IsPartial())
            {
                error = "目标不可达";
                return false;
            }

            var straightPath = new DtStraightPath[MaxStraightPath];
            var straightStatus = _query.FindStraightPath(
                startNearest, endNearest, path.AsSpan(0, pathCount), pathCount,
                straightPath, out var straightCount, straightPath.Length, 0);
            if (straightStatus.Failed() || straightCount == 0)
            {
                error = $"直线路径生成失败: {straightStatus.Value}";
                return false;
            }

            for (var i = 0; i < straightCount; i++)
                corners.Add(straightPath[i].pos);

            return true;
        }
    }

    /// <summary>切换关卡门。gateId 不存在返回 false。</summary>
    public bool TrySetGateState(int gateId, bool open)
    {
        lock (_queryLock)
        {
            if (!_gates.TryGetValue(gateId, out var gate))
                return false;

            gate.Open = open;
            ApplyGateFlags(gate);
            return true;
        }
    }

    /// <summary>
    /// 把地图建筑关卡映射到导航多边形。每个关卡分配一个 GateIndex，
    /// poly 上只写入 GatePolyFlag 标记，具体属于哪些关卡放在 _polyGateIndices 里。
    /// </summary>
    public void RegisterMapGates(IReadOnlyList<MapGateDefinition> gateDefinitions)
    {
        if (gateDefinitions.Count == 0)
            return;

        lock (_queryLock)
        {
            var polyGates = new Dictionary<long, List<int>>();

            foreach (var gate in gateDefinitions)
            {
                var probe = new RcVec3f(gate.PosX, 0f, gate.PosZ);
                var probeExtents = new RcVec3f(5f, 500f, 5f);
                float groundY = 0f;

                var nearestStatus = _query.FindNearestPoly(
                    probe, probeExtents, _gateQueryFilter, out var nearestRef, out var nearest, out _);
                if (nearestStatus.Succeeded() && nearestRef != 0)
                    groundY = nearest.Y;

                const float gateHeight = 10f;
                float yaw = gate.RotationY * (MathF.PI / 180f);
                float cos = MathF.Abs(MathF.Cos(yaw));
                float sin = MathF.Abs(MathF.Sin(yaw));
                float halfX = gate.SizeX * 0.5f;
                float halfZ = gate.SizeZ * 0.5f;

                var center = new RcVec3f(gate.PosX, groundY + gateHeight * 0.5f, gate.PosZ);
                var extents = new RcVec3f(
                    cos * halfX + sin * halfZ,
                    gateHeight * 0.5f,
                    sin * halfX + cos * halfZ);

                var refs = new long[256];
                var status = _query.QueryPolygons(
                    center, extents, _gateQueryFilter, refs, out var count, refs.Length);

                if (status.Failed())
                {
                    Log.Warn($"[navmesh] 建筑关卡 {gate.InstanceId} QueryPolygons 失败: {status.Value}");
                    continue;
                }

                for (var i = 0; i < count; i++)
                {
                    long polyRef = refs[i];

                    // 保留全局关卡门的开启/关闭状态，只额外加一个建筑关卡标记。
                    int currentFlags = WalkableFlag;
                    if (_navMesh.GetPolyFlags(polyRef, out int readFlags).Succeeded())
                        currentFlags = readFlags;

                    int flags = (currentFlags & WalkableFlag) != 0
                        ? currentFlags | GatePolyFlag
                        : 0;

                    var setStatus = _navMesh.SetPolyFlags(polyRef, flags);
                    if (setStatus.Failed())
                        Log.Warn($"[navmesh] 建筑关卡 {gate.InstanceId} SetPolyFlags 失败: {setStatus.Value}");

                    if (!polyGates.TryGetValue(polyRef, out var gateIndices))
                    {
                        gateIndices = new List<int>();
                        polyGates[polyRef] = gateIndices;
                    }

                    if (!gateIndices.Contains(gate.GateIndex))
                        gateIndices.Add(gate.GateIndex);
                }

                if (count == 0)
                    Log.Warn($"[navmesh] 建筑关卡 {gate.InstanceId} 没有找到覆盖的导航多边形");

                _mapGates.Add(gate);
                Log.Info($"[navmesh] 建筑关卡 {gate.InstanceId} 覆盖 {count} 个多边形，GateIndex={gate.GateIndex}");
            }

            foreach (var pair in polyGates)
                _polyGateIndices[pair.Key] = pair.Value.ToArray();
        }
    }

    private sealed class GateAccessFilter : IDtQueryFilter
    {
        private readonly IDtQueryFilter _baseFilter;
        private readonly IReadOnlyDictionary<long, int[]> _polyGateIndices;
        private readonly GateAccessMask _allowedGates;

        public GateAccessFilter(IDtQueryFilter baseFilter,
            IReadOnlyDictionary<long, int[]> polyGateIndices,
            GateAccessMask allowedGates)
        {
            _baseFilter = baseFilter;
            _polyGateIndices = polyGateIndices;
            _allowedGates = allowedGates;
        }

        public bool PassFilter(long polyRef, DtMeshTile tile, DtPoly poly)
        {
            if (!_baseFilter.PassFilter(polyRef, tile, poly))
                return false;

            if ((poly.flags & GatePolyFlag) == 0)
                return true;

            if (!_polyGateIndices.TryGetValue(polyRef, out var gateIndices))
                return true;

            for (int i = 0; i < gateIndices.Length; i++)
            {
                if (!_allowedGates.IsAllowed(gateIndices[i]))
                    return false;
            }

            return true;
        }

        public float GetCost(
            RcVec3f start, RcVec3f end,
            long prevRef, DtMeshTile prevTile, DtPoly prevPoly,
            long curRef, DtMeshTile curTile, DtPoly curPoly,
            long nextRef, DtMeshTile nextTile, DtPoly nextPoly)
        {
            return _baseFilter.GetCost(
                start, end,
                prevRef, prevTile, prevPoly,
                curRef, curTile, curPoly,
                nextRef, nextTile, nextPoly);
        }
    }

    public NavMeshGate? FindGate(int gateId)
    {
        lock (_queryLock)
            return _gates.TryGetValue(gateId, out var gate) ? gate : null;
    }

    /// <summary>
    /// 从指定世界坐标向周围搜索最近的可行走点。用于出生点：(0,0,0) 通常不在导航网格上，
    /// FindNearestPoly 会返回它所在搜索框内最近的导航多边形上的点。
    /// </summary>
    private void ComputeSpawnPointNear(RcVec3f target)
    {
        lock (_queryLock)
        {
            // 由小到大扩大搜索范围；y 方向给足，避免地面高度导致搜索框打不到导航网格
            float[] radii = { 10f, 50f, 200f, 1000f, 5000f };
            foreach (float radius in radii)
            {
                var extents = new RcVec3f(radius, 1000f, radius);
                var status = _query.FindNearestPoly(
                    target, extents, _filter, out var polyRef, out var nearest, out var isOverPoly);

                if (status.Failed() || polyRef == 0)
                    continue;

                SpawnPoint = nearest;
                Log.Info($"[navmesh] 出生点从 ({target.X:F1}, {target.Y:F1}, {target.Z:F1}) 吸附到" +
                         $" ({nearest.X:F3}, {nearest.Y:F3}, {nearest.Z:F3})，搜索半径 {radius:F0}m，" +
                         $"overPoly={isOverPoly}");
                return;
            }
        }

        Log.Warn($"[navmesh] 未能从 ({target.X:F1}, {target.Y:F1}, {target.Z:F1}) 找到可寻路点，" +
                 $"使用第一个三角形质心作为出生点");
    }

    private void InitializeGates(List<NavMeshGateData> gateData)
    {
        if (gateData.Count == 0)
            return;

        lock (_queryLock)
        {
            // 先把所有门映射到导航多边形，再统一应用初始开关状态，
            // 避免先关闭的门把后映射的 QueryPolygons 过滤掉。
            foreach (var data in gateData)
            {
                if (_gates.ContainsKey(data.GateId))
                {
                    Log.Warn($"[navmesh] 关卡门 gateID 重复: {data.GateId}，已跳过");
                    continue;
                }

                var gate = new NavMeshGate
                {
                    GateId = data.GateId,
                    Center = new RcVec3f(data.Center[0], data.Center[1], data.Center[2]),
                    Size = new RcVec3f(data.Size[0], data.Size[1], data.Size[2]),
                    RotationY = data.RotationY,
                    Open = data.Open
                };

                MapGatePolygons(gate);
                _gates[gate.GateId] = gate;

                Log.Info($"[navmesh] 关卡门 {gate.GateId} 覆盖 {gate.PolyRefs.Count} 个多边形，" +
                         $"初始{(gate.Open ? "打开" : "关闭")}");
            }

            foreach (var gate in _gates.Values)
                ApplyGateFlags(gate);
        }
    }

    private void MapGatePolygons(NavMeshGate gate)
    {
        float yaw = gate.RotationY * (MathF.PI / 180f);
        float cos = MathF.Abs(MathF.Cos(yaw));
        float sin = MathF.Abs(MathF.Sin(yaw));
        float halfX = gate.Size.X * 0.5f;
        float halfZ = gate.Size.Z * 0.5f;

        var extents = new RcVec3f(
            cos * halfX + sin * halfZ,
            gate.Size.Y * 0.5f,
            sin * halfX + cos * halfZ);

        var refs = new long[256];
        var status = _query.QueryPolygons(
            gate.Center, extents, _gateQueryFilter, refs, out var count, refs.Length);

        if (status.Failed())
        {
            Log.Warn($"[navmesh] 关卡门 {gate.GateId} QueryPolygons 失败: {status.Value}");
            return;
        }

        for (var i = 0; i < count; i++)
            gate.PolyRefs.Add(refs[i]);

        if (count == 0)
            Log.Warn($"[navmesh] 关卡门 {gate.GateId} 没有找到覆盖的导航多边形，关闭后不会阻挡寻路");
    }

    private void ApplyGateFlags(NavMeshGate gate)
    {
        int flags = gate.Open ? WalkableFlag : 0;

        foreach (var polyRef in gate.PolyRefs)
        {
            // 地图建筑关卡和全局门重叠时，开门要恢复 GatePolyFlag，关门则整块不可走。
            int polyFlags = flags;
            if (gate.Open && _polyGateIndices.ContainsKey(polyRef))
                polyFlags |= GatePolyFlag;

            var status = _navMesh.SetPolyFlags(polyRef, polyFlags);
            if (status.Failed())
                Log.Warn($"[navmesh] 关卡门 {gate.GateId} SetPolyFlags 失败: {status.Value}");
        }
    }
}
