#pragma once

#include <vector>

#include "DetourNavMesh.h"

struct duDebugDraw;

// 每帧渲染:navmesh(含 off-mesh connection) + 起点/终点标记 + 路径折线。
// pathCorners 是 findStraightPath 输出的拍平坐标数组(xyz,xyz,...),size==0 表示还没有有效路径。
void RenderScene(duDebugDraw& dd, const dtNavMesh& navMesh,
                  bool hasStart, const float startPos[3],
                  bool hasEnd, const float endPos[3],
                  const std::vector<float>& pathCorners,
                  float markerRadius);
