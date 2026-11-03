# Game Server (C# / .NET 9)

Unity 客户端登录 / 心跳服务器的 C# 实现。协议、加密、压缩、消息 ID 全部复用
[客户端既有网络结构](../Client/Assets/HotFixScripts/Framework/Network)，protobuf 仍然只有
[`Protos/protos`](../Protos/protos) 一份，服务器直接编译客户端已经生成好的 C# 代码。

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
│  ├─ Game/
│  │  ├─ AccountStore.cs       内存账号表 + 登录校验
│  │  └─ LoginCodes.cs         返回码
│  └─ Utility/Log.cs
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
```

## 客户端接入

客户端登录逻辑在：

- `Client/Assets/HotFixScripts/GameEntry/UI/Login/UIGameEntryLogin.cs`
- `Client/Assets/HotFixScripts/GameEntry/Network/LoginSession.cs`

流程：登录按钮 -> `FW.NetMgr.Create/ConnectAsync` -> 握手完成后发送 `LoginReq` ->
收到 `LoginAck.code == 0` 进入 GameMain -> 每 5 秒发送心跳。
登录服地址来自 `Client/Assets/Resources/appConfig.json` 的 `loginServer[0]`。

## 注意

- 账号表在内存里，服务器重启后清空；生产环境应使用数据库，并对密码做哈希。
- 当前 AES 固定 IV 是客户端既有协议，安全性有限；正式项目建议改为每条消息随机 IV / TLS。
- `Server_Go_Backup/` 是之前 Go 版本服务器的备份（清理 Server 目录时保留），确认不再需要后可以删除。
