#pragma once

#include <string>
#include <vector>

#include "DetourNavMeshQuery.h"
#include "NavMeshBuilder.h"
#include "NavMeshDataFile.h"

#include "Camera.h"

// 把"加载 .rnav(或内置自测数据) -> BuildNavMesh -> dtNavMeshQuery::init -> 算包围盒 ->
// 相机对焦"整条流水线封装起来,启动时和用户点 Load 按钮重新加载时共用同一份代码。
//
// 失败语义:Load() 失败时,除 lastLoadError 外的所有字段保持上一次成功加载后的状态不变
// (不崩溃、不黑屏、不残留悬空指针)。
struct MeshSession
{
    NavMeshRawData raw;
    NavMeshBuildResult buildResult;
    dtNavMeshQuery query; // 不可拷贝/移动,始终原地调用 query.init() 复用
    float bmin[3] = {}, bmax[3] = {};
    float markerRadius = 0.3f;

    bool hasStart = false;
    bool hasEnd = false;
    float startPos[3] = {};
    float endPos[3] = {};
    std::vector<float> pathCorners;

    std::string currentFilePath; // 空 = 当前显示内置自测数据
    std::string currentFileName; // 仅文件名,给 UI 显示
    std::string lastLoadError;   // 空 = 没有错误

    // path 为空字符串 => 加载内置自测数据(MakeSelfTestData)。
    // 成功:替换 raw/buildResult/bmin/bmax/currentFile*,重新 query.init(),
    //       camera.FitToBounds(),清空 hasStart/hasEnd/pathCorners/lastLoadError,返回 true。
    // 失败:lastLoadError 写入原因,其余字段原样保留,返回 false。
    bool Load(const std::string& path, Camera& camera);
};
