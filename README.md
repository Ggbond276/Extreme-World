<div align="center">

# Extreme World

**Unity MMO 全栈架构练习项目** —— 客户端 + 服务端 + 共享协议层完整实现

[![Unity](https://img.shields.io/badge/Unity-2022.3.46f1c1-000?logo=unity)](https://unity.com/releases/editor/whats-new/2022.3.46)
[![C#](https://img.shields.io/badge/C%23-Unity%20%2F%20.NET%204.6.1-blue?logo=csharp)](https://learn.microsoft.com/dotnet/csharp)
[![Protobuf](https://img.shields.io/badge/Protobuf-3.5.1-green?logo=protobuf)](https://github.com/protocolbuffers/protobuf)
[![EF6](https://img.shields.io/badge/Entity_Framework-6.2.0-purple)](https://learn.microsoft.com/ef/ef6/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0.14-4479A1?logo=mysql&logoColor=white)](https://dev.mysql.com/doc/connector-net/en/)
[![Newtonsoft](https://img.shields.io/badge/Newtonsoft.Json-11.0.2-8A2BE2)](https://www.newtonsoft.com/json)
[![log4net](https://img.shields.io/badge/log4net-2.0.8-orange)](https://logging.apache.org/log4net/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey?logo=windows)]()
[![License](https://img.shields.io/badge/License-学习与面试准备-red)]()

</div>

---

## 一、项目简介

**Extreme World** 是一个用 Unity 实现的**完整 MMO 多人在线角色扮演游戏架构练习项目**,
覆盖传统 MMORPG 中**客户端、服务端、共享协议**三端全部核心模块,
系统性地实现 12 个业务系统,并配套**架构评审文档**。

**项目定位**:
- 🚫 不是商业级成品游戏
- ✅ 是**对 MMO 架构每一个难点都用代码正面回答一遍**的可解释参考实现
- ✅ 适合 **Unity / .NET / 游戏服务端方向面试准备** 与 **架构学习**

---

## 二、技术栈

### 2.1 客户端

| 项 | 版本 |
|---|---|
| 引擎 | **Unity 2022.3.46f1c1**(LTS) |
| 语言 | C#(API Compatibility Level: .NET Standard 2.1) |
| 关键模块 | 2D Sprite / Tilemap / TextMeshPro 3.0.6 / UGUI |
| 编程模式 | Singleton + Action 事件 + Coroutine |
| 网络 | `System.Net.Sockets.TcpClient` 异步收发 |
| 序列化 | protobuf3 |
| IDE | Visual Studio / Rider |

### 2.2 服务端

| 项 | 版本 |
|---|---|
| 运行时 | **.NET Framework 4.6.1**(Console 应用) |
| 项目格式 | .csproj / .sln / `packages.config` |
| ORM | **Entity Framework 6.2.0** |
| 数据库 | **MySQL 8.0.14**(`MySql.Data` + `MySql.Data.EntityFramework`) |
| 网络库 | `RayMix.NetLibs`(基于 .NET Socket 异步) |
| 序列化 | `Google.Protobuf 3.5.1` |
| JSON | `Newtonsoft.Json 11.0.2` |
| 日志 | `log4net 2.0.8` |
| IDE | Visual Studio 2019+ |

### 2.3 共享层

| 项 | 版本 |
|---|---|
| IDL | `protobuf3`(`Src/Lib/proto/message.proto`) |
| 静态配置 | JSON(`Src/Data/*.txt`,启动时反序列化为字典) |
| 代码生成 | `.proto → .cs`(Google Protobuf Compiler) |

### 2.4 协议 / 消息规模

- `.proto` 文件数:**1**(`message.proto`)
- 顶层 `message`:**70+**
- `enum`:**20+**
- 覆盖模块:User / Map / Item / Shop / Quest / Friend / Team / Guild / Chat / Status

### 2.5 运行环境

- **OS**:Windows 10 / 11 x64
- **DB**:MySQL 8.0+(连接字符串见 `Src/Server/GameServer/GameServer/Properties/Settings.Designer.cs`)

---

## 三、系统模块清单

| # | 模块 | 服务端 | 客户端 | 关键能力 | 状态 |
|---|---|---|---|---|---|
| 1 | **登录 / 注册 / 角色创建** | `UserService` | `UILogin` / `UIRegister` | TCP 连接、注册、进入游戏全量同步 `NCharacterInfo` | ✅ |
| 2 | **角色 / 实体** | `Character` / `Entity` / `Monster` | `EntityController` / `MapController` | 实体生命周期、地图同步、移动 | ✅ |
| 3 | **地图 / 传送** | `MapService` / `MapManager` | `TeleporterObject` | 地图切换、传送点、NPC / 怪物生成 | ✅ |
| 4 | **任务系统** | `QuestService` / `QuestManager` | `UIQuestSystem` / `UIQuestDialog` | 接取 / 交付 / 状态机 / 前置任务 / NPC 头顶图标 | ✅ |
| 5 | **道具系统** | `ItemService` / `ItemManager` | `ItemManager` / `BagManager` | 增减数量、自动堆叠拆分 | ✅ |
| 6 | **装备系统** | `EquipManager` | `EquipManager` / `UICharEquip` | 7 槽位装备切换、`byte[28]` 持久化 | ✅ |
| 7 | **背包** | `TCharacterItem` 表 | `BagManager` / `UIBag` | 格子位置、自动拆分堆叠、UI 渲染 | ✅ |
| 8 | **商店** | `ShopManager` | `UIShop` / `UIShopItem` | 购买道具、金币扣减 | ✅ |
| 9 | **好友** | `FriendService` / `FriendManager` | `FriendManager` / `UIFriends` | 好友列表、上下线通知 | ✅ |
| 10 | **公会** | `GuildService` / `GuildManager` | `GuildManager` / `UIGuildMain` | 创建、加入、成员管理、申请审批、聊天 | ✅ |
| 11 | **队伍** | `TeamService` / `TeamManager` | `TeamManager` / `UITeamSystem` | 组队、状态同步 | ✅ |
| 12 | **聊天** | `ChatService` / `ChatManager` | `ChatManager` / `UIChat` | 全服 / 私聊 / 公会 多频道广播 | ✅ |
| 13 | **战斗 / 怪物** | `MonsterManager` / `Spawner` | `PlayerInputController` | 怪物生成、AOI 同步 | 🚧 部分 |
| 14 | **状态广播** | `StatusManager` | `StatusService` | 金币 / 经验 / 道具 增量同步 | ✅ |

---

## 四、项目结构

```
Extreme-World/
├── Src/
│   ├── Client/                          Unity 客户端
│   │   ├── Assets/Scripts/              97 个 C# 文件
│   │   │   ├── Entities/                Character / Entity / Monster
│   │   │   ├── GameObject/              EntityController / PlayerInputController /
│   │   │   │                            MapController / NPCController / SpawnPoint
│   │   │   ├── Managers/                15 个 Manager(Quest / Item / Equip / Bag /
│   │   │   │                            Shop / Friend / Guild / Team / Chat /
│   │   │   │                            Entity / NPC / Minimap / Data / ...)
│   │   │   ├── Models/                  运行时数据类(User / Character / Item /
│   │   │   │                            BagItem / Quest)
│   │   │   ├── Network/                 NetClient(TcpClient 封装)
│   │   │   ├── Services/                8 个 Service(Quest / Item / Status /
│   │   │   │                            Friend / Team / Guild / Map / User / Chat)
│   │   │   ├── UI/                      UIBag / UIEquip / UIQuest / UIFriend /
│   │   │   │                            UIGuild / UITeam / UIShop / UIChat /
│   │   │   │                            UILogin / UIRegister / ...
│   │   │   └── Utilities/               Singleton / MonoSingleton / Resloader /
│   │   │                                LoadingManager / GameUtil / EnumUtil
│   │   ├── Packages/manifest.json       Unity 包清单
│   │   └── ProjectSettings/             Unity 工程配置
│   │
│   ├── Server/GameServer/GameServer/    .NET 服务端
│   │   ├── Entities/                    Character / CharacterBase / Entity / Monster
│   │   ├── Managers/                    14 个 per-character / 全局 Manager
│   │   ├── Models/                      Item / Quest / Friend / Guild / GuildMember /
│   │   │                                GuildApply / Team / Map
│   │   ├── Network/                     TcpSocketListener / NetConnection /
│   │   │                                NetSession / NetService /
│   │   │                                MessageDistributer / IPostResponser
│   │   ├── Services/                    Item / Quest / Friend / Guild / Team /
│   │   │                                Chat / Map / User / DB
│   │   ├── *.cs                         TUser / TPlayer / TCharacter /
│   │   │                                TCharacterBag / TCharacterItem /
│   │   │                                TCharacterQuest / TCharacterFriend /
│   │   │                                TGuild / TGuildMember / TGuildApply
│   │   │                                (EF6 + edmx 自动生成的 MySQL 实体)
│   │   ├── Entities.edmx / .tt          EF6 数据库映射与 T4 模板
│   │   ├── Properties/Settings.settings MySQL 连接字符串
│   │   └── GameServer.csproj            .NET Framework 4.6.1
│   │
│   ├── Lib/                             跨端共享库
│   │   ├── Common/Data/                 ItemDefine / EquipDefine / QuestDefine /
│   │   │                                ShopDefine / ...(JSON 反序列化的策划配置)
│   │   ├── proto/message.proto          ⭐ 所有跨端网络协议的唯一来源
│   │   └── Protocol/                    protobuf 自动生成的 C# 消息类
│   │                                    (Google.Protobuf 3.5.1)
│   │
│   └── Data/                            服务端运行时数据(JSON 配置)
│       ├── ItemDefine.txt               道具静态配置
│       ├── EquipDefine.txt              装备静态配置
│       ├── QuestDefine.txt              任务静态配置
│       └── ...
│
└── Doc/
    ├── ServerArchitecture/              服务端架构总览(Mermaid 思维导图)
    ├── RefactorReview/                  已完成的重构评审记录
    └── InterviewArchitectureReview/     ⭐ 系统架构分析文档(面试准备)
        ├── README.md
        ├── QuestSystem/                 任务系统分析(5 份)
        └── ItemSystem/                  道具系统分析(2 份)
```

---

## 五、数据库设计

### 5.1 数据模型

```
静态策划配置(JSON,全服共享)        玩家动态数据(MySQL,per-character)
─────────────────────────         ──────────────────────────────
ItemDefine              ─→       TCharacterItem
EquipDefine             ─→       TCharacter.Equips (byte[28])
QuestDefine             ─→       TCharacterQuest
ShopDefine              ─→         (运行时计算)
FriendDefine / TeamDefine         TCharacterFriend

运行时包装对象(Models/, per-session)
─────────────────────────
Models.Item / Models.Quest / Models.Friend / Models.Guild / Models.Team
把"静态配置 + 玩家动态数据"合并,并加业务方法
```

### 5.2 核心数据表

| 表 | 引擎 | 说明 |
|---|---|---|
| `TUser` | MySQL | 用户表(id / username / password) |
| `TPlayer` | MySQL | 玩家表(用户 → 角色 1:N) |
| `TCharacter` | MySQL | 角色主表(职业 / 等级 / 金币 / 经验 / 装备 byte[28]) |
| `TCharacterItem` | MySQL | 玩家道具堆叠表 |
| `TCharacterBag` | MySQL | 玩家背包解锁状态 |
| `TCharacterQuest` | MySQL | 玩家任务状态机表 |
| `TCharacterFriend` | MySQL | 玩家好友关系表 |
| `TGuild` | MySQL | 公会主表 |
| `TGuildMember` | MySQL | 公会成员表 |
| `TGuildApply` | MySQL | 入会申请表 |

数据库模型见 `Src/Server/GameServer/GameServer/Entities.edmx`。

---

## 六、协议设计

### 6.1 三段式协议(Request / Response / Notify)

```
玩家操作 ─→ *Request{业务参数}
         ─→ Server: Service.OnXxx(自动派发)
         ─→ Manager.业务方法
         ─→ DBService.save()
         ─→ *Response{result, errormsg}
         + StatusNotify(状态变化,自动跟随主响应)
         ─→ Client: Service.OnXxx + StatusService.OnStatusNotify
```

### 6.2 协议规模

- `NetMessageRequest` 字段数:**40+**
- `NetMessageResponse` 字段数:**40+**
- `NetMessage` 顶层结构:`Request` + `Response` 两个 `oneof`
- 全局推送(Notify):`StatusNotify` / `GuildMemberAddNotify` / `GuildApplyResultNotify` / `ChatNotify` / `MapCharacterLeaveResponse` 等

### 6.3 服务器权威

- ❌ 客户端**不发身份**(playerId / characterId 都从 session 拿)
- ❌ 客户端**不决定状态**(成功/失败全由服务器判)
- ✅ 关键判断(`Items.ContainsKey` / `Gold` 够不够 / `CanSubmit`)全在服务端

---

## 七、运行指南

### 7.1 环境要求

| 环境 | 版本 |
|---|---|
| Unity | **2022.3.46f1c1 LTS** |
| .NET Framework | **4.6.1** |
| Visual Studio | **2019 / 2022**(打开 .sln) |
| MySQL | **8.0+**(本地或远程实例) |
| MySQL 连接器 | `MySql.Data 8.0.14`(已包含在 `packages/`) |
| OS | Windows 10 / 11 x64 |

### 7.2 数据库初始化

```bash
# 1. 启动 MySQL 8.0+,创建数据库
mysql -u root -p
CREATE DATABASE ExtremeWorld DEFAULT CHARACTER SET utf8mb4;
EXIT;

# 2. 修改服务端连接字符串
#    文件:Src/Server/GameServer/GameServer/Properties/Settings.Designer.cs
#    修改:Server=localhost;Database=ExtremeWorld;Uid=root;Pwd=你的密码;

# 3. EF6 首次运行会根据 Entities.edmx 自动建表
```

### 7.3 启动服务端

```bash
# 方式 A:命令行
cd Src/Server/GameServer/GameServer
nuget restore GameServer.sln   # 还原 packages
msbuild GameServer.sln /p:Configuration=Debug
bin\Debug\GameServer.exe

# 方式 B:Visual Studio
# 打开 Src/Server/GameServer/GameServer.sln → F5
```

启动后默认监听 `tcp://0.0.0.0:8888`(见 `Config.cs`)。

### 7.4 启动客户端

```bash
# 1. Unity Hub → Open → 选择 Src/Client/
# 2. 等 Unity 导入所有包(首次较慢)
# 3. 打开场景:Assets/Scenes/Login.unity
# 4. 点击 ▶  Play
```

---

## 八、开发规范

- **命名空间**:`GameServer`(服务端) / `Assets.Scripts`(客户端) / `SkillBridge.Message`(协议)
- **代码生成**:任何对 `Src/Lib/proto/message.proto` 的修改,必须重新生成 `Protocol/` 下的 .cs
- **日志**:服务端用 `log4net`,客户端用 `UnityLogger`
- **数据库**:禁止直接操作 EF6 实体做业务,**所有状态变更走 Manager**
- **网络**:禁止客户端发送身份字段(characterId / userId),全部由 session 反查

---

## 九、当前开发边界

### ✅ 已完整实现

- 登录 / 注册 / 角色创建 / 进入游戏
- 任务接取 / 交付 / 状态机 / 前置任务
- 道具获取 / 增减 / 商店购买
- 装备切换(7 槽位)
- 背包格子(客户端)+ 自动堆叠拆分
- 好友 / 公会 / 队伍 / 聊天(完整业务流)
- 地图实体同步 / 传送
- 状态广播(运行时增量同步)

### 🚧 部分实现

- 战斗系统:只有怪物生成,**没有技能 / 伤害公式 / 战斗结算**
- 装备属性:装备切换**没有实际加成到 Character 属性**

### ❌ 未实现

- 道具拖拽移动 / 出售给 NPC
- 装备强化 / 耐久度 / 随机属性
- 唯一 Item Instance GUID
- 拍卖行 / 邮件系统 / 交易系统
- 任务进度自动推进(目前没有击杀事件总线)
- Lua 热更新 / AOI 优化 / 对象池 / 防作弊

详见 `Doc/InterviewArchitectureReview/QuestSystem/05_当前实现边界.md`。

---

## 十、补充文档

| 文档 | 路径 | 内容 |
|---|---|---|
| 服务端架构总览 | `Doc/ServerArchitecture/` | Mermaid 思维导图 |
| 重构评审记录 | `Doc/RefactorReview/` | 历史重构决策 |
| 任务系统分析 | `Doc/InterviewArchitectureReview/QuestSystem/` | 5 篇面试准备文档 |
| 道具系统分析 | `Doc/InterviewArchitectureReview/ItemSystem/` | 2 篇分析文档 |
| protobuf 协议源 | `Src/Lib/proto/message.proto` | 70+ 消息定义 |
| EF6 数据模型 | `Src/Server/GameServer/GameServer/Entities.edmx` | 数据库映射 |

---

## 十一、致谢

- 网络层基于 **RayMix.NetLibs**(基于 .NET Socket 异步 TCP 库)封装
- 协议设计参考了多款经典 MMORPG 的客户端 / 服务端分离模式

---

## 十二、License

本项目仅用于个人学习与面试准备,**不做商业用途**。

---

<div align="center">

**⭐ 如果这个项目对你有帮助,欢迎 Star**

</div>
