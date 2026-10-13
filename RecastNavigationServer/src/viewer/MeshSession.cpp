#include "MeshSession.h"

#include <cstdio>
#include <limits>

#include "SelfTestData.h"

namespace
{
    void ComputeBounds(const NavMeshRawData& raw, float bmin[3], float bmax[3])
    {
        bmin[0] = bmin[1] = bmin[2] = std::numeric_limits<float>::max();
        bmax[0] = bmax[1] = bmax[2] = -std::numeric_limits<float>::max();
        const size_t vertCount = raw.vertices.size() / 3;
        for (size_t i = 0; i < vertCount; ++i)
        {
            for (int axis = 0; axis < 3; ++axis)
            {
                const float v = raw.vertices[i * 3 + static_cast<size_t>(axis)];
                if (v < bmin[axis]) bmin[axis] = v;
                if (v > bmax[axis]) bmax[axis] = v;
            }
        }
    }
}

bool MeshSession::Load(const std::string& path, Camera& camera)
{
    NavMeshRawData newRaw;
    if (path.empty())
    {
        newRaw = MakeSelfTestData();
    }
    else
    {
        std::string err;
        if (!LoadNavMeshDataFile(path, newRaw, err))
        {
            lastLoadError = "load failed: " + err;
            std::printf("[FAIL] failed to load data file '%s': %s\n", path.c_str(), err.c_str());
            return false;
        }
    }

    NavMeshBuildResult newBuildResult = BuildNavMesh(newRaw);
    if (!newBuildResult.success)
    {
        lastLoadError = "build failed: " + newBuildResult.error;
        std::printf("[FAIL] BuildNavMesh failed: %s\n", newBuildResult.error.c_str());
        return false;
    }

    // query.init() 对 newBuildResult.navMesh 做校验;dtNavMeshQuery 不可拷贝/移动,
    // 所以直接在现有 query 上原地 init(内部会自动复用/重建节点池)。unique_ptr 的 move
    // 只搬运指针值,不搬运指向的对象本身,所以就算这里先 init 成功、下面再
    // std::move(newBuildResult),query 内部持有的 dtNavMesh* 仍然有效。
    if (dtStatusFailed(query.init(newBuildResult.navMesh.get(), 2048)))
    {
        // 极端情况(节点池内存分配失败):newBuildResult 即将在函数返回时销毁,
        // 把 query 指回旧 mesh,避免悬空指针。
        if (buildResult.navMesh) query.init(buildResult.navMesh.get(), 2048);
        lastLoadError = "dtNavMeshQuery::init failed, kept previous mesh";
        std::printf("[FAIL] dtNavMeshQuery::init failed\n");
        return false;
    }

    // 全部校验通过,这里才真正提交。
    raw = std::move(newRaw);
    buildResult = std::move(newBuildResult);
    ComputeBounds(raw, bmin, bmax);
    markerRadius = raw.agentRadius > 0.0f ? raw.agentRadius : 0.3f;
    camera.FitToBounds(bmin, bmax);

    hasStart = false;
    hasEnd = false;
    pathCorners.clear();
    lastLoadError.clear();

    currentFilePath = path;
    const size_t slash = path.find_last_of("/\\");
    currentFileName = path.empty() ? "" : (slash == std::string::npos ? path : path.substr(slash + 1));

    std::printf("[OK] navmesh built: %d triangles\n", buildResult.stats.polyCount);
    return true;
}
