#pragma once

#include "NavMeshDataFile.h"

// 内置的最小合成测试数据,无头 CLI 和可视化 viewer 共用,
// 不依赖 Unity 导出就能验证构建流程/交互本身没问题。
NavMeshRawData MakeSelfTestData();
