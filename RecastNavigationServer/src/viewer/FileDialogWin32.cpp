#define NOMINMAX
#include <windows.h>
#include <commdlg.h>

#include "FileDialogWin32.h"

#include "SDL.h"
#include "SDL_syswm.h"

std::string OpenRnavFileDialog(SDL_Window* window, const std::string& initialDir)
{
    HWND owner = nullptr;
    SDL_SysWMinfo wmInfo;
    SDL_VERSION(&wmInfo.version); // 必须先设置版本号,否则 SDL_GetWindowWMInfo 会失败
    if (window && SDL_GetWindowWMInfo(window, &wmInfo))
        owner = wmInfo.info.win.window;

    char fileBuf[MAX_PATH] = {};

    OPENFILENAMEA ofn = {};
    ofn.lStructSize = sizeof(ofn);
    ofn.hwndOwner = owner;
    ofn.lpstrFilter = "RecastNavigationServer navmesh (*.rnav)\0*.rnav\0All files (*.*)\0*.*\0";
    ofn.lpstrFile = fileBuf;
    ofn.nMaxFile = sizeof(fileBuf);
    ofn.lpstrInitialDir = initialDir.empty() ? nullptr : initialDir.c_str();
    ofn.lpstrTitle = "Load .rnav file";
    ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

    if (GetOpenFileNameA(&ofn))
        return std::string(fileBuf);
    return std::string();
}
