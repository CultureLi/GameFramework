内城需求整理：

# v1:

1. 现在有内城Midgard, 世界地图 OpenWorld，登录后，默认模式为内城，主界面上有按钮，点击后在内城、世界地图之间切换，点击响应函数在UIMain.SwitchScene中，这里不是真正的切换场景，而是在Main场景中，创建Midgard和OpenWorld的节点，来进行显示和隐藏，Midgard中的地块加载、mapData加载方式和OpenWorld一样
2. 内城需要多导出一点数据，哪些地方可以手动放置建筑，
   在场景上，需要添加一种放置类型：可建造区域, 不需要关联任何配置表，当放之后，画出一个绿色的矩形区域，导出的数据需要矩形的基础数据，在改区域移动、放置建筑，逻辑跟之前内城放置一样，你先做一版我看看

---

# v1 实现说明

## 1. Main 场景中的内城 / 世界地图切换

### 1.1 场景结构

不再切换真正的场景，Main 场景里直接挂两个区域根节点，同一时刻只激活一个：

```
Main
├─ Main Camera                 （WorldCameraController）
├─ Directional Light
├─ EventSystem
├─ Midgard                     （MapArea，默认激活）
│  └─ Midgard_ChunkStreaming   （WorldChunkStreaming，sceneName=Midgard，10×10 块，50m/块）
└─ OpenWorld                   （MapArea，默认隐藏）
   └─ OpenWorld_ChunkStreaming （WorldChunkStreaming，sceneName=OpenWorld，10×10 块，100m/块）
```

- 区域节点上的 `MapArea` 记录区域名、地块流式组件、相机切换后的注视点与地图边界。
- 登录进入 Main 后，`MapDataMgr` 默认切到 `Midgard`（内城）。
- 主界面按钮 `CompBtnRed_Switch`（文字「切换世界」）调用 `UIMain.SwitchScene` →
  `MapDataMgr.SwitchNextArea()`，在 Midgard / OpenWorld 之间来回切换。

### 1.2 切换时发生的事（MapDataMgr）

1. 隐藏当前区域节点并销毁它下面的 `MapDataStreaming` / `BuildableAreaMgr`，释放地图数据句柄；
2. 激活目标区域节点，`WorldChunkStreaming` 重新按相机覆盖范围流式加载地块；
3. 从 `Assets/AssetBundles/MapData/<区域名>/` 加载五个模块：
   `BuildingData`、`DecorateData`、`ResourceData`、`ObstacleData`、`BuildableAreaData`（缺失模块跳过）；
4. 数据齐了以后在区域节点下创建 `MapDataStreaming`，按相机投射框创建/销毁地图实体；
5. 单机模式通知 `PathfindingMgr` 换一份对应区域的 `navmesh.bytes`，玩家传送到新地图出生点；
6. 相机边界和注视点换成该区域配置（Midgard 0~500，OpenWorld 0~1000）。

> 地块流式加载节点被隐藏时会回收已加载地块（`WorldChunkStreaming.OnDisable`），切回来会重新加载；
> 每个区域有独立的对象池（MidgardChunkPool / OpenWorldChunkPool）。

## 2. 可建造区域（BuildableArea）

### 2.1 编辑器

- `Tools/Map Editor` 的「类型」下拉框新增 **可建造区域**（`MapEntityCategory.BuildableArea`）。
- 不关联任何配置表：列表里只有一个进入绘制模式的条目。
- **左键按下 → 拖动 → 松开** 画出矩形；Grid 吸附打开时角点按格子吸附；
  单击（不拖）会以点击点为中心放一块默认 10×10 的区域。
- 绘制预览和已放置区域都显示为绿色半透明矩形 + 绿色边框（`BuildableAreaMarker.OnDrawGizmos`）。
- 放置后对象挂在 `DataEditor/BuildableArea` 下，自动挂：
  - `BuildableAreaMarker`：矩形本地尺寸 `size (X/Z)`；
  - `MapEntityInfo`：`Category=BuildableArea`、`ConfigId=0`、稳定的 `InstanceId`。
- 可建造区域没有 Collider/Renderer，不参与 NavMesh 烘焙。

### 2.2 导出

`MapEditorWindow` 的导出模块新增 **BuildableArea**（`All` 会一并勾选）。导出产物：

| 模块 | 客户端 | 服务端 |
|---|---|---|
| BuildableArea | `Assets/AssetBundles/MapData/<地图名>/BuildableAreaData.asset` | `Server/MapData/<地图名>/BuildableAreaData.bytes` |

导出的 `MapItemData`：

```csharp
Id          = 0            // 不关联配置表
InstanceId  = 稳定 UID
Pos         = 矩形中心世界 XZ
Size        = 矩形本地尺寸 X/Z
RotationY   = 绕 Y 旋转
```

二进制格式复用 `MapDataBinary v2`，服务端不需要改动即可按同一结构读取。

### 2.3 运行时

- `MapDataMgr` 把 `BuildableAreaData` 交给 `BuildableAreaMgr`（挂在区域节点下）。
- 等首批地块加载完成、射线能打到地面后，按每个条目生成 `BuildableArea`：
  - 用 `Sprites/Default` 运行时材质画出绿色半透明矩形 + 边框（不挂 Collider，不影响寻路）；
  - 以 1m 为格子（`BuildableAreaMgr.cellSize`）把矩形切成 `CellsX × CellsZ` 的建造网格，
    维护建筑占位表。
- 建造规则与 Midgard 场景里 `CityGridView / CityGrid / BuildingMoveCtrl` 的内城放置一致：
  `IsPointAvailable` / `IsCellAvailable` / 提交时 `SetCellBuildingId`。

### 2.4 建造流程（主界面「建造」下拉框）

1. `UIMain` 在右上角运行时实例化 `Assets/AssetBundles/UI/Common/Dropdown/Dropdown.prefab`：
   - 未选择建筑时显示占位文字「建造」；
   - 下拉列表只包含 `MidgardBuildingCfg.Name` 的建筑名字。
2. 选中某个建筑后调用当前区域的 `CityBuildCtrl.SelectBuilding(cfg)`：
   - 按 `MidgardBuildingCfg.AssetPath` 异步加载建筑预制体；
   - 生成预览对象（关闭全部 Collider 和 Behaviour，不执行预制体脚本）。
3. 鼠标悬浮时预览对象按可建造区域格子吸附并贴地：
   - 在区域内且不与其他建筑重叠 → 半透明绿色；
   - 超出可建造区域 / 与已有建筑重叠 → 半透明红色；
   - 绿色时鼠标左键按下放置建筑；红色时左键无反应。
   - 右键或 Esc 退出建造模式，下拉框恢复为「建造」。
4. **移动已有建筑**：不在建造模式时，鼠标左键在已放置建筑上长按 2 秒进入移动状态；
   - 移动时建筑按格子吸附，同样绿色可放置、红色越界/重叠；
   - 再次左键放置在绿色位置，右键 / Esc 取消并回到原位置；
   - 进入移动状态时会先释放原格子占位，提交或取消后恢复。
5. **建造 / 移动状态下显示小格子**：
   - `BuildableArea` 为每个格子生成一张 quad（顶点色区分空闲/占用），用 `Custom/BuildingGrid`
     Shader 画出格线和半透明填充；
   - 空闲格绿色、被建筑占用的格子红色；
   - 只在建造或移动状态显示，退出后隐藏。
   - Shader：`Assets/Arts/Shader/Scene/BuildingGrid.shader`；
   - 材质：`Assets/Arts/Map/MidGard/Material/BuildingGrid.mat`。
6. 预览材质与 Shader：
   - `Assets/Arts/Shader/Scene/BuildingPreview.shader`（`Custom/BuildingPreview`，URP 透明无光照）；
   - `Assets/Arts/Map/MidGard/Material/BuildingPreview_Valid.mat`（绿色）；
   - `Assets/Arts/Map/MidGard/Material/BuildingPreview_Invalid.mat`（红色）。
7. 建造或移动模式下 `CityBuildCtrl.IsEditing` 为 true，`LocalPlayerController` 不再响应左键寻路，
   避免与放置建筑抢输入；短按已有建筑也不会触发寻路。
8. 当前放置的建筑只在运行期存在，不写回地图数据；Midgard 示例场景里有一块 50×50
   的可建造区域并已导出对应数据，可直接在游戏里查看效果。

## 3. 涉及文件

| 端 | 文件 |
|---|---|
| 运行时 | `Map/MapData/MapArea.cs`、`MapDataMgr.cs`、`BuildableAreaData.cs`、`MapEntityCategory.cs` |
| 运行时 | `Map/CityBuilding/BuildableAreaMarker.cs`、`BuildableArea.cs`、`BuildableAreaMgr.cs`、`CityBuilding.cs`、`CityBuildCtrl.cs` |
| 运行时 | `Stage/OpenWorld/WorldChunkStreaming.cs`（区域隐藏时回收地块）、`Pathfinding/PathfindingMgr.cs`、`Pathfinding/LocalPlayerController.cs` |
| 编辑器 | `Editor/MapEditor/MapEditorWindow.cs`、`MapDataExporter.cs`、`MapEditorUtils.cs` |
| UI | `UI/Main/UIMain.cs`（「建造」下拉框）、`AssetBundles/UI/Main/UIMain.prefab`（「切换世界」按钮） |
| 美术 | `Arts/Shader/Scene/BuildingPreview.shader`、`Arts/Map/MidGard/Material/BuildingPreview_Valid.mat`、`BuildingPreview_Invalid.mat` |
| 配置 | `DataTables/Datas/#MidgardBuildingCfg.xlsx`（assetPath 列）、生成的 `MidgardBuildingCfg.cs` / `TbMidgardBuildingCfg.cs` |
| 场景 | `AssetBundles/Scene/Main.unity`（Midgard / OpenWorld 区域节点） |

