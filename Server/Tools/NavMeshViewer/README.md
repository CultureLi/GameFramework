# NavMesh Viewer

服务端导航网格可视化工具，用于检查 `.rnav` 的连通性、关卡位置和地图建筑摆放。

## 启动

在 `Server` 目录执行：

```bat
view_navmesh.bat
```

或者指定文件：

```bat
dotnet run --project Tools\NavMeshViewer\NavMeshViewer.csproj -- ^
  NavData\navmesh.rnav ^
  MapData\OpenWorld\BuildingData.bytes
```

不传参数时会自动向上查找：

- `NavData/navmesh.rnav`
- `Server/MapData/OpenWorld/BuildingData.bytes`

## 操作

| 操作 | 功能 |
|---|---|
| 左键 | 设置路径起点 |
| 右键 | 设置路径终点 |
| 中键拖动 | 平移视图 |
| 滚轮 | 以鼠标位置为中心缩放 |
| 适配视图 | 缩放到整张导航网格 |
| 清除路径 | 清除起点、终点和路径 |

## 颜色

- 不同颜色：导航网格的不同连通块
- 蓝色/彩色区域：NavMesh 三角形
- 红色矩形：地图建筑关卡
- 灰色矩形：其他建筑
- 绿点/红点：路径起点/终点
- 黄线：路径查询结果

左下角的状态栏会显示：

```text
NavMesh: 顶点数、三角形数、连通块数、.rnav 关卡数
建筑数、识别出的关卡数
Path OK / Path FAIL
```

注意：路径查询只使用 `.rnav` 导航网格，不套用服务端的建筑关卡权限；关卡权限需要直接在服务端验证。
