using DotRecast.Core.Numerics;
using DotRecast.Detour;
using GameServer.Utility;

namespace GameServer.Pathfinding;

/// <summary>
/// 导航网格运行时封装：加载 .rnav、构建 Detour 导航网格、执行寻路查询。
/// DtNavMeshQuery 不是线程安全的，这里用锁串行化所有查询。
/// </summary>
public sealed class NavMeshWorld
{
    private static readonly RcVec3f QueryExtents = new(2f, 4f, 2f);
    private const int MaxPath = 256;
    private const int MaxStraightPath = 256;

    private readonly DtNavMesh _navMesh;
    private readonly DtNavMeshQuery _query;
    private readonly DtQueryDefaultFilter _filter;
    private readonly object _queryLock = new();

    private NavMeshWorld(DtNavMesh navMesh, RcVec3f spawnPoint, NavMeshBuildStats stats)
    {
        _navMesh = navMesh;
        _query = new DtNavMeshQuery(navMesh);
        _filter = new DtQueryDefaultFilter();
        _filter.SetIncludeFlags(1);
        SpawnPoint = spawnPoint;
        Stats = stats;
    }

    public RcVec3f SpawnPoint { get; }

    public NavMeshBuildStats Stats { get; }

    public static NavMeshWorld Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"导航数据文件不存在: {path}", path);

        var raw = NavMeshFileReader.Load(path);
        var result = NavMeshBuilder.Build(raw);
        var stats = result.Stats;

        Log.Info($"[navmesh] 加载完成 {path}");
        Log.Info($"[navmesh] 顶点={raw.VertexCount} 三角形={raw.TriangleCount} Link={raw.Links.Count} " +
                 $"poly={stats.PolyCount} 边界边={stats.BoundaryEdgeCount} 内部边={stats.InternalEdgeCount} " +
                 $"非流形边={stats.NonManifoldEdgeCount}");
        Log.Info($"[navmesh] 出生点 ({result.SpawnPoint.X:F3}, {result.SpawnPoint.Y:F3}, {result.SpawnPoint.Z:F3})");

        return new NavMeshWorld(result.NavMesh, result.SpawnPoint, stats);
    }

    /// <summary>
    /// 求从 start 到 end 的直线路径。成功返回 true，corners 至少包含起点和终点。
    /// </summary>
    public bool FindPath(RcVec3f start, RcVec3f end, out List<RcVec3f> corners, out string error)
    {
        corners = new List<RcVec3f>();
        error = string.Empty;

        lock (_queryLock)
        {
            var startStatus = _query.FindNearestPoly(
                start, QueryExtents, _filter, out var startRef, out var startNearest, out _);
            if (startStatus.Failed() || startRef == 0)
            {
                error = "起点不在导航网格上";
                return false;
            }

            var endStatus = _query.FindNearestPoly(
                end, QueryExtents, _filter, out var endRef, out var endNearest, out _);
            if (endStatus.Failed() || endRef == 0)
            {
                error = "目标点不在导航网格上";
                return false;
            }

            var path = new long[MaxPath];
            var pathStatus = _query.FindPath(
                startRef, endRef, startNearest, endNearest, _filter, path, out var pathCount, path.Length);
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
}
