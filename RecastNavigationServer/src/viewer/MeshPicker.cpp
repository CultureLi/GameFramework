#include "MeshPicker.h"

#include <limits>

namespace
{
    void VSub(float out[3], const float* a, const float* b)
    {
        out[0] = a[0] - b[0];
        out[1] = a[1] - b[1];
        out[2] = a[2] - b[2];
    }

    void VCross(float out[3], const float* a, const float* b)
    {
        out[0] = a[1] * b[2] - a[2] * b[1];
        out[1] = a[2] * b[0] - a[0] * b[2];
        out[2] = a[0] * b[1] - a[1] * b[0];
    }

    float VDot(const float* a, const float* b)
    {
        return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    }

    // 照搬 RecastDemo InputGeom.cpp 的 intersectSegmentTriangle,但去掉了原版 "d <= 0 就剔除"
    // 的单面检测:原版假定三角形法线(由 a,b,c 的绕序决定)朝向射线起点才算命中,等价于渲染里的
    // 背面剔除。但场景渲染是双面绘制的(没开 GL_CULL_FACE,和 Unity navmesh gizmo 一致),如果
    // 拾取仍然单面,就会出现"从网格背面能点,从正面(视觉对的那一面)点不到"的不一致。这里把
    // t/v/w 都除以 d 变成归一化的重心坐标再比较范围,这样不管 d 正负(也就是射线从哪一侧穿过
    // 三角形)都能正确命中,只有 d 恰好为 0(射线和三角形平面平行)才算不命中。
    bool IntersectSegmentTriangle(const float* sp, const float* sq, const float* a, const float* b, const float* c, float& t)
    {
        float ab[3];
        VSub(ab, b, a);
        float ac[3];
        VSub(ac, c, a);
        float qp[3];
        VSub(qp, sp, sq);

        float norm[3];
        VCross(norm, ab, ac);

        float d = VDot(qp, norm);
        if (d == 0.0f)
            return false;

        float ap[3];
        VSub(ap, sp, a);
        const float tt = VDot(ap, norm) / d;
        if (tt < 0.0f || tt > 1.0f)
            return false;

        float e[3];
        VCross(e, qp, ap);
        const float v = VDot(ac, e) / d;
        if (v < 0.0f || v > 1.0f)
            return false;
        const float w = -VDot(ab, e) / d;
        if (w < 0.0f || v + w > 1.0f)
            return false;

        t = tt;
        return true;
    }
}

bool PickTriangle(const NavMeshRawData& raw, const float rayStart[3], const float rayEnd[3], float outHitPos[3])
{
    bool hit = false;
    float bestT = std::numeric_limits<float>::max();

    const int triCount = raw.TriangleCount();
    for (int t = 0; t < triCount; ++t)
    {
        const int i0 = raw.indices[static_cast<size_t>(t) * 3 + 0];
        const int i1 = raw.indices[static_cast<size_t>(t) * 3 + 1];
        const int i2 = raw.indices[static_cast<size_t>(t) * 3 + 2];
        const float* v0 = &raw.vertices[static_cast<size_t>(i0) * 3];
        const float* v1 = &raw.vertices[static_cast<size_t>(i1) * 3];
        const float* v2 = &raw.vertices[static_cast<size_t>(i2) * 3];

        float hitT;
        if (IntersectSegmentTriangle(rayStart, rayEnd, v0, v1, v2, hitT))
        {
            if (hitT < bestT)
            {
                bestT = hitT;
                hit = true;
            }
        }
    }

    if (hit)
    {
        for (int axis = 0; axis < 3; ++axis)
            outHitPos[axis] = rayStart[axis] + (rayEnd[axis] - rayStart[axis]) * bestT;
    }
    return hit;
}
