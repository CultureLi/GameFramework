#pragma once

#include "DebugDraw.h"

// 从 RecastDemo (RecastDemo/Include/SampleInterfaces.h + Source/SampleInterfaces.cpp) 搬过来的
// duDebugDraw 的 OpenGL 固定管线实现。texture() 不需要真的贴图(duDebugDrawNavMesh 从不调用它),
// 这里保留接口但做成空实现。
class DebugDrawGL : public duDebugDraw
{
public:
    void depthMask(bool state) override;
    void texture(bool state) override;
    void begin(duDebugDrawPrimitives prim, float size = 1.0f) override;
    void vertex(const float* pos, unsigned int color) override;
    void vertex(const float* pos, unsigned int color, const float* uv) override;
    void vertex(float x, float y, float z, unsigned int color) override;
    void vertex(float x, float y, float z, unsigned int color, float u, float v) override;
    void end() override;
};
