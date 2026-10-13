#pragma once

#include <cstdint>
#include <string>
#include <vector>

// Unity 导出的 NavMeshLink 数据
struct NavMeshLinkData
{
    float startPos[3];
    float endPos[3];
    float radius;
    bool bidirectional;
    uint8_t area;
};

// 从 .rnav 文件读出的原始数据(未经任何加工,坐标仍是世界坐标)
struct NavMeshRawData
{
    // Header / 烘焙参数
    float agentHeight = 0.0f;
    float agentRadius = 0.0f;
    float agentMaxClimb = 0.0f;
    float cellSize = 0.0f;
    float cellHeight = 0.0f;

    // Mesh
    std::vector<float> vertices;   // size = vertexCount * 3
    std::vector<int32_t> indices;  // size = triangleCount * 3
    std::vector<uint8_t> areas;    // size = triangleCount

    // Links
    std::vector<NavMeshLinkData> links;

    int VertexCount() const { return static_cast<int>(vertices.size() / 3); }
    int TriangleCount() const { return static_cast<int>(indices.size() / 3); }
};

// 读取 .rnav 二进制文件。成功返回 true,失败返回 false 并把原因写入 outError。
bool LoadNavMeshDataFile(const std::string& path, NavMeshRawData& outData, std::string& outError);
