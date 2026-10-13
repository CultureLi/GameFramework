# RecastNavigationServer 数据交换格式

Unity 导出脚本和本工具之间约定的自定义小端二进制格式(不依赖 JSON/protobuf)。
文件扩展名建议使用 `.rnav`。

所有整数/浮点均为小端(Windows/Unity 默认即小端,一般无需特殊处理)。

```
Header:
  char[4]  magic              = "RNAV" (不含结尾 \0, 即 4 个字节 'R','N','A','V')
  uint32   version            = 1
  float    agentHeight        // 对应 NavMeshSurface 烘焙时的 Agent Height
  float    agentRadius        // 对应 Agent Radius
  float    agentMaxClimb      // 对应 Agent Max Climb
  float    cellSize           // 体素量化步长 cs(xz 平面),建议与烘焙时 Cell Size 一致
  float    cellHeight         // 体素量化步长 ch(y 轴),建议与烘焙时 Cell Height 一致

Mesh:
  int32    vertexCount
  float[vertexCount * 3]   vertices   // 世界坐标 x,y,z,顺序对应 NavMesh.CalculateTriangulation().vertices
  int32    triangleCount
  int32[triangleCount * 3] indices    // 对应 .indices,每 3 个一组构成一个三角形(逆时针)
  uint8[triangleCount]     areas      // 对应 .areas,每个三角形的 area type id

Links (NavMeshLink):
  int32    linkCount
  重复 linkCount 次,每次:
    float[3] startPos
    float[3] endPos
    float    radius
    uint8    bidirectional   // 0 = 单向(startPos -> endPos),1 = 双向
    uint8    area             // area type id,默认可填 0
```

## 字段说明 / 约定

- `bmin`/`bmax`(体素化包围盒)**不包含在文件里**,由本工具读取顶点数据后自动计算,避免 Unity 端和 C++ 端各自计算导致不一致。
- `areas`/Link 的 `area` 字段:本次 PoC 阶段统一当作可行走区域处理(内部 `polyFlags` 全部置 `1`),不做按 area 区分的流过滤。后续如果需要区分"可行走但减速"等语义,再扩展格式。
- 三角形顶点顺序沿用 Unity `NavMesh.CalculateTriangulation()` 的原始顺序,工具会用共享顶点索引(而不是坐标去重)精确计算三角形邻接关系,所以 `indices` 必须是基于 `vertices` 数组的共享索引(Unity 的 API 本身就是这样输出的)。

## 示例写入顺序(供 Unity 导出脚本参考)

```
写 "RNAV" (4 bytes)
写 version (uint32)
写 agentHeight, agentRadius, agentMaxClimb, cellSize, cellHeight (5 个 float)
写 vertexCount (int32)
写 vertices (vertexCount*3 个 float)
写 triangleCount (int32)
写 indices (triangleCount*3 个 int32)
写 areas (triangleCount 个 byte)
写 linkCount (int32)
对每个 link 依次写: startPos(3 float), endPos(3 float), radius(float), bidirectional(byte), area(byte)
```
