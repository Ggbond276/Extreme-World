# Extreme World

Unity MMO 全栈架构练习项目,涵盖客户端、服务端、共享协议层。

## 项目内容

- **客户端**:Unity 2021+,C#
- **服务端**:.NET Framework 4.6.1 Console 应用
- **协议**:protobuf3,所有跨端消息定义在 `Src/Lib/proto/message.proto`
- **数据库**:SQL Server + Entity Framework 6
- **网络**:TCP 长连接,基于 `RayMix.NetLibs` 异步监听

## 已实现模块

| 模块 | 说明 |
|---|---|
| 登录 / 注册 / 角色创建 | `UserService`,进入游戏下发完整 `NCharacterInfo` |
| 任务系统 | 接取 / 交付 / 状态机 / 前置任务 / NPC 头顶图标 |
| 道具 / 背包 / 装备 | 增减数量、堆叠拆分、7 槽位装备切换 |
| 商店 | 购买、金币扣减 |
| 好友 / 公会 / 队伍 / 聊天 | 完整业务流 |
| 地图 / 实体同步 | 传送、怪物生成、AOI 同步(基础) |
| 状态广播 | 金币 / 经验 / 道具的运行时增量同步 |

战斗系统、装备属性加成、AOI 优化等尚在开发中。

## 项目结构

```
Src/
├── Client/                          Unity 客户端代码
├── Server/GameServer/GameServer/    服务端代码
├── Lib/                             共享层:protobuf 协议 + JSON 静态配置
└── Data/                            服务端运行时数据(JSON 配置)
Doc/
├── ServerArchitecture/              服务端架构总览(Mermaid)
├── RefactorReview/                  重构评审记录
└── InterviewArchitectureReview/     系统架构分析文档(任务/道具系统)
```

## 运行

### 环境

- Unity 2021+
- Visual Studio 2019+ / .NET Framework 4.6.1
- SQL Server 2017+

### 步骤

1. 创建数据库 `ExtremeWorld`,在 `Src/Server/GameServer/GameServer/Properties/Settings.Designer.cs` 中配置连接字符串
2. 启动服务端:`cd Src/Server/GameServer/GameServer && dotnet run`(或 VS F5)
3. 启动客户端:Unity Hub 打开 `Src/Client/`,启动 `Assets/Scenes/Login.unity`

## 架构文档

详细的系统设计分析见 [`Doc/InterviewArchitectureReview/`](Doc/InterviewArchitectureReview/README.md),包含任务系统、道具系统的完整需求 → 代码推导。

## License

仅用于个人学习与面试准备。
