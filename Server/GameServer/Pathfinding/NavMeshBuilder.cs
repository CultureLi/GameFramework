using DotRecast.Core.Numerics;
using DotRecast.Detour;
using GameServer.Utility;

namespace GameServer.Pathfinding;

public readonly record struct NavMeshBuildStats(
    int PolyCount,
    int BoundaryEdgeCount,
    int InternalEdgeCount,
    int NonManifoldEdgeCount);

public sealed record NavMeshBuildResult(
    DtNavMesh NavMesh,
    NavMeshBuildStats Stats,
    RcVec3f SpawnPoint);

/// <summary>
/// 把 Unity 导出的三角形网格组装成 Detour 可查询的导航网格。
/// 移植自 RecastNavigationServer/src/NavMeshBuilder.cpp，查询部分使用 DotRecast（Recast Navigation 的 C# 移植）。
///
/// 关键点：
///   1. 顶点按体素精度量化后焊接，修复 Unity CalculateTriangulation 在 tile 接缝处索引不共享的问题；
///   2. 用焊接后的索引计算三角形邻接关系；
///   3. 组装 DtNavMeshCreateParams，交给 DotRecast 的 DtNavMeshBuilder 生成 DtMeshData。
/// </summary>
public static class NavMeshBuilder
{
    private const int Nvp = 3;
    private const int NoNeighbor = 0xffff;

    public static NavMeshBuildResult Build(NavMeshRawData raw)
    {
        var rawVertCount = raw.VertexCount;
        var rawTriCount = raw.TriangleCount;
        if (rawVertCount < 3 || rawTriCount < 1)
            throw new InvalidDataException("导航数据顶点或三角形数量不足");

        var cellSize = raw.CellSize > 0f ? raw.CellSize : 0.1f;
        var cellHeight = raw.CellHeight > 0f ? raw.CellHeight : 0.1f;

        var boundsMin = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
        var boundsMax = new[] { float.MinValue, float.MinValue, float.MinValue };
        for (var i = 0; i < rawVertCount; i++)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                var value = raw.Vertices[i * 3 + axis];
                boundsMin[axis] = Math.Min(boundsMin[axis], value);
                boundsMax[axis] = Math.Max(boundsMax[axis], value);
            }
        }

        // 1. 顶点量化 + 焊接
        var quantized = new ushort[rawVertCount * 3];
        for (var i = 0; i < rawVertCount; i++)
        {
            quantized[i * 3 + 0] = Quantize(raw.Vertices[i * 3 + 0], boundsMin[0], cellSize);
            quantized[i * 3 + 1] = Quantize(raw.Vertices[i * 3 + 1], boundsMin[1], cellHeight);
            quantized[i * 3 + 2] = Quantize(raw.Vertices[i * 3 + 2], boundsMin[2], cellSize);
        }

        var weldedIndex = new int[rawVertCount];
        var weldedVerts = new List<int>();
        var seen = new Dictionary<(int, int, int), int>();
        for (var i = 0; i < rawVertCount; i++)
        {
            var key = (quantized[i * 3 + 0], quantized[i * 3 + 1], quantized[i * 3 + 2]);
            if (seen.TryGetValue(key, out var welded))
            {
                weldedIndex[i] = welded;
                continue;
            }

            var newIndex = weldedVerts.Count / 3;
            weldedVerts.Add(key.Item1);
            weldedVerts.Add(key.Item2);
            weldedVerts.Add(key.Item3);
            seen[key] = newIndex;
            weldedIndex[i] = newIndex;
        }

        // 2. 过滤退化三角形，并计算邻接
        var validTris = new List<(int V0, int V1, int V2, byte Area, int RawTriangle)>();
        for (var t = 0; t < rawTriCount; t++)
        {
            var v0 = weldedIndex[raw.Indices[t * 3 + 0]];
            var v1 = weldedIndex[raw.Indices[t * 3 + 1]];
            var v2 = weldedIndex[raw.Indices[t * 3 + 2]];
            if (v0 == v1 || v1 == v2 || v2 == v0)
            {
                Log.Warn($"[navmesh] 三角形 {t} 焊接后退化，已跳过");
                continue;
            }

            validTris.Add((v0, v1, v2, raw.Areas[t], t));
        }

        if (validTris.Count == 0)
            throw new InvalidDataException("导航数据没有有效三角形");

        var edgeMap = new Dictionary<(int, int), List<(int Tri, int Slot)>>();
        for (var t = 0; t < validTris.Count; t++)
        {
            var (v0, v1, v2, _, _) = validTris[t];
            AddEdge(edgeMap, EdgeKey(v0, v1), (t, 0));
            AddEdge(edgeMap, EdgeKey(v1, v2), (t, 1));
            AddEdge(edgeMap, EdgeKey(v2, v0), (t, 2));
        }

        var polys = new int[validTris.Count * 2 * Nvp];
        Array.Fill(polys, NoNeighbor);
        for (var t = 0; t < validTris.Count; t++)
        {
            var (v0, v1, v2, _, _) = validTris[t];
            polys[t * 2 * Nvp + 0] = v0;
            polys[t * 2 * Nvp + 1] = v1;
            polys[t * 2 * Nvp + 2] = v2;
        }

        var boundaryEdges = 0;
        var internalEdges = 0;
        var nonManifoldEdges = 0;
        foreach (var refs in edgeMap.Values)
        {
            if (refs.Count == 1)
            {
                boundaryEdges++;
                continue;
            }

            if (refs.Count == 2)
            {
                internalEdges++;
            }
            else
            {
                nonManifoldEdges++;
                Log.Warn($"[navmesh] 非流形边，被 {refs.Count} 个三角形引用");
            }

            var a = refs[0];
            var b = refs[1];
            polys[a.Tri * 2 * Nvp + Nvp + a.Slot] = b.Tri;
            polys[b.Tri * 2 * Nvp + Nvp + b.Slot] = a.Tri;
        }

        var polyAreas = validTris.Select(t => (int)t.Area).ToArray();
        var polyFlags = Enumerable.Repeat(1, validTris.Count).ToArray();

        // 3. off-mesh links（NavMeshLink）
        var linkCount = raw.Links.Count;
        var offMeshVerts = new float[linkCount * 6];
        var offMeshRad = new float[linkCount];
        var offMeshFlags = new int[linkCount];
        var offMeshAreas = new int[linkCount];
        var offMeshDir = new int[linkCount];
        var offMeshUserId = new int[linkCount];
        for (var i = 0; i < linkCount; i++)
        {
            var link = raw.Links[i];
            offMeshVerts[i * 6 + 0] = link.StartPos[0];
            offMeshVerts[i * 6 + 1] = link.StartPos[1];
            offMeshVerts[i * 6 + 2] = link.StartPos[2];
            offMeshVerts[i * 6 + 3] = link.EndPos[0];
            offMeshVerts[i * 6 + 4] = link.EndPos[1];
            offMeshVerts[i * 6 + 5] = link.EndPos[2];
            offMeshRad[i] = link.Radius;
            offMeshFlags[i] = 1;
            offMeshAreas[i] = link.Area;
            offMeshDir[i] = link.Bidirectional ? 1 : 0;
            offMeshUserId[i] = i;
        }

        var createParams = new DtNavMeshCreateParams
        {
            verts = weldedVerts.ToArray(),
            vertCount = weldedVerts.Count / 3,
            polys = polys,
            polyFlags = polyFlags,
            polyAreas = polyAreas,
            polyCount = validTris.Count,
            nvp = Nvp,
            detailMeshes = null,
            detailVerts = null,
            detailVertsCount = 0,
            detailTris = null,
            detailTriCount = 0,
            offMeshConVerts = offMeshVerts,
            offMeshConRad = offMeshRad,
            offMeshConFlags = offMeshFlags,
            offMeshConAreas = offMeshAreas,
            offMeshConDir = offMeshDir,
            offMeshConUserID = offMeshUserId,
            offMeshConCount = linkCount,
            userId = 0,
            tileX = 0,
            tileZ = 0,
            tileLayer = 0,
            bmin = new RcVec3f(boundsMin[0], boundsMin[1], boundsMin[2]),
            bmax = new RcVec3f(boundsMax[0], boundsMax[1], boundsMax[2]),
            walkableHeight = raw.AgentHeight,
            walkableRadius = raw.AgentRadius,
            walkableClimb = raw.AgentMaxClimb,
            cs = cellSize,
            ch = cellHeight,
            buildBvTree = true
        };

        var meshData = DtNavMeshBuilder.CreateNavMeshData(createParams);
        if (meshData == null)
            throw new InvalidDataException("DtNavMeshBuilder.CreateNavMeshData 返回 null");

        var navMesh = new DtNavMesh();
        var status = navMesh.Init(meshData, Nvp, 0);
        if (status.Failed())
            throw new InvalidDataException($"DtNavMesh.Init 失败: {status.Value}");

        var stats = new NavMeshBuildStats(validTris.Count, boundaryEdges, internalEdges, nonManifoldEdges);
        var spawn = ComputeSpawnPoint(raw, validTris);
        return new NavMeshBuildResult(navMesh, stats, spawn);
    }

    private static RcVec3f ComputeSpawnPoint(
        NavMeshRawData raw, List<(int V0, int V1, int V2, byte Area, int RawTriangle)> validTris)
    {
        // 用第一个有效三角形的质心作为出生点；FindNearestPoly 会再做一次吸附。
        var tri = validTris[0].RawTriangle;
        var i0 = raw.Indices[tri * 3 + 0] * 3;
        var i1 = raw.Indices[tri * 3 + 1] * 3;
        var i2 = raw.Indices[tri * 3 + 2] * 3;
        return new RcVec3f(
            (raw.Vertices[i0 + 0] + raw.Vertices[i1 + 0] + raw.Vertices[i2 + 0]) / 3f,
            (raw.Vertices[i0 + 1] + raw.Vertices[i1 + 1] + raw.Vertices[i2 + 1]) / 3f,
            (raw.Vertices[i0 + 2] + raw.Vertices[i1 + 2] + raw.Vertices[i2 + 2]) / 3f);
    }

    private static ushort Quantize(float value, float origin, float cell)
    {
        var quantized = MathF.Floor((value - origin) / cell + 0.5f);
        if (quantized < 0f)
            return 0;
        if (quantized > 65535f)
            return 65535;
        return (ushort)quantized;
    }

    private static (int, int) EdgeKey(int a, int b) => a < b ? (a, b) : (b, a);

    private static void AddEdge(
        Dictionary<(int, int), List<(int, int)>> map, (int, int) key, (int, int) value)
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = new List<(int, int)>();
            map[key] = list;
        }
        list.Add(value);
    }
}
