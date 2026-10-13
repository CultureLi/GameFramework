#include "Camera.h"

#include <cmath>

#ifdef _WIN32
#include <windows.h>
#endif
#include <GL/gl.h>

void Camera::FitToBounds(const float bmin[3], const float bmax[3])
{
    float r = (bmax[0] - bmin[0]) * (bmax[0] - bmin[0]);
    r += (bmax[1] - bmin[1]) * (bmax[1] - bmin[1]);
    r += (bmax[2] - bmin[2]) * (bmax[2] - bmin[2]);
    r = std::sqrt(r) / 2.0f;

    const float dist = r * std::sqrt(3.0f); // 和原来斜 45°/-45° 视角到中心的距离保持一致,只是改成垂直方向

    // 实测这份数据是标准的水平 NavMesh(Y 几乎不变,X/Z 才是地面范围),"正面"确实应该是
    // 从上往下俯视(看 X/Z 平面)。但 Unity 是左手坐标系,我们这边是右手坐标系(OpenGL),
    // 整条管线没有做任何坐标变换(原始 X/Y/Z 直接照搬),这意味着单纯"从正上方(+Y 一侧)往下看"
    // 在右手系里只能让 X 轴朝右、Z 轴朝下屏幕,或者 Z 轴朝上屏幕、X 轴朝左——没法同时让
    // X 轴朝右、Z 轴朝上屏幕,而这恰好是 Unity 俯视图(左手系)的习惯朝向。
    // 解法:相机摆到网格正下方(-Y 一侧)往上看,这样右手系的旋转方向正好抵消了左右手系的差异,
    // 得到 X 轴朝右、Z 轴朝上屏幕,和 Unity 俯视图一致——网格是薄片,双面绘制下从下往上看
    // 外形和从上往下看完全一样,不影响可读性。
    pos[0] = (bmax[0] + bmin[0]) / 2.0f;
    pos[1] = (bmax[1] + bmin[1]) / 2.0f - dist;
    pos[2] = (bmax[2] + bmin[2]) / 2.0f;

    camr = r * 3.0f;

    eulers[0] = -90.0f;
    eulers[1] = 0.0f;
}

void Camera::Update(float dt, const double modelviewMatrix[16], float speed)
{
    const float moveX = (moveRight - moveLeft) * speed * dt;
    const float moveY = (moveBack - moveFront) * speed * dt + scrollZoom * 2.0f;
    scrollZoom = 0.0f;

    // 和 RecastDemo main.cpp 一样,左右/前后移动量投影到当前 modelview 的基向量上(相对相机朝向移动),
    // 上下移动直接作用在世界 Y 轴上。
    pos[0] += moveX * static_cast<float>(modelviewMatrix[0]);
    pos[1] += moveX * static_cast<float>(modelviewMatrix[4]);
    pos[2] += moveX * static_cast<float>(modelviewMatrix[8]);

    pos[0] += moveY * static_cast<float>(modelviewMatrix[2]);
    pos[1] += moveY * static_cast<float>(modelviewMatrix[6]);
    pos[2] += moveY * static_cast<float>(modelviewMatrix[10]);

    pos[1] += (moveUp - moveDown) * speed * dt;
}

void Camera::ApplyModelview(double outModelviewMatrix[16]) const
{
    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();
    glRotatef(eulers[0], 1, 0, 0);
    glRotatef(eulers[1], 0, 1, 0);
    glTranslatef(-pos[0], -pos[1], -pos[2]);
    glGetDoublev(GL_MODELVIEW_MATRIX, outModelviewMatrix);
}
