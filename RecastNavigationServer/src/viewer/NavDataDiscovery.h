#pragma once

#include <string>

// 定位 NavData 文件夹:优先用 CWD 下的 "NavData"(覆盖"在项目根目录用终端跑"的场景),
// 否则从可执行文件自身所在目录(SDL_GetBasePath)向上找最多 5 层(覆盖"双击 exe,
// exe 实际在 build/ 或 build/Release/ 下,NavData 在上一两级目录"的场景)。
// 找不到返回空字符串。
std::string FindNavDataDir();

// 在给定文件夹里按文件名升序找第一个 *.rnav 文件(扩展名大小写不敏感)。
// 文件夹不存在/没有匹配文件,返回空字符串。
std::string PickFirstRnavFile(const std::string& navDataDir);
