<div align="center">

# Extreme World

**Unity MMO 客户端 / 服务端架构实践**

[![Unity](https://img.shields.io/badge/Unity-2021%2B-black?logo=unity)](https://unity.com)
[![C#](https://img.shields.io/badge/.NET-4.6.1-blue?logo=dotnet)](https://dotnet.microsoft.com)
[![Protobuf](https://img.shields.io/badge/Protocol-protobuf3-green)](https://protobuf.dev)
[![EF6](https://img.shields.io/badge/ORM-Entity%20Framework%206-purple)](https://learn.microsoft.com/ef)
[![Status](https://img.shields.io/badge/Status-开发中-orange)]()

> 一个完整覆盖 **客户端 + 服务端 + 共享协议层** 的 Unity 多人在线角色扮演游戏(MMO)架构项目。
> 系统性地实现传统 MMORPG 所需的全部核心模块,并在 **分层 / 协议 / 同步 / 持久化** 等关键架构点上做出可解释的设计取舍。

</div>

---

## 📌 项目概述

**Extreme World** 是一个用于**学习和面试准备**的 Unity MMO 全栈架构项目,目标不是完成一款商业级游戏,而是**把 MMO 架构里的每一个难点都用代码正面回答一遍**:

- 客户端 / 服务端如何分离?
- 协议怎么设计才能让客户端不发身份、不发状态?
- 玩家登录时如何把整个 `NCharacterInfo` 一次下发?
- 运行时金币 / 经验 / 道具变化如何**增量同步**给客户端?
- 跨系统调用(任务发奖 / 商店购买 / 装备切换)如何控制边界?

每个业务系统都配套**中文架构分析文档**,放于 `Doc/InterviewArchitectureReview/`,从**需求 → 数据 → 协议 → 权限 → 模块 → 同步 → 持久化 → 代码**一条线推导"为什么这样设计",而不只是背类名。

---

## 🏗️ 项目结构

```
Extreme-World/
├── Src/
│   ├── Client/                          Unity 客户端
│   │   └── Assets/Scripts/
│   │       ├── Entities/                Character / Entity / Monster
│   │       ├── GameObject/              实体控制器 / 相机 / 输入 / NPC
│   │       ├── Managers/                Bag / Item / Equip / Quest / Friend /
│   │       │                            Guild / Team / Chat / NPC / Data / ...
│   │       ├── Models/                  运行时数据类(Item / BagItem / Quest / User)
│   │       ├── Network/                 NetClient(TcpClient 封装)
│   │       ├── Services/                ItemService / QuestService / UserService
│   │       │                            / StatusService / ChatService / ...
│   │       ├── UI/                      UIBag / UIEquip / UIQuest / UIFriend /
│   │       │                            UIGuild / UITeam / UIShop / UIChat / ...
│   │       └── Utilities/               Singleton / MonoSingleton / Resloader
│   │
│   ├── Server/GameServer/GameServer/    .NET 服务端
│   │   ├── Entities/                    Character / CharacterBase / Entity / Monster
│   │   ├── Managers/                    Item / Equip / Quest / Friend / Guild /
│   │   │                                Team / Chat / Map / Shop / Spawn /
│   │   │                                Status / Session / Data / Entity
│   │   ├── Models/                      Item / Quest / Friend / Guild / Team / Map
│   │   ├── Network/                     TcpSocketListener / NetConnection /
│   │   │                                NetSession / MessageDistributer
│   │   ├── Services/                    Item / Quest / Friend / Guild / Team /
│   │   │                                Chat / Map / User / DB
│   │   ├── *.cs                         TCharacter / TCharacterItem /
│   │   │                                TCharacterBag / TCharacterQuest / ...
│   │   │                                (EF6 自动生成的数据库实体)
│   │   └── GameServer.csproj            .NET Framework 4.6.1
│   │
│   ├── Lib/                             跨端共享库
│   │   ├── Common/Data/                 ItemDefine / EquipDefine / QuestDefine /
│   │   │                                ShopDefine / ShopItemDefine / ...(JSON 反序列化)
│   │   ├── proto/message.proto          所有跨端网络协议定义
│   │   └── Protocol/                    protobuf 自动生成的 C# 消息类
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
    └── InterviewArchitectureReview/     ⭐ 面试准备文档库
        ├── README.md                    系统索引
        ├── QuestSystem/                 任务系统分析(5 份)
        └── ItemSystem/                  道具系统分析(2 份)
```

---

## 🧩 已实现的系统模块

| 模块 | 服务端 | 客户端 | 关键能力 | 状态 |
|---|---|---|---|---|
| **登录 / 注册** | `UserService` | `UILogin` / `UIRegister` | TCP 连接、注册、角色创建、进入游戏全量同步 | ✅ |
| **角色 / 实体** | `Character` / `Entity` / `Monster` | `EntityController` / `MapController` | 实体生命周期、地图同步、移动 | ✅ |
| **地图 / 传送** | `MapService` / `MapManager` | `TeleporterObject` | 地图切换、传送点、NPC / 怪物生成 | ✅ |
| **任务系统** | `QuestService` / `QuestManager` | `UIQuestSystem` / `UIQuestDialog` | 接取 / 交付 / 状态机 / 前置任务 / NPC 头顶图标 | ✅ |
| **道具系统** | `ItemService` / `ItemManager` | `ItemManager` / `BagManager` | 增减数量、自动堆叠拆分 | ✅ |
| **装备系统** | `EquipManager` | `EquipManager` / `UICharEquip` | 7 槽位装备切换、`byte[28]` 持久化 | ✅ |
| **背包** | `TCharacterItem` 表 | `BagManager` / `UIBag` | 格子位置、自动拆分堆叠、UI 渲染 | ✅ |
| **商店** | `ShopManager` | `UIShop` / `UIShopItem` | 购买道具、金币扣减 | ✅ |
| **好友** | `FriendService` / `FriendManager` | `FriendManager` / `UIFriends` | 好友列表、上下线通知 | ✅ |
| **公会** | `GuildService` / `GuildManager` | `GuildManager` / `UIGuildMain` | 创建公会、加入、成员管理、申请审批 | ✅ |
| **队伍** | `TeamService` / `TeamManager` | `TeamManager` / `UITeamSystem` | 组队、状态同步 | ✅ |
| **聊天** | `ChatService` / `ChatManager` | `ChatManager` / `UIChat` | 全服 / 私聊广播 | ✅ |
| **战斗** | `MonsterManager` / `Spawner` | `PlayerInputController` | 怪物生成、AOI 同步 | 🚧 部分 |
| **状态广播** | `StatusManager` | `StatusService` | 金币 / 经验 / 道具 增量同步 | ✅ |

---

## 🛠️ 技术栈

### 客户端
- **Unity 2021+** + **C#**
- **单例 + Action 事件 + 协程** 作为基础编程模式
- **TcpClient** 异步收发,protobuf 序列化

### 服务端
- **.NET Framework 4.6.1** Console 应用
- **TcpSocketListener**(基于 RayMix.NetLibs)多连接异步监听
- **Entity Framework 6** ORM,SQL Server 持久化
- **MessageDistributer<NetConnection<NetSession>>** 自动消息派发
- **PostResponse 钩子** 把状态变化塞进当前响应包

### 共享层
- **protobuf3** 定义所有跨端消息(70+ 个 message / enum)
- **JSON** 配置静态策划数据,通过 `DataManager` 加载到字典
- **EF6 Entity Designer** 自动生成 `TCharacter*` 数据库实体

### 数据库
- **SQL Server** + EF6 Code First 反向工程
- 核心表:`TUser` / `TPlayer` / `TCharacter` / `TCharacterItem` /
  `TCharacterBag` / `TCharacterQuest` / `TCharacterFriend` /
  `TGuild` / `TGuildMember` / `TGuildApply`

---

## 🎯 架构亮点(面试重点)

### 1. 三层数据建模

```
静态策划配置(JSON)     玩家动态数据(EF6 实体)     运行时包装对象
ItemDefine       →     TCharacterItem       →      Models.Item
EquipDefine      →     TCharacter.Equips    →      EquipManager.Equips
QuestDefine      →     TCharacterQuest      →      Models.Quest
                  ↓                          ↓
            跨会话真理来源                当前会话工作区
```

静态配置全服共享、玩家状态每个角色一行 / 多行、运行时对象把两者包起来加业务方法。

### 2. 服务器权威架构

客户端**永远不发身份、不决定状态**:

- ❌ 客户端不会说"我已经有 999 个血瓶"
- ✅ 客户端只会说"我要使用 ItemId 1001"
- ✅ 服务器从 `sender.Session.Character` 拿身份
- ✅ 关键判断(`Items.ContainsKey` / `Gold` 够不够 / `CanSubmit`)全在服务端

证据:`Src/Server/GameServer/GameServer/Managers/EquipManager.cs`
```csharp
if (!character.ItemManager.Items.ContainsKey(itemId))
    return Result.Failed;
```

### 3. 三段式协议(Request / Response / Notify)

```
玩家操作 → ItemBuyRequest{shopId, shopItemId}
        → ItemService.OnItemBuy(自动派发)
        → ShopManager.BuyItem
        → ItemManager.AddItem
        → DBService.save()
        → ItemBuyResponse{result}
        + StatusNotify(金币 -50, 道具 +1, 自动跟随主响应)
        → 客户端 ItemService.OnItemBuy + StatusService.OnStatusNotify
```

每个业务操作对应一个 `Request`;响应回 `result`;状态变化通过 **`StatusNotify` 增量推送**,不需要专门的 `ItemNotify`。

### 4. 登录全量 + 运行时增量

- **登录**:`UserService.OnGameEnter` 把整个 `NCharacterInfo`(包含 Items / Bag / Equips / Quests / Friends / Guild)一次推下来
- **运行时**:任何状态变化(金币 / 经验 / 道具)都走 `StatusManager.AddXxxChange` → `PostResponse` 钩子 → 自动塞进当前响应包

### 5. Service vs Manager 职责分层

| 角色 | 生命周期 | 职责 |
|---|---|---|
| **Service**(单例) | 全局唯一 | 网络层入口、收包 / 回包、转发到对应 Manager |
| **Manager**(per-character) | 随 Character 生命周期 | 业务逻辑、内存状态、操作 EF6 实体 |

任务 / 道具 / 好友 / 公会 / 队伍 全部遵循这一分层。

### 6. 跨系统调用统一入口

任务奖励、商店购买、装备切换**都通过** `character.ItemManager.AddItem`,**绝不直接修改 EF6 实体**:

```csharp
// 任务系统发奖
Owner.ItemManager.AddItem(quest.Define.RewardItem1, quest.Define.RewardItem1Count);
// 商店系统购买
character.ItemManager.AddItem(define.ItemID, 1);
```

这样 ItemManager 集中负责所有"道具数量变化",业务方只需要知道"加几个"。

---

## 📐 一张图记住整体架构

```
                      ┌──────────────────────────────┐
                      │   Unity Client (C#)          │
                      │                              │
                      │  UI ─→ Manager ─→ Service    │
                      │              │               │
                      │              ▼               │
                      │         NetClient            │
                      └──────────────┬───────────────┘
                                     │  TCP + protobuf3
                                     ▼
                      ┌──────────────────────────────┐
                      │   GameServer (.NET 4.6.1)    │
                      │                              │
                      │   Service ─→ Manager         │
                      │              │               │
                      │              ▼               │
                      │   Character(per-player)      │
                      │   ├── ItemManager            │
                      │   ├── QuestManager           │
                      │   ├── FriendManager          │
                      │   ├── GuildManager           │
                      │   ├── TeamManager            │
                      │   └── statusManager          │
                      │              │               │
                      │              ▼               │
                      │     DBService.save()         │
                      └──────────────┬───────────────┘
                                     │  EF6 / SQL Server
                                     ▼
                                ┌─────────┐
                                │   DB    │
                                └─────────┘
```

---

## 🚀 运行项目

### 1. 环境要求

- **Unity 2021+**(客户端)
- **Visual Studio 2019+** / **.NET Framework 4.6.1**(服务端)
- **SQL Server 2017+**(数据库)

### 2. 数据库初始化

```bash
# 1. 创建数据库 ExtremeWorld
# 2. 配置连接字符串:Src/Server/GameServer/GameServer/Settings.Designer.cs
# 3. EF6 根据 TCharacter/TUser/... 自动建表
```

### 3. 启动服务端

```bash
cd Src/Server/GameServer/GameServer
dotnet run
# 或用 Visual Studio 打开 GameServer.sln → F5
```

### 4. 启动客户端

```bash
# Unity Hub 打开 Src/Client/
# 启动场景:Assets/Scenes/Login.unity
```

---

## 📚 文档索引(面试准备)

> 📂 `Doc/InterviewArchitectureReview/` — 中文架构分析文档库
> 每个系统都按 **需求 → 数据 → 行为 → 协议 → 权威 → 模块 → 同步 → 持久化 → 代码** 一条线推导。

### 已有分析

| 系统 | 文档 | 内容 |
|---|---|---|
| **任务系统** | `QuestSystem/01_从0设计任务系统.md` | 完整思考框架教程(需求 → 代码) |
| | `QuestSystem/02_面试口述版.md` | 自然口语版回答(可直接背诵) |
| | `QuestSystem/03_面试官拷打题.md` | 按难度递进的问题清单 |
| | `QuestSystem/04_代码证据.md` | 每个结论的真实代码引用 |
| | `QuestSystem/05_当前实现边界.md` | 已实现 / 未实现的明确分隔 |
| **道具系统** | `ItemSystem/01_架构分析.md` | 从需求推导到代码的完整分析(13 节 + 速记卡) |
| | `ItemSystem/02_面试拷问.md` | 12 大类递进问题(不含答案) |

### 阅读顺序建议

1. 先读 `QuestSystem/01_从0设计任务系统.md` 学会思考框架
2. 面试前 1 小时读 `QuestSystem/02_面试口述版.md` 背诵要点
3. 模拟面试对照 `QuestSystem/03_面试官拷打题.md` 自我追问
4. 任何具体类不记得时,翻 `QuestSystem/04_代码证据.md` 找原文
5. 用 `QuestSystem/05_当前实现边界.md` 防止"过度自信"

---

## ⚠️ 当前开发边界 / 已知未完成

> 项目仍在开发中,以下功能**尚未实现**或**部分实现**,面试时务必诚实区分。

### 完整实现的

- ✅ 登录 / 注册 / 角色创建 / 进入游戏
- ✅ 任务接取 / 交付 / 状态机 / 前置任务
- ✅ 道具获取 / 增减 / 商店购买
- ✅ 装备切换(7 槽位)
- ✅ 背包格子(客户端) + 自动堆叠拆分
- ✅ 好友 / 公会 / 队伍 / 聊天
- ✅ 地图实体同步 / 传送

### 部分实现

- 🚧 战斗系统:只有怪物生成,没有技能 / 伤害公式 / 战斗结算
- 🚧 装备属性:装备切换没有实际加成到 Character 属性(Character 没有从装备读 STR/HP 等)

### 未实现

- ❌ 道具拖拽移动 / 拆分堆叠
- ❌ 出售给 NPC
- ❌ 装备强化 / 耐久度 / 随机属性
- ❌ 唯一 Item Instance GUID
- ❌ 拍卖行 / 邮件系统 / 交易系统
- ❌ 任务进度自动推进(目前没有击杀事件总线)
- ❌ Lua 热更新
- ❌ AOI 优化(当前地图同步是全量广播)
- ❌ 对象池(ObjectPool)
- ❌ 防作弊 / 服务器压力测试

---

## 📊 项目数据

| 维度 | 数量 |
|---|---|
| 客户端 C# 脚本 | 100+ |
| 服务端 C# 类 | 60+ |
| protobuf 消息 | 70+ |
| 已实现核心系统 | 12 |
| 架构分析文档 | 7 篇 |
| 总代码行数 | 15,000+ |

---

## 🤝 致谢

本项目的服务端网络层基于 **RayMix.NetLibs**(基于 .NET 的高性能异步 TCP 库)封装。架构设计参考了多款经典 MMORPG 的客户端 / 服务端分离模式。

---

## 📜 License

本项目仅用于个人学习与面试准备,不做商业用途。

<div align="center">

**⭐ 如果这个项目对你有帮助,欢迎 Star**

</div>
