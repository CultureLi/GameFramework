#include "NavDataDiscovery.h"

#include <algorithm>
#include <cctype>
#include <filesystem>
#include <vector>

#include "SDL.h"

namespace fs = std::filesystem;

std::string FindNavDataDir()
{
    std::error_code ec;
    if (fs::exists("NavData", ec) && fs::is_directory("NavData", ec))
        return fs::absolute("NavData", ec).string();

    char* base = SDL_GetBasePath();
    fs::path dir = base ? fs::path(base) : fs::current_path();
    if (base) SDL_free(base);

    for (int i = 0; i < 5; ++i)
    {
        fs::path candidate = dir / "NavData";
        if (fs::exists(candidate, ec) && fs::is_directory(candidate, ec))
            return fs::absolute(candidate, ec).string();
        fs::path parent = dir.parent_path();
        if (parent.empty() || parent == dir) break;
        dir = parent;
    }
    return std::string();
}

std::string PickFirstRnavFile(const std::string& navDataDir)
{
    if (navDataDir.empty()) return std::string();
    std::error_code ec;
    std::vector<std::string> names;
    for (const auto& entry : fs::directory_iterator(navDataDir, ec))
    {
        if (!entry.is_regular_file(ec)) continue;
        std::string ext = entry.path().extension().string();
        std::transform(ext.begin(), ext.end(), ext.begin(),
                        [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
        if (ext == ".rnav") names.push_back(entry.path().string());
    }
    std::sort(names.begin(), names.end());
    return names.empty() ? std::string() : names.front();
}
