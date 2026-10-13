#include <cstdio>
#include <cstring>
#include <string>
#include <vector>

#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"
#include "NavMeshBuilder.h"
#include "NavMeshDataFile.h"
#include "SelfTestData.h"

namespace
{
    void PrintRawDataSummary(const NavMeshRawData& raw)
    {
        std::printf("---- Loaded data summary ----\n");
        std::printf("vertexCount   = %d\n", raw.VertexCount());
        std::printf("triangleCount = %d\n", raw.TriangleCount());
        std::printf("linkCount     = %d\n", static_cast<int>(raw.links.size()));
        std::printf("agentHeight=%.3f agentRadius=%.3f agentMaxClimb=%.3f cellSize=%.4f cellHeight=%.4f\n",
                     raw.agentHeight, raw.agentRadius, raw.agentMaxClimb, raw.cellSize, raw.cellHeight);
    }

    void PrintBuildStats(const NavMeshBuildStats& stats)
    {
        std::printf("---- Build stats ----\n");
        std::printf("polyCount           = %d\n", stats.polyCount);
        std::printf("boundaryEdgeCount   = %d\n", stats.boundaryEdgeCount);
        std::printf("internalEdgeCount   = %d\n", stats.internalEdgeCount);
        std::printf("nonManifoldEdgeCount= %d\n", stats.nonManifoldEdgeCount);
    }

    // 对两个点分别做 findNearestPoly,再对两点间跑 findPath+findStraightPath。
    // 两点之间要确实能找到路径(说明共享边被正确识别为内部可通行边)才算验证通过。
    bool RunQueryVerification(dtNavMesh& navMesh, const float startWorld[3], const float endWorld[3])
    {
        dtNavMeshQuery query;
        if (dtStatusFailed(query.init(&navMesh, 2048)))
        {
            std::printf("[FAIL] dtNavMeshQuery::init failed\n");
            return false;
        }

        dtQueryFilter filter;
        const float halfExtents[3] = {2.0f, 4.0f, 2.0f};

        dtPolyRef startRef = 0;
        dtPolyRef endRef = 0;
        float startNearest[3];
        float endNearest[3];

        dtStatus status = query.findNearestPoly(startWorld, halfExtents, &filter, &startRef, startNearest);
        if (dtStatusFailed(status) || startRef == 0)
        {
            std::printf("[FAIL] findNearestPoly failed for start point (%.2f, %.2f, %.2f)\n",
                        startWorld[0], startWorld[1], startWorld[2]);
            return false;
        }
        std::printf("[OK] start point legality check passed, nearest=(%.3f, %.3f, %.3f) ref=%llu\n",
                    startNearest[0], startNearest[1], startNearest[2],
                    static_cast<unsigned long long>(startRef));

        status = query.findNearestPoly(endWorld, halfExtents, &filter, &endRef, endNearest);
        if (dtStatusFailed(status) || endRef == 0)
        {
            std::printf("[FAIL] findNearestPoly failed for end point (%.2f, %.2f, %.2f)\n",
                        endWorld[0], endWorld[1], endWorld[2]);
            return false;
        }
        std::printf("[OK] end point legality check passed, nearest=(%.3f, %.3f, %.3f) ref=%llu\n",
                    endNearest[0], endNearest[1], endNearest[2],
                    static_cast<unsigned long long>(endRef));

        std::vector<dtPolyRef> path(256);
        int pathCount = 0;
        status = query.findPath(startRef, endRef, startNearest, endNearest, &filter,
                                 path.data(), &pathCount, static_cast<int>(path.size()));
        if (dtStatusFailed(status) || pathCount == 0)
        {
            std::printf("[FAIL] findPath failed\n");
            return false;
        }
        std::printf("[OK] findPath succeeded, polygon count in corridor = %d\n", pathCount);

        std::vector<float> straightPath(256 * 3);
        std::vector<unsigned char> straightPathFlags(256);
        std::vector<dtPolyRef> straightPathRefs(256);
        int straightPathCount = 0;
        status = query.findStraightPath(startNearest, endNearest, path.data(), pathCount,
                                         straightPath.data(), straightPathFlags.data(), straightPathRefs.data(),
                                         &straightPathCount, static_cast<int>(straightPath.size() / 3));
        if (dtStatusFailed(status) || straightPathCount == 0)
        {
            std::printf("[FAIL] findStraightPath failed\n");
            return false;
        }

        std::printf("[OK] findStraightPath succeeded, corner count = %d\n", straightPathCount);
        for (int i = 0; i < straightPathCount; ++i)
        {
            std::printf("  corner[%d] = (%.3f, %.3f, %.3f)\n", i,
                        straightPath[static_cast<size_t>(i) * 3 + 0],
                        straightPath[static_cast<size_t>(i) * 3 + 1],
                        straightPath[static_cast<size_t>(i) * 3 + 2]);
        }
        return true;
    }

    int RunVerification(const NavMeshRawData& raw)
    {
        PrintRawDataSummary(raw);

        NavMeshBuildResult buildResult = BuildNavMesh(raw);
        if (!buildResult.success)
        {
            std::printf("[FAIL] BuildNavMesh failed: %s\n", buildResult.error.c_str());
            return 1;
        }
        std::printf("[OK] BuildNavMesh + dtNavMesh::init succeeded\n");
        PrintBuildStats(buildResult.stats);

        // 用网格里第一个和最后一个三角形的质心作为起止点:两者分别落在不同的三角形里,
        // findPath 必须跨越三角形之间的共享边才能连通,这样才能真正验证邻接关系算对了
        // (如果直接用顶点坐标,共享顶点可能让起止点落在同一个三角形上,测试就形同虚设)。
        const int triCount = raw.TriangleCount();
        auto triangleCentroid = [&raw](int triIndex, float out[3]) {
            const int v0 = raw.indices[static_cast<size_t>(triIndex) * 3 + 0];
            const int v1 = raw.indices[static_cast<size_t>(triIndex) * 3 + 1];
            const int v2 = raw.indices[static_cast<size_t>(triIndex) * 3 + 2];
            for (int axis = 0; axis < 3; ++axis)
            {
                out[axis] = (raw.vertices[static_cast<size_t>(v0) * 3 + axis] +
                             raw.vertices[static_cast<size_t>(v1) * 3 + axis] +
                             raw.vertices[static_cast<size_t>(v2) * 3 + axis]) / 3.0f;
            }
        };

        float startWorld[3];
        float endWorld[3];
        triangleCentroid(0, startWorld);
        triangleCentroid(triCount - 1, endWorld);

        if (!RunQueryVerification(*buildResult.navMesh, startWorld, endWorld))
        {
            return 1;
        }

        std::printf("\nPoC verification passed\n");
        return 0;
    }
}

int main(int argc, char** argv)
{
    if (argc >= 2 && std::strcmp(argv[1], "--file") == 0)
    {
        if (argc < 3)
        {
            std::printf("usage: RecastNavigationServer --file <path-to-rnav-file>\n");
            return 1;
        }

        NavMeshRawData raw;
        std::string error;
        if (!LoadNavMeshDataFile(argv[2], raw, error))
        {
            std::printf("[FAIL] failed to load data file '%s': %s\n", argv[2], error.c_str());
            return 1;
        }
        return RunVerification(raw);
    }

    // 默认(或 --selftest):跑内置合成测试数据,不依赖 Unity 导出的真实文件。
    std::printf("Running built-in self-test dataset (use --file <path> to test real exported data)\n\n");
    return RunVerification(MakeSelfTestData());
}
