#pragma once

// 最小 orbit 相机,状态和交互方式照搬 RecastDemo(main.cpp 的 AppState):
// 位置 cameraPos + 俯仰/偏航角 cameraEulers,每帧用 glRotatef+glTranslatef 更新 modelview。
struct Camera
{
    float pos[3] = {0.0f, 0.0f, 0.0f};
    float eulers[2] = {-90.0f, 0.0f}; // eulers[0]=pitch(绕X), eulers[1]=yaw(绕Y);-90/0=俯视(正面,从网格下方往上看以抵消左右手系差异)
    float moveFront = 0.0f, moveBack = 0.0f, moveLeft = 0.0f, moveRight = 0.0f, moveUp = 0.0f, moveDown = 0.0f;
    float scrollZoom = 0.0f;
    float camr = 1000.0f; // 场景半径估计,用于远裁剪面/雾效距离(照搬 AppState::camr)

    bool isRotatingCamera = false;
    float origMousePos[2] = {0.0f, 0.0f};
    float origEulers[2] = {0.0f, 0.0f};

    // 用网格包围盒把相机摆到一个能看到全部网格的位置(照搬 AppState::resetCamera 的思路)。
    void FitToBounds(const float bmin[3], const float bmax[3]);

    // 根据 WASD/方向键+滚轮状态,在当前 modelview 基向量上平移相机(照搬 main.cpp 的键盘平移代码)。
    // speed: 像 RecastDemo 一样默认快速移动(22/s),按住 Shift 切换成慢速精确移动(4/s)。
    void Update(float dt, const double modelviewMatrix[16], float speed);

    // 应用到当前 GL_MODELVIEW 矩阵(glRotatef+glTranslatef),并取出结果供拾取使用。
    void ApplyModelview(double outModelviewMatrix[16]) const;
};
