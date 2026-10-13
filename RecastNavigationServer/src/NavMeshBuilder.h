#pragma once

#include <memory>
#include <string>
#include <vector>

#include "DetourNavMesh.h"
#include "NavMeshDataFile.h"

// 自定义删除器,保证 dtNavMesh/dtNavMeshQuery 一定走官方的 dtFreeNavMesh/dtFreeNavMeshQuery 释放
struct DtNavMeshDeleter
{
    void operator()(dtNavMesh* mesh) const;
};
struct DtNavMeshQueryDeleter
{
    void operator()(class dtNavMeshQuery* query) const;
};

// 构建过程中的统计信息,用于 main.cpp 打印验证报告
struct NavMeshBuildStats
{
    int polyCount = 0;
    int boundaryEdgeCount = 0;  // 只被一个三角形引用的边(navmesh 外轮廓)
    int internalEdgeCount = 0;  // 被恰好两个三角形共享的边(可通行的内部边)
    int nonManifoldEdgeCount = 0; // 被 3 个及以上三角形引用的异常边
};

struct NavMeshBuildResult
{
    bool success = false;
    std::string error;
    NavMeshBuildStats stats;
    std::unique_ptr<dtNavMesh, DtNavMeshDeleter> navMesh;
};

// 把 Unity 导出的三角网格(NavMeshRawData)组装成可查询的 dtNavMesh。
// 核心步骤:计算 bmin/bmax -> 顶点体素量化 -> 三角形邻接计算(共享边检测)
//          -> 组装 dtNavMeshCreateParams(含 off-mesh connection) -> dtCreateNavMeshData -> dtNavMesh::init
NavMeshBuildResult BuildNavMesh(const NavMeshRawData& raw);
