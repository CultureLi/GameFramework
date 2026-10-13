#pragma once

#include <string>

struct SDL_Window;

// Windows 原生"打开文件"对话框,只接受 .rnav。本工具是 Windows-only PoC,不做跨平台 #ifdef。
// initialDir: 对话框默认定位的文件夹(建议传 NavData/ 的绝对路径;为空则用进程当前目录)。
// 返回选中文件的绝对路径;用户取消返回空字符串。阻塞调用,PoC 场景可接受。
std::string OpenRnavFileDialog(SDL_Window* window, const std::string& initialDir);
