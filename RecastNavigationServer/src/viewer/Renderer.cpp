#include "Renderer.h"

#include "DebugDraw.h"
#include "DetourDebugDraw.h"

namespace
{
    // 照搬 DetourDebugDraw.cpp 里 duDebugDrawNavMesh 的 off-mesh connection 绘制逻辑,
    // 但端点圆/竖直标记线的 Y 偏移、连接弧的凸起方向全部取反(+0.1f/+0.2f/0.25f -> 负数)。
    // 原版这些偏移都是朝 +Y 画的,目的是在 Unity 俯视图(+Y 朝相机)里让它们"鼓"向观察者;
    // 现在相机摆在网格下方往上看(Camera::FitToBounds 的 pitch=-90 修复),+Y 变成了远离相机的
    // 方向,原版偏移就变成了"鼓"向网格背面,看起来像画到了另一面。这里只翻转偏移方向,
    // 不改任何 vendored 库文件,也不影响 duDebugDrawNavMesh 画的三角形/边界(flags=0 跳过这段)。
    void DrawOffMeshConnectionsMirrored(duDebugDraw& dd, const dtNavMesh& navMesh)
    {
        dd.begin(DU_DRAW_LINES, 2.0f);
        for (int ti = 0; ti < navMesh.getMaxTiles(); ++ti)
        {
            const dtMeshTile* tile = navMesh.getTile(ti);
            if (!tile->header) continue;

            for (int i = 0; i < tile->header->polyCount; ++i)
            {
                const dtPoly* p = &tile->polys[i];
                if (p->getType() != DT_POLYTYPE_OFFMESH_CONNECTION) continue;

                unsigned int col;
                col = duDarkenCol(duTransCol(dd.areaToCol(p->getArea()), 220));

                const dtOffMeshConnection* con = &tile->offMeshCons[i - tile->header->offMeshBase];
                const float* va = &tile->verts[p->verts[0] * 3];
                const float* vb = &tile->verts[p->verts[1] * 3];

                bool startSet = false, endSet = false;
                for (unsigned int k = p->firstLink; k != DT_NULL_LINK; k = tile->links[k].next)
                {
                    if (tile->links[k].edge == 0) startSet = true;
                    if (tile->links[k].edge == 1) endSet = true;
                }

                unsigned int col2;

                dd.vertex(va[0], va[1], va[2], col);
                dd.vertex(con->pos[0], con->pos[1], con->pos[2], col);
                col2 = startSet ? col : duRGBA(220, 32, 16, 196);
                duAppendCircle(&dd, con->pos[0], con->pos[1] - 0.1f, con->pos[2], con->rad, col2);

                dd.vertex(vb[0], vb[1], vb[2], col);
                dd.vertex(con->pos[3], con->pos[4], con->pos[5], col);
                col2 = endSet ? col : duRGBA(220, 32, 16, 196);
                duAppendCircle(&dd, con->pos[3], con->pos[4] - 0.1f, con->pos[5], con->rad, col2);

                dd.vertex(con->pos[0], con->pos[1], con->pos[2], duRGBA(0, 48, 64, 196));
                dd.vertex(con->pos[0], con->pos[1] - 0.2f, con->pos[2], duRGBA(0, 48, 64, 196));
                dd.vertex(con->pos[3], con->pos[4], con->pos[5], duRGBA(0, 48, 64, 196));
                dd.vertex(con->pos[3], con->pos[4] - 0.2f, con->pos[5], duRGBA(0, 48, 64, 196));

                duAppendArc(&dd, con->pos[0], con->pos[1], con->pos[2], con->pos[3], con->pos[4], con->pos[5], -0.25f,
                            (con->flags & 1) ? 0.6f : 0, 0.6f, col);
            }
        }
        dd.end();
    }
}

void RenderScene(duDebugDraw& dd, const dtNavMesh& navMesh,
                  bool hasStart, const float startPos[3],
                  bool hasEnd, const float endPos[3],
                  const std::vector<float>& pathCorners,
                  float markerRadius)
{
    duDebugDrawNavMesh(&dd, navMesh, 0);
    DrawOffMeshConnectionsMirrored(dd, navMesh);

    // 配色借用 RecastDemo Tool_NavMeshTester 的起点/终点标记颜色。
    if (hasStart)
        duDebugDrawCircle(&dd, startPos[0], startPos[1], startPos[2], markerRadius, duRGBA(128, 25, 0, 192), 2.0f);
    if (hasEnd)
        duDebugDrawCircle(&dd, endPos[0], endPos[1], endPos[2], markerRadius, duRGBA(51, 102, 0, 192), 2.0f);

    const size_t cornerCount = pathCorners.size() / 3;
    if (cornerCount >= 2)
    {
        dd.begin(DU_DRAW_LINES, 3.0f);
        for (size_t i = 0; i + 1 < cornerCount; ++i)
        {
            dd.vertex(&pathCorners[i * 3], duRGBA(0, 0, 0, 220));
            dd.vertex(&pathCorners[(i + 1) * 3], duRGBA(0, 0, 0, 220));
        }
        dd.end();
    }
}
