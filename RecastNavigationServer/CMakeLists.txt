cmake_minimum_required(VERSION 3.15)
project(RecastNavigationServer CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 只需要 Detour/DebugUtils,不需要 recastnavigation 自己的 Demo/Tests/Examples
set(RECASTNAVIGATION_DEMO OFF CACHE BOOL "" FORCE)
set(RECASTNAVIGATION_TESTS OFF CACHE BOOL "" FORCE)
set(RECASTNAVIGATION_EXAMPLES OFF CACHE BOOL "" FORCE)

add_subdirectory("E:/Work/recastnavigation" recastnavigation)

find_package(SDL2 CONFIG REQUIRED)
find_package(OpenGL REQUIRED)

# NavMeshBuilder/NavMeshDataFile/SelfTestData:无头 CLI(RecastNavigationServer)和
# 可视化 viewer(RecastNavigationViewer)共用的核心逻辑,拆成静态库避免重复编译/维护两份。
add_library(NavMeshCore STATIC
    src/NavMeshDataFile.cpp
    src/NavMeshBuilder.cpp
    src/SelfTestData.cpp
)
target_include_directories(NavMeshCore PUBLIC src)
target_link_libraries(NavMeshCore PUBLIC RecastNavigation::Detour)

if (MSVC)
    target_compile_options(NavMeshCore PRIVATE /utf-8)
endif()

add_executable(RecastNavigationServer
    src/main.cpp
)
target_link_libraries(RecastNavigationServer PRIVATE NavMeshCore RecastNavigation::Detour)

add_executable(RecastNavigationViewer
    src/viewer/main.cpp
    src/viewer/Camera.cpp
    src/viewer/DebugDrawGL.cpp
    src/viewer/MeshPicker.cpp
    src/viewer/Renderer.cpp
    src/viewer/MeshSession.cpp
    src/viewer/NavDataDiscovery.cpp
    src/viewer/FileDialogWin32.cpp
    third_party/imgui/imgui.cpp
    third_party/imgui/imgui_draw.cpp
    third_party/imgui/imgui_widgets.cpp
    third_party/imgui/imgui_tables.cpp
    third_party/imgui/backends/imgui_impl_sdl2.cpp
    third_party/imgui/backends/imgui_impl_opengl2.cpp
)
target_include_directories(RecastNavigationViewer PRIVATE
    third_party/imgui
    third_party/imgui/backends
)
target_link_libraries(RecastNavigationViewer PRIVATE
    NavMeshCore
    RecastNavigation::Detour
    RecastNavigation::DebugUtils
    SDL2::SDL2
    OpenGL::GL
    OpenGL::GLU
)
if (WIN32)
    target_link_libraries(RecastNavigationViewer PRIVATE Comdlg32)
endif()

if (MSVC)
    # 源文件含中文注释,是 UTF-8 编码,强制让 MSVC 按 UTF-8 读取源码(否则默认按本地代码页解析会导致乱码甚至语法错误)
    target_compile_options(RecastNavigationServer PRIVATE /utf-8)
    target_compile_options(RecastNavigationViewer PRIVATE /utf-8)
endif()
