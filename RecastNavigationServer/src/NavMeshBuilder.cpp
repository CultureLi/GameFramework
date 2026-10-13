#include "NavMeshBuilder.h"

#include <algorithm>
#include <array>
#include <cmath>
#include <cstdio>
#include <cstring>
#include <limits>
#include <map>

#include "DetourAlloc.h"
#include "DetourNavMeshBuilder.h"
#include "DetourNavMeshQuery.h"

void DtNavMeshDeleter::operator()(dtNavMesh* mesh) const
{
    dtFreeNavMesh(mesh);
}

void DtNavMeshQueryDeleter::operator()(dtNavMeshQuery* query) const
{
    dtFreeNavMeshQuery(query);
}

namespace
{
    constexpr unsigned short kNoNeighbor = 0xffff;
    constexpr int kNvp = 3; // Unity 导出的都是三角形,每个 poly 固定 3 个顶点

    struct EdgeRef
    {
        int triIndex;
        int edgeSlot; // 0: (v0,v1) 1: (v1,v2) 2: (v2,v0)
    };

    void ComputeBounds(const std::vector<float>& vertices, float bmin[3], float bmax[3])
    {
        bmin[0] = bmin[1] = bmin[2] = std::numeric_limits<float>::max();
        bmax[0] = bmax[1] = bmax[2] = -std::numeric_limits<float>::max();
        const size_t vertCount = vertices.size() / 3;
        for (size_t i = 0; i < vertCount; ++i)
        {
            const float x = vertices[i * 3 + 0];
            const float y = vertices[i * 3 + 1];
            const float z = vertices[i * 3 + 2];
            bmin[0] = std::min(bmin[0], x);
            bmin[1] = std::min(bmin[1], y);
            bmin[2] = std::min(bmin[2], z);
            bmax[0] = std::max(bmax[0], x);
            bmax[1] = std::max(bmax[1], y);
            bmax[2] = std::max(bmax[2], z);
        }
    }

    unsigned short QuantizeAxis(float value, float origin, float cell)
    {
        const float v = (value - origin) / cell;
        const float rounded = std::floor(v + 0.5f);
        if (rounded < 0.0f) return 0;
        if (rounded > 65535.0f) return 65535;
        return static_cast<unsigned short>(rounded);
    }
}

NavMeshBuildResult BuildNavMesh(const NavMeshRawData& raw)
{
    NavMeshBuildResult result;

    const int vertCount = raw.VertexCount();
    const int triCount = raw.TriangleCount();
    if (vertCount < 3 || triCount < 1)
    {
        result.error = "not enough vertices/triangles to build a navmesh";
        return result;
    }

    const float cs = raw.cellSize > 0.0f ? raw.cellSize : 0.1f;
    const float ch = raw.cellHeight > 0.0f ? raw.cellHeight : 0.1f;

    float bmin[3];
    float bmax[3];
    ComputeBounds(raw.vertices, bmin, bmax);

    // 1. 顶点体素量化 + 焊接。Unity 的 NavMesh 内部按 tile 分块存储,CalculateTriangulation()
    //    导出整网时不保证 tile 接缝处的顶点是同一个索引——坐标相同但索引不同,如果直接按原始
    //    索引算邻接,这些本该共享的边会被误判成边界边,导致 mesh 碎成一堆孤立小块(实测过真实
    //    导出数据:33 个三角形里 73 条边界边、只有 13 条内部边,明显不正常)。这里把量化坐标
    //    相同的顶点焊接成同一个顶点再算邻接——量化后坐标相同,在体素化精度下本来就是同一个点。
    std::vector<unsigned short> quantVerts(static_cast<size_t>(vertCount) * 3);
    for (int i = 0; i < vertCount; ++i)
    {
        const float x = raw.vertices[static_cast<size_t>(i) * 3 + 0];
        const float y = raw.vertices[static_cast<size_t>(i) * 3 + 1];
        const float z = raw.vertices[static_cast<size_t>(i) * 3 + 2];
        quantVerts[static_cast<size_t>(i) * 3 + 0] = QuantizeAxis(x, bmin[0], cs);
        quantVerts[static_cast<size_t>(i) * 3 + 1] = QuantizeAxis(y, bmin[1], ch);
        quantVerts[static_cast<size_t>(i) * 3 + 2] = QuantizeAxis(z, bmin[2], cs);
    }

    std::vector<int> weldedIndex(static_cast<size_t>(vertCount));
    std::vector<unsigned short> weldedVerts;
    {
        std::map<std::array<unsigned short, 3>, int> seen;
        for (int i = 0; i < vertCount; ++i)
        {
            const std::array<unsigned short, 3> key = {
                quantVerts[static_cast<size_t>(i) * 3 + 0],
                quantVerts[static_cast<size_t>(i) * 3 + 1],
                quantVerts[static_cast<size_t>(i) * 3 + 2]};
            const auto it = seen.find(key);
            if (it != seen.end())
            {
                weldedIndex[static_cast<size_t>(i)] = it->second;
            }
            else
            {
                const int newIndex = static_cast<int>(weldedVerts.size() / 3);
                weldedVerts.push_back(key[0]);
                weldedVerts.push_back(key[1]);
                weldedVerts.push_back(key[2]);
                seen.emplace(key, newIndex);
                weldedIndex[static_cast<size_t>(i)] = newIndex;
            }
        }
    }
    const int weldedVertCount = static_cast<int>(weldedVerts.size() / 3);

    // 2. 用焊接后的索引过滤退化三角形(三个角焊接到了 <3 个不同顶点,量化后面积为 0),
    //    再做三角形邻接计算(共享边检测)。
    struct ValidTri
    {
        unsigned short v[3];
        unsigned char area;
    };
    std::vector<ValidTri> validTris;
    validTris.reserve(static_cast<size_t>(triCount));
    for (int t = 0; t < triCount; ++t)
    {
        const int wv0 = weldedIndex[static_cast<size_t>(raw.indices[static_cast<size_t>(t) * 3 + 0])];
        const int wv1 = weldedIndex[static_cast<size_t>(raw.indices[static_cast<size_t>(t) * 3 + 1])];
        const int wv2 = weldedIndex[static_cast<size_t>(raw.indices[static_cast<size_t>(t) * 3 + 2])];
        if (wv0 == wv1 || wv1 == wv2 || wv2 == wv0)
        {
            std::fprintf(stderr, "[warn] triangle %d degenerated after vertex welding, skipped\n", t);
            continue;
        }
        validTris.push_back({{static_cast<unsigned short>(wv0), static_cast<unsigned short>(wv1), static_cast<unsigned short>(wv2)},
                              raw.areas[static_cast<size_t>(t)]});
    }
    const int validTriCount = static_cast<int>(validTris.size());

    std::map<std::pair<int, int>, std::vector<EdgeRef>> edgeMap;
    auto edgeKey = [](int a, int b) {
        return a < b ? std::make_pair(a, b) : std::make_pair(b, a);
    };

    for (int t = 0; t < validTriCount; ++t)
    {
        const int v0 = validTris[static_cast<size_t>(t)].v[0];
        const int v1 = validTris[static_cast<size_t>(t)].v[1];
        const int v2 = validTris[static_cast<size_t>(t)].v[2];
        edgeMap[edgeKey(v0, v1)].push_back({t, 0});
        edgeMap[edgeKey(v1, v2)].push_back({t, 1});
        edgeMap[edgeKey(v2, v0)].push_back({t, 2});
    }

    std::vector<unsigned short> polys(static_cast<size_t>(validTriCount) * 2 * kNvp, kNoNeighbor);
    for (int t = 0; t < validTriCount; ++t)
    {
        polys[static_cast<size_t>(t) * 2 * kNvp + 0] = validTris[static_cast<size_t>(t)].v[0];
        polys[static_cast<size_t>(t) * 2 * kNvp + 1] = validTris[static_cast<size_t>(t)].v[1];
        polys[static_cast<size_t>(t) * 2 * kNvp + 2] = validTris[static_cast<size_t>(t)].v[2];
    }

    NavMeshBuildStats stats;
    for (auto& entry : edgeMap)
    {
        std::vector<EdgeRef>& refs = entry.second;
        if (refs.size() == 1)
        {
            ++stats.boundaryEdgeCount;
        }
        else if (refs.size() == 2)
        {
            ++stats.internalEdgeCount;
            const EdgeRef& a = refs[0];
            const EdgeRef& b = refs[1];
            polys[static_cast<size_t>(a.triIndex) * 2 * kNvp + kNvp + a.edgeSlot] = static_cast<unsigned short>(b.triIndex);
            polys[static_cast<size_t>(b.triIndex) * 2 * kNvp + kNvp + b.edgeSlot] = static_cast<unsigned short>(a.triIndex);
        }
        else if (refs.size() > 2)
        {
            ++stats.nonManifoldEdgeCount;
            // 非 manifold 边:只取前两个建立互相邻接,其余按边界处理(保持 kNoNeighbor),不中断构建
            const EdgeRef& a = refs[0];
            const EdgeRef& b = refs[1];
            polys[static_cast<size_t>(a.triIndex) * 2 * kNvp + kNvp + a.edgeSlot] = static_cast<unsigned short>(b.triIndex);
            polys[static_cast<size_t>(b.triIndex) * 2 * kNvp + kNvp + b.edgeSlot] = static_cast<unsigned short>(a.triIndex);
            std::fprintf(stderr, "[warn] non-manifold edge referenced by %zu triangles\n", refs.size());
        }
    }
    stats.polyCount = validTriCount;

    // 3. polyAreas / polyFlags(本次 PoC 统一按可行走处理)
    std::vector<unsigned char> polyAreas(static_cast<size_t>(validTriCount));
    for (int t = 0; t < validTriCount; ++t)
    {
        polyAreas[static_cast<size_t>(t)] = validTris[static_cast<size_t>(t)].area;
    }
    std::vector<unsigned short> polyFlags(static_cast<size_t>(validTriCount), 1);

    // 4. off-mesh connections(来自 NavMeshLink)
    const int linkCount = static_cast<int>(raw.links.size());
    std::vector<float> offMeshVerts(static_cast<size_t>(linkCount) * 6);
    std::vector<float> offMeshRad(static_cast<size_t>(linkCount));
    std::vector<unsigned short> offMeshFlags(static_cast<size_t>(linkCount), 1);
    std::vector<unsigned char> offMeshAreas(static_cast<size_t>(linkCount));
    std::vector<unsigned char> offMeshDir(static_cast<size_t>(linkCount));
    std::vector<unsigned int> offMeshUserId(static_cast<size_t>(linkCount));
    for (int i = 0; i < linkCount; ++i)
    {
        const NavMeshLinkData& link = raw.links[static_cast<size_t>(i)];
        offMeshVerts[static_cast<size_t>(i) * 6 + 0] = link.startPos[0];
        offMeshVerts[static_cast<size_t>(i) * 6 + 1] = link.startPos[1];
        offMeshVerts[static_cast<size_t>(i) * 6 + 2] = link.startPos[2];
        offMeshVerts[static_cast<size_t>(i) * 6 + 3] = link.endPos[0];
        offMeshVerts[static_cast<size_t>(i) * 6 + 4] = link.endPos[1];
        offMeshVerts[static_cast<size_t>(i) * 6 + 5] = link.endPos[2];
        offMeshRad[static_cast<size_t>(i)] = link.radius;
        offMeshAreas[static_cast<size_t>(i)] = link.area;
        offMeshDir[static_cast<size_t>(i)] = link.bidirectional ? DT_OFFMESH_CON_BIDIR : 0;
        offMeshUserId[static_cast<size_t>(i)] = static_cast<unsigned int>(i);
    }

    // 5. 组装 dtNavMeshCreateParams,调用 dtCreateNavMeshData
    dtNavMeshCreateParams params;
    std::memset(&params, 0, sizeof(params));
    params.verts = weldedVerts.data();
    params.vertCount = weldedVertCount;
    params.polys = polys.data();
    params.polyFlags = polyFlags.data();
    params.polyAreas = polyAreas.data();
    params.polyCount = validTriCount;
    params.nvp = kNvp;

    // detail mesh 不提供(可选),Unity 三角形顶点本身已经带精确高度
    params.detailMeshes = nullptr;
    params.detailVerts = nullptr;
    params.detailVertsCount = 0;
    params.detailTris = nullptr;
    params.detailTriCount = 0;

    if (linkCount > 0)
    {
        params.offMeshConVerts = offMeshVerts.data();
        params.offMeshConRad = offMeshRad.data();
        params.offMeshConFlags = offMeshFlags.data();
        params.offMeshConAreas = offMeshAreas.data();
        params.offMeshConDir = offMeshDir.data();
        params.offMeshConUserID = offMeshUserId.data();
        params.offMeshConCount = linkCount;
    }

    params.walkableHeight = raw.agentHeight;
    params.walkableRadius = raw.agentRadius;
    params.walkableClimb = raw.agentMaxClimb;
    params.bmin[0] = bmin[0];
    params.bmin[1] = bmin[1];
    params.bmin[2] = bmin[2];
    params.bmax[0] = bmax[0];
    params.bmax[1] = bmax[1];
    params.bmax[2] = bmax[2];
    params.cs = cs;
    params.ch = ch;
    params.buildBvTree = true;

    unsigned char* navData = nullptr;
    int navDataSize = 0;
    if (!dtCreateNavMeshData(&params, &navData, &navDataSize))
    {
        result.error = "dtCreateNavMeshData failed (check params validity, e.g. polyCount/vertCount too large or invalid bounds)";
        return result;
    }

    dtNavMesh* navMesh = dtAllocNavMesh();
    if (navMesh == nullptr)
    {
        dtFree(navData);
        result.error = "dtAllocNavMesh failed (out of memory)";
        return result;
    }

    const dtStatus status = navMesh->init(navData, navDataSize, DT_TILE_FREE_DATA);
    if (dtStatusFailed(status))
    {
        dtFreeNavMesh(navMesh);
        dtFree(navData);
        result.error = "dtNavMesh::init failed, status=" + std::to_string(status);
        return result;
    }

    result.success = true;
    result.stats = stats;
    result.navMesh.reset(navMesh);
    return result;
}
