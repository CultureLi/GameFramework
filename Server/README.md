# Game Server (C# / .NET 9)

Unity 客户端登录 / 心跳 / 寻路服务器的 C# 实现。协议、加密、压缩、消息 ID 全部复用
[客户端既有网络结构](../Client/Assets/HotFixScripts/Framework/Network)，protobuf 仍然只有
[`Protos/protos`](../Protos/protos) 一份，服务器直接编译客户端已经生成好的 C# 代码。

寻路使用 [DotRecast](https://github.com/ikpil/DotRecast)（Recast Navigation 的 C# 移植）。
Unity 端继续用 `NavMeshExporter` 把烘焙好的 NavMesh 导出成 `.rnav`，服务端把三角形网格
组装成 Detour 导航网格后执行 `FindPath / FindStraightPath`。

## 为什么用 System.Net.Sockets 而不是 TouchSocket / SuperSocket

现有客户端的协议是定制的：

1. 建连后先做 RSA 公钥下发 + AES 密钥交换（裸长度前缀包，不是业务帧）
2. 之后才是 9 字节自定义头（length + msgId + flag）
3. 消息体还有 AES 固定 IV + LZ4 + protobuf 的固定顺序

TouchSocket、SuperSocket 这类框架的包适配器主要面向"固定长度头 / 分隔符 / 长度字段"
的标准协议，接入这套握手和加密仍然要写自定义 pipeline。这里直接用
`System.Net.Sockets` + `async/await`，依赖少、行为完全可控，和客户端 Unity 侧的
`TcpClient/NetworkStream` 实现也保持一致。如果后续要换成 TouchSocket，只需要替换
`Network/` 目录，协议层和业务层不用动。

## 目录

```
Server/
├─ GameServer/                 服务器项目
│  ├─ Program.cs               入口
│  ├─ Configuration/           命令行配置
│  ├─ Network/
│  │  ├─ TcpGameServer.cs      监听、会话管理、消息分发、心跳检测
│  │  └─ ClientSession.cs      单连接：握手、收包、发包、断线清理
│  ├─ Protocol/
│  │  ├─ PacketCodec.cs        9 字节消息头 + LZ4
│  │  ├─ AesCrypto.cs          AES-CBC + PKCS7
│  │  └─ MessageRegistry.cs    BKDRHash 消息 ID
│  ├─ Pathfinding/
│  │  ├─ NavMeshFileReader.cs  读取 Unity 导出的 .rnav
│  │  ├─ NavMeshBuilder.cs     三角形网格 -> Detour 导航网格
│  │  └─ NavMeshWorld.cs       DotRecast 查询封装（FindPath/FindStraightPath）
│  ├─ Game/
│  │  ├─ AccountStore.cs       内存账号表 + 登录校验
│  │  ├─ LoginCodes.cs         登录返回码
│  │  ├─ PlayerEntity.cs       服务端玩家位置、路径与移动
│  │  └─ PathFindCodes.cs      寻路返回码
│  └─ Utility/Log.cs
├─ NavData/navmesh.rnav        Unity 导出的导航数据
├─ Tools/TestClient/           命令行测试客户端（不依赖 Unity）
├─ build.bat                   编译（dotnet publish）
├─ run.bat                     启动服务器
└─ publish/                    编译产物（gitignore）
```

## 编译 / 启动

```
build.bat
run.bat
```

`run.bat` 默认监听 `0.0.0.0:19000`，可以透传参数：

```
run.bat --addr 127.0.0.1:19000 --heartbeat-timeout 30
```

参数：

| 参数 | 默认值 | 说明 |
|---|---|---|
| `--addr` | `0.0.0.0:19000` | 监听地址 |
| `--handshake-timeout` | `10` 秒 | RSA/AES 握手超时 |
| `--heartbeat-timeout` | `30` 秒 | 超过该时间没有收到任何消息则断开 |
| `--navmesh` | `NavData/navmesh.rnav` | Unity 导出的导航数据路径 |

### 端口被占用

如果启动时提示 `端口 xxxx 已被占用`，先用下面两条命令确认占用进程：

```
netstat -ano | findstr :19000
tasklist /FI "PID eq <上一步的PID>"
```

关闭占用进程，或者换一个端口启动：

```
run.bat --addr 127.0.0.1:19001
```

默认端口从 `9000` 改成了 `19000`，因为部分机器上 `9000` 会被聊天/办公软件占用。

## 网络协议（与客户端完全一致）

消息头 9 字节，整数均为大端：

```
[length 4字节]  消息体长度（加密/压缩后）
[msgId  4字节]  protobuf 消息 ID
[flag   1字节]  1=加密，2=压缩，3=加密+压缩
```

客户端上行处理顺序：`protobuf 序列化 -> AES-256-CBC/PKCS7 加密 -> LZ4 压缩`。
服务器回包只加密不压缩（flag=1），客户端 `Receiver` 同样支持。

握手：

1. 服务器发送 `4字节长度 + RSA 公钥 XML`（明文）
2. 客户端生成 AES Key(32) + IV(16)，用 RSA 公钥 PKCS1v15 加密后发送
3. 服务器 RSA 私钥解密，本连接后续消息都用这把 AES 密钥

消息 ID 使用与客户端 `ProtoTypeHelper.BKDRHash` 相同的算法（seed=131，uint32 溢出）。
新增 proto 消息后，客户端重新生成 C# 代码，服务器重新编译即可，不需要维护 ID 表。

## 已实现的基础功能

- **连接**：`TcpListener` 接受连接，每条连接独立 `ClientSession`，`NoDelay=true`
- **消息收发**：异步读写、发送锁串行化、9 字节帧、LZ4 解压、AES 解密、protobuf 解析
- **心跳**：客户端每 5 秒 `HeartBeatReq`，服务端回 `HeartBeatAck`；服务端每 5 秒扫描一次
  `LastReceivedUtc`，超过 `--heartbeat-timeout` 主动关闭连接
- **断线处理**：EOF、SocketException、IOException、ObjectDisposedException 都会进入统一
  清理；`Close()` 幂等；清理在线账号和会话表
- **登录**：账号格式校验、首次登录自动注册、密码错误、重复在线、连续 5 次失败断开
- **优雅退出**：Ctrl+C 取消监听、关闭所有会话并等待最多 2 秒

## 寻路与移动同步

### 导航数据

1. 在 Unity 里打开 `Assets/NoBundleRes/Scene/OpenWorld/OpenWorld.unity`（地形生成器的目标场景）
2. `Tools/OpenWorld/Terrain Generator` → **Generate All**（生成地形、关卡门并烘焙 NavMeshSurface）
3. 菜单 `Tools/RecastNavigationServer/Export NavMesh` 导出 `.rnav`，覆盖 `Server/NavData/navmesh.rnav`
4. 重新编译/启动服务器；`dotnet publish` 会把它复制到 `publish/server/NavData/`

导出的 `.rnav` 为 **v2**，除了三角形/NavMeshLink，还包含动态关卡门（`LevelGate`）：
`gateId / center / size / rotationY / open`。服务端加载后会把每个门的 Box 覆盖到的导航多边形
记录下来，关门时把这些多边形 flag 置 0，`FindPath` 会自动绕开或返回不可达。

服务端启动时会打印 poly 数、边界边、内部边、关卡门数量和出生点。如果换了客户端场景，
必须重新导出导航数据，保证服务端寻路和 Main 场景流式加载出来的地形一致。

### 服务端实现

- `NavMeshFileReader` 解析 `.rnav`（兼容 v1 无门、v2 含关卡门）
- `NavMeshBuilder` 移植自 `RecastNavigationServer/src/NavMeshBuilder.cpp`：
  顶点按体素精度量化焊接 -> 退化三角形过滤 -> 共享边邻接 -> 组装 `DtNavMeshCreateParams`
- `NavMeshWorld` 使用 DotRecast 的 `DtNavMeshQuery` 执行 `FindNearestPoly`、`FindPath`、
  `FindStraightPath`；查询用锁串行化（`DtNavMeshQuery` 不是线程安全的）
- `NavMeshWorld` 加载 v2 关卡门：用门的 AABB 做 `QueryPolygons` 映射多边形，开关门时
  `SetPolyFlags` 修改通行性
- 登录成功时创建 `PlayerEntity`，出生点从世界坐标 `(0,0,0)` 用 `FindNearestPoly` 找最近的
  可行走点（搜索半径由 10m 逐步扩大到 5000m，并排除已关闭的关卡门）；找不到才回退到导航网格
  第一个有效三角形质心。出生点通过 `LoginAck.spawnPos` 下发给客户端
- 游戏循环 50ms 一 tick，玩家按 4 单位/秒沿 `corners` 移动；每 100ms 广播一次 `MoveSync`
- 关卡门协议：客户端查询/请求，服务端校验后更新导航 flag 并广播给所有在线玩家

### 协议

```
PathFindReq  { seq, target, clientPos }
PathFindAck  { seq, code, message, pathId, corners[], start, speed }
MoveSync     { playerId, pathId, position, rotationY, moving, cornerIndex }
GateStateQueryReq { }
GateStateReq  { gateId, open }
GateStateSync { gateId, open, code, center, size, rotationY }
SetMoveSpeedReq  { speed }
SetMoveSpeedSync { playerId, speed, code }
```

流程：

1. 客户端点击地面 -> `PathFindReq`
2. 服务端以自己维护的玩家位置为起点，`FindNearestPoly` 吸附起点/终点，`FindPath` +
   `FindStraightPath`
3. 返回 `PathFindAck`（路径拐点），同时把路径挂到 `PlayerEntity`
4. 服务端 tick 移动角色，每 100ms 广播 `MoveSync`
5. 客户端按 `corners` 本地预测移动，并用 `MoveSync` 校正位置；到达后服务端下发
   `moving=false`，客户端停止

返回码：`0` 成功、`1` 未登录、`2` 目标无效、`3` 不可达、`4` 服务器错误。

## 测试客户端

`Tools/TestClient` 是一个不依赖 Unity 的命令行客户端，完整走一遍 RSA/AES/LZ4/protobuf：

```
publish\testclient\TestClient.exe --addr 127.0.0.1:19000 --account test --password 123456
```

常用场景：

```
:: 登录 + 2 次心跳
TestClient.exe --account test --password 123456 --heartbeats 2

:: 密码错误（先成功登录一次注册账号，再传错密码）
TestClient.exe --account test --password wrong

:: 登录后停止心跳，验证服务端心跳超时断开
:: 服务端需要用较短的心跳超时启动：run.bat --heartbeat-timeout 3
TestClient.exe --account idle --password 123456 --idle 10

:: 登录 + 寻路 + 接收位置同步（目标点是默认测试导航网格的另一端）
TestClient.exe --account path --password 123456 --path --heartbeats 0

:: 查询并切换一次关卡门（导航数据需要是 v2 且包含 LevelGate）
TestClient.exe --account gate --password 123456 --gates --heartbeats 0

:: 设置移动速度 8 再恢复 4
TestClient.exe --account speed --password 123456 --speed --heartbeats 0
```

## 客户端接入

客户端登录逻辑在：

- `Client/Assets/HotFixScripts/GameEntry/UI/Login/UIGameEntryLogin.cs`
- `Client/Assets/HotFixScripts/GameEntry/Network/LoginSession.cs`

流程：登录按钮 -> `FW.NetMgr.Create/ConnectAsync` -> 握手完成后发送 `LoginReq` ->
收到 `LoginAck.code == 0` 进入 GameMain -> 每 5 秒发送心跳。
登录服地址来自 `Client/Assets/Resources/appConfig.json` 的 `loginServer[0]`。

寻路正式脚本（Main 场景）：

- `Client/Assets/HotFixScripts/GameMain/Pathfinding/PathfindingMgr.cs`：网络消息、玩家、关卡门同步
- `.../LocalPlayerController.cs`：点击寻路、本地预测、MoveSync 校正、相机跟随
- `.../LevelGate.cs`：关卡门开关、`gateID`、`GateStateRequested` 事件
- `.../MazeGateDebugToggle.cs`：按 `G` 请求开关所有门

流程：

- `GameMainEntryMgr` 通过 Addressables 加载 `Main` 场景
- `MainEntryStartStage` 创建 `PathfindingMgr`，在服务端出生点创建本地玩家
- `PathfindingMgr` 注册 `PathFindAck / MoveSync / GateStateSync`，并向服务端发送一次
  `GateStateQueryReq`
- 服务端把导出的关卡门（gateId/center/size/rotationY/open）同步回来，客户端动态创建
  `LevelGate` 方块；Main 场景里如果已经有同 gateID 的 LevelGate，则直接复用
- 手动调用 `LevelGate.RequestSetOpen` / 按 `G` 时，`PathfindingMgr` 发送 `GateStateReq`，
  服务端更新动态障碍并广播 `GateStateSync`，客户端再应用权威状态
- 设置窗口 `UISetting`：
  - 速度输入框：`1~20` 的数值，调用 `PathfindingMgr.RequestMoveSpeed`，服务端广播
    `SetMoveSpeedSync` 后更新本地移动速度
  - 关卡输入框：只填 `gateId` 表示按当前状态切换；`gateId,open` / `gateId,close`
    表示显式打开/关闭，调用 `PathfindingMgr.RequestGateState` / `RequestGateToggle`
- 相机复用 Main 场景的 `WorldCameraController`，每帧 `MoveTo` 到玩家脚下，
  `WorldChunkStreaming` 会按相机覆盖范围加载 OpenWorld 地形块

## 配置表

服务端和客户端**共用同一份配置表代码和数据**：

- 服务端项目直接链接 `Client/Assets/HotFixScripts/GameMain/Config/Gen/*.cs`
  （Luban 导出的表格类）以及 `Framework/Config` 下无 Unity 依赖的运行时类：
  `BeanBase / ByteBuf / StringUtil / ITypeId / TableBase`
- `TableBase` 已从 `ConfigMgr.cs` 抽成独立的
  `Framework/Config/TableBase.cs`，客户端和服务端共用，不需要复制代码
- 服务端 `Configuration/ServerConfigMgr.cs` 直接读取
  `Client/Assets/StreamingAssets/Config/configData.zip`（以及可选的 `i18n.zip`），
  按和客户端相同的 `TableBase.Initialize / LoadAll` 方式加载
- `run.bat` 默认传：

  ```
  --config "%~dp0..\Client\Assets\StreamingAssets\Config"
  ```

  也可以手动指定：

  ```
  run.bat --config D:\GameServerConfig
  ```

服务端启动时会打印每张表的条数：

```
[config] TbBuildingSummaryCfg: 6 条
[config] TbItemSummaryCfg: 8 条
[config] TbLanguageCfg: 17 条
[config] TbMapEntityCfg: 7 条
[config] TbResourceSummaryCfg: 2 条
[config] 配置目录 ...，共加载 5 张表
```

业务代码通过 `TcpGameServer.ConfigManager.GetTable<TbItemSummaryCfg>()` 读取。
Luban 重新导出 Gen 代码或更新 `StreamingAssets/Config` 后，客户端和服务器自动使用同一份，
不需要再往 Server 复制。服务端发布到其他机器时，把 `--config` 指向服务器上的配置目录即可。

## 注意

- 账号表在内存里，服务器重启后清空；生产环境应使用数据库，并对密码做哈希。
- 当前 AES 固定 IV 是客户端既有协议，安全性有限；正式项目建议改为每条消息随机 IV / TLS。
- 服务端目前移动的是所有登录玩家，`MoveSync` 会广播给所有会话；客户端 Demo 只处理自己的
  位置，其他玩家显示可以后续扩展。
- `Server_Go_Backup/` 是之前 Go 版本服务器的备份（清理 Server 目录时保留），确认不再需要后可以删除。
