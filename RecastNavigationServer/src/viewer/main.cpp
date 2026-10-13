#include <cstdio>
#include <cstring>
#include <string>
#include <vector>

#ifdef _WIN32
#define NOMINMAX
#include <windows.h>
#endif

// 不需要 SDL 接管 main()(我们是普通控制台程序,不是 WinMain GUI app)。
#define SDL_MAIN_HANDLED
#include "SDL.h"

#ifdef __APPLE__
#include <OpenGL/glu.h>
#else
#include <GL/glu.h>
#endif

#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"
#include "NavMeshDataFile.h"

#include "Camera.h"
#include "DebugDrawGL.h"
#include "MeshPicker.h"
#include "Renderer.h"
#include "MeshSession.h"
#include "NavDataDiscovery.h"
#include "FileDialogWin32.h"

#include "imgui.h"
#include "imgui_impl_sdl2.h"
#include "imgui_impl_opengl2.h"

namespace
{
    constexpr float kCamMoveSpeed = 4.0f;      // Shift 按住时的慢速精确移动
    constexpr float kCamFastMoveSpeed = 22.0f; // 默认移动速度

    constexpr const char* kControlsText =
        "Controls: left-click = set start, shift+left-click = set end, right-drag = rotate camera,\n"
        "WASD/arrows = move, Q/E or PageUp/PageDown = up/down, wheel = zoom, R = reset, Esc = quit";

    struct ViewportState
    {
        int width = 0, height = 0;
        int drawableWidth = 0, drawableHeight = 0;
        int viewport[4] = {0, 0, 0, 0};
        double projectionMatrix[16] = {};
    };

    void UpdateWindowSize(SDL_Window* window, ViewportState& vp, float camr)
    {
        SDL_GetWindowSize(window, &vp.width, &vp.height);
        SDL_GL_GetDrawableSize(window, &vp.drawableWidth, &vp.drawableHeight);

        glViewport(0, 0, vp.drawableWidth, vp.drawableHeight);
        glGetIntegerv(GL_VIEWPORT, vp.viewport);

        glMatrixMode(GL_PROJECTION);
        glLoadIdentity();
        gluPerspective(50.0f, static_cast<float>(vp.width) / static_cast<float>(vp.height), 1.0f, camr);
        glGetDoublev(GL_PROJECTION_MATRIX, vp.projectionMatrix);
    }

    // 对起点/终点跑 findPath+findStraightPath,结果(拍平的 xyz 数组)写入 outCorners。
    // 失败时只打印提示,不清空 outCorners 之外的任何状态(呼叫方决定要不要保留旧路径)。
    bool RunPathQuery(dtNavMeshQuery& query, const float startWorld[3], const float endWorld[3], std::vector<float>& outCorners)
    {
        outCorners.clear();

        dtQueryFilter filter;
        const float halfExtents[3] = {2.0f, 4.0f, 2.0f};

        dtPolyRef startRef = 0;
        dtPolyRef endRef = 0;
        float startNearest[3];
        float endNearest[3];

        if (dtStatusFailed(query.findNearestPoly(startWorld, halfExtents, &filter, &startRef, startNearest)) || startRef == 0)
        {
            std::printf("[viewer] start point is not on the navmesh\n");
            return false;
        }
        if (dtStatusFailed(query.findNearestPoly(endWorld, halfExtents, &filter, &endRef, endNearest)) || endRef == 0)
        {
            std::printf("[viewer] end point is not on the navmesh\n");
            return false;
        }

        std::vector<dtPolyRef> path(256);
        int pathCount = 0;
        if (dtStatusFailed(query.findPath(startRef, endRef, startNearest, endNearest, &filter,
                                           path.data(), &pathCount, static_cast<int>(path.size()))) ||
            pathCount == 0)
        {
            std::printf("[viewer] findPath failed (no corridor between start and end)\n");
            return false;
        }

        std::vector<float> straightPath(256 * 3);
        std::vector<unsigned char> straightPathFlags(256);
        std::vector<dtPolyRef> straightPathRefs(256);
        int straightPathCount = 0;
        if (dtStatusFailed(query.findStraightPath(startNearest, endNearest, path.data(), pathCount,
                                                   straightPath.data(), straightPathFlags.data(), straightPathRefs.data(),
                                                   &straightPathCount, static_cast<int>(straightPath.size() / 3))) ||
            straightPathCount == 0)
        {
            std::printf("[viewer] findStraightPath failed\n");
            return false;
        }

        outCorners.assign(straightPath.begin(), straightPath.begin() + static_cast<size_t>(straightPathCount) * 3);
        std::printf("[viewer] path found, %d corners\n", straightPathCount);
        return true;
    }
}

int main(int argc, char** argv)
{
    std::string explicitFile;
    bool haveExplicitFile = false;
    if (argc >= 2 && std::strcmp(argv[1], "--file") == 0)
    {
        if (argc < 3)
        {
            std::printf("usage: RecastNavigationViewer --file <path-to-rnav-file>\n");
            return 1;
        }
        explicitFile = argv[2];
        haveExplicitFile = true;
    }

    if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_TIMER) != 0)
    {
        std::printf("Could not initialize SDL.\nError: %s\n", SDL_GetError());
        return 1;
    }

    SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);
    SDL_GL_SetAttribute(SDL_GL_DEPTH_SIZE, 24);
    SDL_GL_SetAttribute(SDL_GL_RED_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_GREEN_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_BLUE_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_ALPHA_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_MULTISAMPLEBUFFERS, 1);
    SDL_GL_SetAttribute(SDL_GL_MULTISAMPLESAMPLES, 4);

    SDL_Window* window = SDL_CreateWindow(
        "RecastNavigationServer Viewer",
        SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
        1280, 800,
        SDL_WINDOW_OPENGL | SDL_WINDOW_RESIZABLE | SDL_WINDOW_ALLOW_HIGHDPI);
    if (!window)
    {
        std::printf("Could not create SDL window.\nError: %s\n", SDL_GetError());
        return 1;
    }

    SDL_GLContext glContext = SDL_GL_CreateContext(window);
    if (!glContext)
    {
        std::printf("Could not create GL context.\nError: %s\n", SDL_GetError());
        return 1;
    }
    SDL_GL_MakeCurrent(window, glContext);
    SDL_GL_SetSwapInterval(1);

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGui::GetIO().IniFilename = nullptr; // 固定位置的 overlay,不需要落地 imgui.ini
    ImGui_ImplSDL2_InitForOpenGL(window, glContext);
    ImGui_ImplOpenGL2_Init();

    const std::string navDataDir = FindNavDataDir();

    std::string initialPath;
    if (haveExplicitFile)
    {
        initialPath = explicitFile;
    }
    else
    {
        initialPath = PickFirstRnavFile(navDataDir);
        if (initialPath.empty())
            std::printf("[viewer] no NavData/ folder or no .rnav files found, using built-in self-test data\n");
        else
            std::printf("[viewer] auto-loading '%s'\n", initialPath.c_str());
    }

    Camera camera;
    MeshSession session;
    if (!session.Load(initialPath, camera))
    {
        const std::string firstError = session.lastLoadError;
        std::printf("[viewer] falling back to built-in self-test data\n");
        session.Load("", camera); // 自测数据永远能成功构建
        session.lastLoadError = firstError + " (showing built-in test data)";
    }

    ViewportState vp;
    UpdateWindowSize(window, vp, camera.camr);

    glEnable(GL_DEPTH_TEST);
    glDepthFunc(GL_LEQUAL);
    // 不开 GL_CULL_FACE:导出的三角形(连自带的 self-test 数据)按 Unity 的顶点顺序走,从上往下看
    // 法线朝下(CW),默认 GL_CCW 前面约定会把它们当成背面裁掉——结果是必须转到网格背面才能看到,
    // 和 Unity 场景视图里navmesh gizmo(双面绘制,不裁剪)对不上。这是纯调试可视化工具,双面绘制更合理。
    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

    DebugDrawGL dd;

    Uint32 prevFrameTime = SDL_GetTicks();
    bool done = false;
    while (!done)
    {
        // 上一帧 ImGui::NewFrame() 根据当时的控件布局算出来的,固定布局下这一帧的滞后不影响判断。
        const bool imguiWantsMouse = ImGui::GetIO().WantCaptureMouse;

        bool processHitTest = false;
        bool processHitTestShift = false;

        SDL_Event event;
        while (SDL_PollEvent(&event))
        {
            ImGui_ImplSDL2_ProcessEvent(&event);

            switch (event.type)
            {
            case SDL_KEYDOWN:
                switch (event.key.keysym.sym)
                {
                case SDLK_ESCAPE:
                    done = true;
                    break;
                case SDLK_r:
                    session.hasStart = false;
                    session.hasEnd = false;
                    session.pathCorners.clear();
                    std::printf("[viewer] reset start/end points\n");
                    break;
                default:
                    break;
                }
                break;

            case SDL_MOUSEWHEEL:
                if (!imguiWantsMouse)
                    camera.scrollZoom += static_cast<float>(event.wheel.y);
                break;

            case SDL_MOUSEBUTTONDOWN:
                if (event.button.button == SDL_BUTTON_RIGHT && !imguiWantsMouse)
                {
                    camera.isRotatingCamera = true;
                    camera.origMousePos[0] = static_cast<float>(event.button.x);
                    camera.origMousePos[1] = static_cast<float>(vp.height - 1 - event.button.y);
                    camera.origEulers[0] = camera.eulers[0];
                    camera.origEulers[1] = camera.eulers[1];
                }
                break;

            case SDL_MOUSEBUTTONUP:
                if (event.button.button == SDL_BUTTON_RIGHT)
                {
                    // 右键只负责旋转相机,不做点选。不受 imguiWantsMouse 门控,避免卡在"正在旋转"状态。
                    camera.isRotatingCamera = false;
                }
                else if (event.button.button == SDL_BUTTON_LEFT && !imguiWantsMouse)
                {
                    processHitTest = true;
                    processHitTestShift = (SDL_GetModState() & KMOD_SHIFT) != 0;
                }
                break;

            case SDL_MOUSEMOTION:
                if (camera.isRotatingCamera)
                {
                    const float mx = static_cast<float>(event.motion.x);
                    const float my = static_cast<float>(vp.height - 1 - event.motion.y);
                    const float dx = mx - camera.origMousePos[0];
                    const float dy = my - camera.origMousePos[1];
                    camera.eulers[0] = camera.origEulers[0] - dy * 0.25f;
                    camera.eulers[1] = camera.origEulers[1] + dx * 0.25f;
                }
                break;

            case SDL_WINDOWEVENT:
                if (event.window.event == SDL_WINDOWEVENT_RESIZED)
                {
                    UpdateWindowSize(window, vp, camera.camr);
                }
                break;

            case SDL_QUIT:
                done = true;
                break;

            default:
                break;
            }
        }

        const Uint32 time = SDL_GetTicks();
        const float dt = static_cast<float>(time - prevFrameTime) / 1000.0f;
        prevFrameTime = time;

        glClearColor(0.3f, 0.3f, 0.32f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        ImGui_ImplOpenGL2_NewFrame();
        ImGui_ImplSDL2_NewFrame();
        ImGui::NewFrame();

        ImGui::SetNextWindowPos(ImVec2(10.0f, 10.0f), ImGuiCond_Always);
        ImGui::SetNextWindowBgAlpha(0.55f);
        ImGui::Begin("Overlay", nullptr,
                      ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoMove |
                          ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoSavedSettings | ImGuiWindowFlags_AlwaysAutoResize);

        ImGui::TextUnformatted(kControlsText);
        ImGui::Separator();

        if (ImGui::Button("Load..."))
        {
            const std::string chosen = OpenRnavFileDialog(window, navDataDir);
            if (!chosen.empty())
            {
                if (session.Load(chosen, camera))
                {
                    UpdateWindowSize(window, vp, camera.camr);
                    std::printf("[viewer] loaded '%s'\n", chosen.c_str());
                }
                else
                {
                    std::printf("[viewer] failed to load '%s': %s\n", chosen.c_str(), session.lastLoadError.c_str());
                }
            }
        }
        ImGui::SameLine();
        ImGui::Text("Loaded: %s",
                    session.currentFileName.empty() ? "(built-in self-test data)" : session.currentFileName.c_str());

        if (!session.lastLoadError.empty())
            ImGui::TextColored(ImVec4(1.0f, 0.35f, 0.35f, 1.0f), "Error: %s", session.lastLoadError.c_str());

        ImGui::End();

        double modelviewMatrix[16];
        camera.ApplyModelview(modelviewMatrix);

        int mouseLogicalX, mouseLogicalY;
        SDL_GetMouseState(&mouseLogicalX, &mouseLogicalY);
        const float scaleX = static_cast<float>(vp.drawableWidth) / static_cast<float>(vp.width);
        const float scaleY = static_cast<float>(vp.drawableHeight) / static_cast<float>(vp.height);
        const float mouseX = mouseLogicalX * scaleX;
        const float mouseY = vp.drawableHeight - mouseLogicalY * scaleY;

        double ux, uy, uz;
        float rayStart[3];
        float rayEnd[3];
        gluUnProject(mouseX, mouseY, 0.0, modelviewMatrix, vp.projectionMatrix, vp.viewport, &ux, &uy, &uz);
        rayStart[0] = static_cast<float>(ux);
        rayStart[1] = static_cast<float>(uy);
        rayStart[2] = static_cast<float>(uz);
        gluUnProject(mouseX, mouseY, 1.0, modelviewMatrix, vp.projectionMatrix, vp.viewport, &ux, &uy, &uz);
        rayEnd[0] = static_cast<float>(ux);
        rayEnd[1] = static_cast<float>(uy);
        rayEnd[2] = static_cast<float>(uz);

        if (processHitTest)
        {
            float hitPos[3];
            if (PickTriangle(session.raw, rayStart, rayEnd, hitPos))
            {
                dtQueryFilter filter;
                const float halfExtents[3] = {2.0f, 4.0f, 2.0f};
                dtPolyRef nearestRef = 0;
                float nearestPos[3];
                if (!dtStatusFailed(session.query.findNearestPoly(hitPos, halfExtents, &filter, &nearestRef, nearestPos)) && nearestRef != 0)
                {
                    if (processHitTestShift)
                    {
                        session.endPos[0] = nearestPos[0];
                        session.endPos[1] = nearestPos[1];
                        session.endPos[2] = nearestPos[2];
                        session.hasEnd = true;
                        std::printf("[viewer] end point set: (%.3f, %.3f, %.3f)\n", session.endPos[0], session.endPos[1], session.endPos[2]);
                    }
                    else
                    {
                        session.startPos[0] = nearestPos[0];
                        session.startPos[1] = nearestPos[1];
                        session.startPos[2] = nearestPos[2];
                        session.hasStart = true;
                        std::printf("[viewer] start point set: (%.3f, %.3f, %.3f)\n", session.startPos[0], session.startPos[1], session.startPos[2]);
                    }

                    if (session.hasStart && session.hasEnd)
                    {
                        RunPathQuery(session.query, session.startPos, session.endPos, session.pathCorners);
                    }
                }
                else
                {
                    std::printf("[viewer] clicked point is not close enough to the navmesh\n");
                }
            }
        }

        const Uint8* keystate = SDL_GetKeyboardState(nullptr);
        auto ramp = [dt](float v, bool held) {
            v += dt * 4.0f * (held ? 1.0f : -1.0f);
            if (v < 0.0f) v = 0.0f;
            if (v > 1.0f) v = 1.0f;
            return v;
        };
        camera.moveFront = ramp(camera.moveFront, keystate[SDL_SCANCODE_W] || keystate[SDL_SCANCODE_UP]);
        camera.moveLeft = ramp(camera.moveLeft, keystate[SDL_SCANCODE_A] || keystate[SDL_SCANCODE_LEFT]);
        camera.moveBack = ramp(camera.moveBack, keystate[SDL_SCANCODE_S] || keystate[SDL_SCANCODE_DOWN]);
        camera.moveRight = ramp(camera.moveRight, keystate[SDL_SCANCODE_D] || keystate[SDL_SCANCODE_RIGHT]);
        camera.moveUp = ramp(camera.moveUp, keystate[SDL_SCANCODE_Q] || keystate[SDL_SCANCODE_PAGEUP]);
        camera.moveDown = ramp(camera.moveDown, keystate[SDL_SCANCODE_E] || keystate[SDL_SCANCODE_PAGEDOWN]);

        const float speed = (SDL_GetModState() & KMOD_SHIFT) ? kCamMoveSpeed : kCamFastMoveSpeed;
        camera.Update(dt, modelviewMatrix, speed);

        RenderScene(dd, *session.buildResult.navMesh, session.hasStart, session.startPos,
                    session.hasEnd, session.endPos, session.pathCorners, session.markerRadius);

        ImGui::Render();
        ImGui_ImplOpenGL2_RenderDrawData(ImGui::GetDrawData());

        SDL_GL_SwapWindow(window);
    }

    ImGui_ImplOpenGL2_Shutdown();
    ImGui_ImplSDL2_Shutdown();
    ImGui::DestroyContext();

    SDL_GL_DeleteContext(glContext);
    SDL_DestroyWindow(window);
    SDL_Quit();
    return 0;
}
