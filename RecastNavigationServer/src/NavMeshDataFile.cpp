#include "NavMeshDataFile.h"

#include <cstdio>
#include <cstring>

namespace
{
    bool ReadBytes(FILE* f, void* dst, size_t size, std::string& outError)
    {
        if (fread(dst, 1, size, f) != size)
        {
            outError = "unexpected end of file";
            return false;
        }
        return true;
    }

    template <typename T>
    bool ReadValue(FILE* f, T& value, std::string& outError)
    {
        return ReadBytes(f, &value, sizeof(T), outError);
    }
}

bool LoadNavMeshDataFile(const std::string& path, NavMeshRawData& outData, std::string& outError)
{
    FILE* f = nullptr;
#ifdef _WIN32
    if (fopen_s(&f, path.c_str(), "rb") != 0 || f == nullptr)
#else
    f = fopen(path.c_str(), "rb");
    if (f == nullptr)
#endif
    {
        outError = "failed to open file: " + path;
        return false;
    }

    char magic[4];
    if (!ReadBytes(f, magic, 4, outError) || std::memcmp(magic, "RNAV", 4) != 0)
    {
        fclose(f);
        outError = "bad magic, expected 'RNAV'";
        return false;
    }

    uint32_t version = 0;
    if (!ReadValue(f, version, outError))
    {
        fclose(f);
        return false;
    }
    if (version != 1)
    {
        fclose(f);
        outError = "unsupported version: " + std::to_string(version);
        return false;
    }

    if (!ReadValue(f, outData.agentHeight, outError) ||
        !ReadValue(f, outData.agentRadius, outError) ||
        !ReadValue(f, outData.agentMaxClimb, outError) ||
        !ReadValue(f, outData.cellSize, outError) ||
        !ReadValue(f, outData.cellHeight, outError))
    {
        fclose(f);
        return false;
    }

    int32_t vertexCount = 0;
    if (!ReadValue(f, vertexCount, outError) || vertexCount < 0)
    {
        fclose(f);
        outError = "invalid vertexCount";
        return false;
    }
    outData.vertices.resize(static_cast<size_t>(vertexCount) * 3);
    if (!outData.vertices.empty() &&
        !ReadBytes(f, outData.vertices.data(), outData.vertices.size() * sizeof(float), outError))
    {
        fclose(f);
        return false;
    }

    int32_t triangleCount = 0;
    if (!ReadValue(f, triangleCount, outError) || triangleCount < 0)
    {
        fclose(f);
        outError = "invalid triangleCount";
        return false;
    }
    outData.indices.resize(static_cast<size_t>(triangleCount) * 3);
    if (!outData.indices.empty() &&
        !ReadBytes(f, outData.indices.data(), outData.indices.size() * sizeof(int32_t), outError))
    {
        fclose(f);
        return false;
    }

    outData.areas.resize(static_cast<size_t>(triangleCount));
    if (!outData.areas.empty() &&
        !ReadBytes(f, outData.areas.data(), outData.areas.size() * sizeof(uint8_t), outError))
    {
        fclose(f);
        return false;
    }

    int32_t linkCount = 0;
    if (!ReadValue(f, linkCount, outError) || linkCount < 0)
    {
        fclose(f);
        outError = "invalid linkCount";
        return false;
    }
    outData.links.resize(static_cast<size_t>(linkCount));
    for (int32_t i = 0; i < linkCount; ++i)
    {
        NavMeshLinkData& link = outData.links[static_cast<size_t>(i)];
        uint8_t bidirectional = 0;
        if (!ReadBytes(f, link.startPos, sizeof(link.startPos), outError) ||
            !ReadBytes(f, link.endPos, sizeof(link.endPos), outError) ||
            !ReadValue(f, link.radius, outError) ||
            !ReadValue(f, bidirectional, outError) ||
            !ReadValue(f, link.area, outError))
        {
            fclose(f);
            return false;
        }
        link.bidirectional = (bidirectional != 0);
    }

    fclose(f);

    // 顶点/三角形数据的健全性校验留给调用方(NavMeshBuilder)在构建邻接关系时做,
    // 这里只负责按格式把字节读出来。
    for (int32_t idx : outData.indices)
    {
        if (idx < 0 || idx >= vertexCount)
        {
            outError = "triangle index out of range: " + std::to_string(idx);
            return false;
        }
    }

    return true;
}
