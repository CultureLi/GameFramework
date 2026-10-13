#pragma once

#include "NavMeshDataFile.h"

// 对 NavMeshRawData 做鼠标射线和三角形的暴力求交(PoC 规模,三角形数量小,
// 不需要 InputGeom 那层 BVH/PartitionedMesh 加速)。
// rayStart/rayEnd 是射线段的两端(近平面/远平面上的点,见 gluUnProject)。
// 命中时把世界坐标写入 outHitPos,返回离 rayStart 最近的那个交点。
bool PickTriangle(const NavMeshRawData& raw, const float rayStart[3], const float rayEnd[3], float outHitPos[3]);
