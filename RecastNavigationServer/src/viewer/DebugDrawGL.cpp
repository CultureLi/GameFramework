#include "DebugDrawGL.h"

#ifdef _WIN32
#include <windows.h>
#endif
#include <GL/gl.h>

void DebugDrawGL::depthMask(bool state)
{
    glDepthMask(state ? GL_TRUE : GL_FALSE);
}

void DebugDrawGL::texture(bool /*state*/)
{
    // PoC 不需要贴图,duDebugDrawNavMesh 从不调用这个接口,留空即可。
}

void DebugDrawGL::begin(duDebugDrawPrimitives prim, float size)
{
    switch (prim)
    {
    case DU_DRAW_POINTS:
        glPointSize(size);
        glBegin(GL_POINTS);
        break;
    case DU_DRAW_LINES:
        glLineWidth(size);
        glBegin(GL_LINES);
        break;
    case DU_DRAW_TRIS:
        glBegin(GL_TRIANGLES);
        break;
    case DU_DRAW_QUADS:
        glBegin(GL_QUADS);
        break;
    }
}

void DebugDrawGL::vertex(const float* pos, unsigned int color)
{
    glColor4ubv((GLubyte*)&color);
    glVertex3fv(pos);
}

void DebugDrawGL::vertex(const float x, const float y, const float z, unsigned int color)
{
    glColor4ubv((GLubyte*)&color);
    glVertex3f(x, y, z);
}

void DebugDrawGL::vertex(const float* pos, unsigned int color, const float* uv)
{
    glColor4ubv((GLubyte*)&color);
    glTexCoord2fv(uv);
    glVertex3fv(pos);
}

void DebugDrawGL::vertex(const float x, const float y, const float z, unsigned int color, const float u, const float v)
{
    glColor4ubv((GLubyte*)&color);
    glTexCoord2f(u, v);
    glVertex3f(x, y, z);
}

void DebugDrawGL::end()
{
    glEnd();
    glLineWidth(1.0f);
    glPointSize(1.0f);
}
