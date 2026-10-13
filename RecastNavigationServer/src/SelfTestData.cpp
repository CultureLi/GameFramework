#include "SelfTestData.h"

// 一个 2x1 的矩形平面,拆成 2 个共享一条边的三角形。
//
//   v3(0,0,1) ---- v2(1,0,1)
//      |  \           |
//      |    \         |
//   v0(0,0,0) ---- v1(1,0,0)
//
// 三角形 0: v0, v1, v2   三角形 1: v0, v2, v3
// 共享边 (v0,v2) 应该被识别为内部边,其余 4 条边是边界边。
NavMeshRawData MakeSelfTestData()
{
    NavMeshRawData data;
    data.agentHeight = 2.0f;
    data.agentRadius = 0.5f;
    data.agentMaxClimb = 0.4f;
    data.cellSize = 0.1667f;
    data.cellHeight = 0.1667f;

    data.vertices = {
        0.0f, 0.0f, 0.0f, // v0
        1.0f, 0.0f, 0.0f, // v1
        1.0f, 0.0f, 1.0f, // v2
        0.0f, 0.0f, 1.0f, // v3
    };
    data.indices = {
        0, 1, 2,
        0, 2, 3,
    };
    data.areas = {0, 0};

    // 一个简单的 NavMeshLink:从矩形中心跳到矩形外一个点(仅用于验证组装流程不崩溃,
    // 不要求几何上有实际意义)。
    NavMeshLinkData link{};
    link.startPos[0] = 0.5f; link.startPos[1] = 0.0f; link.startPos[2] = 0.25f;
    link.endPos[0] = 0.5f; link.endPos[1] = 0.0f; link.endPos[2] = 0.75f;
    link.radius = 0.3f;
    link.bidirectional = true;
    link.area = 0;
    data.links.push_back(link);

    return data;
}
