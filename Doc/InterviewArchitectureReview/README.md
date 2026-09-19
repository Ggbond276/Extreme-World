# Interview Architecture Review · 索引

> **目的**:建立长期可复用的面试复习知识库。每个系统按**同一套思考框架**分析:
> **需求 → 数据 → 行为 → 协议 → 权威边界 → 模块划分 → 同步 → 持久化 → 代码落地**

---

## 标签约定

- **[当前项目实现]** — 在本项目代码中能找到的证据
- **[通用设计知识]** — 行业里通用的概念,但本项目未实现
- **[代码推断]** — 项目没有明确代码,但基于上下文合理推断

---

## 系统索引

| 系统 | 状态 | 说明 |
|---|---|---|
| **QuestSystem** | ✅ 已分析 | 见 `QuestSystem/` |
| **ItemSystem** | ✅ 已分析 | 见 `ItemSystem/` |
| **NetworkSystem** | 🚧 未分析 | TCP+protobuf 收发 |
| **ResourceSystem** | 🚧 未分析 | Resloader / 资源加载 |
| **LuaSystem** | ❌ 当前项目未实现 | 目录保留待未来分析 |
| **UISystem** | 🚧 未分析 | UIWindow / TabView / ListView |
| **ObjectPoolSystem** | ❌ 当前项目未实现 | 目录保留待未来分析 |
| **HotUpdateSystem** | ❌ 当前项目未实现 | 目录保留待未来分析 |
| **BattleSystem** | 🚧 部分实现 | 怪物 / 战斗 |
| **GuildSystem** | 🚧 部分实现 | GuildManager / GuildService |
| **ChatSystem** | 🚧 部分实现 | ChatManager / ChatService |
| **TeamSystem** | 🚧 部分实现 | TeamManager / TeamService |

---

## QuestSystem 当前内容

- 📄 **`01_从0设计任务系统.md`** — 思考框架推理教程(需求→代码)
- 📄 **`02_面试口述版.md`** — 自然口语版回答(可直接背诵)
- 📄 **`03_面试官拷打题.md`** — 按难度递进的问题清单
- 📄 **`04_代码证据.md`** — 每个结论的真实代码引用
- 📄 **`05_当前实现边界.md`** — 已实现 / 未实现的明确分隔

### QuestSystem 关键源文件

**服务端**:
- `Src/Server/GameServer/GameServer/Services/QuestService.cs`
- `Src/Server/GameServer/GameServer/Managers/QuestManager.cs`
- `Src/Server/GameServer/GameServer/Models/Quest.cs`
- `Src/Server/GameServer/GameServer/Entities/Character.cs`
- `Src/Server/GameServer/GameServer/TCharacter.cs`
- `Src/Server/GameServer/GameServer/TCharacterQuest.cs`
- `Src/Server/GameServer/GameServer/Services/DBService.cs`
- `Src/Server/GameServer/GameServer/Managers/DataManager.cs`
- `Src/Lib/Common/Data/QuestDefine.cs`
- `Src/Lib/proto/message.proto`

**客户端**:
- `Src/Client/Assets/Scripts/Managers/QuestManager.cs`
- `Src/Client/Assets/Scripts/Services/QuestService.cs`
- `Src/Client/Assets/Scripts/Models/Quest.cs`
- `Src/Client/Assets/Scripts/UI/UIQuest/UIQuestDialog.cs`
- `Src/Client/Assets/Scripts/UI/UIQuest/UIQuestSystem.cs`
- `Src/Client/Assets/Scripts/UI/UIQuest/UIQuestInfo.cs`
- `Src/Client/Assets/Scripts/Managers/NPCManager.cs`
- `Src/Client/Assets/Scripts/Managers/DataManager.cs`
- `Src/Client/Assets/Scripts/Services/UserService.cs`(OnGameEnter 初始化)

### QuestSystem 待解决/待深入问题

1. 任务进度(target1/2/3)如何被推进?目前没有击杀事件总线
2. 任务奖励发放没有专门的 RewardService,直接写在 QuestManager.SubmitQuest
3. 没有任务事件总线(EventBus),客户端 OnQuestStatusChanged 只是一个 Action 委托
4. 服务端 QuestManager 没有重置 `GoldSnapshot` 类似的发包一致性检查
5. 队伍共享任务未实现,目前任务严格 per-character

---

## ItemSystem 当前内容

- 📄 **`01_架构分析.md`** — 从需求推导到代码的完整分析(13 节 + 速记卡)
- 📄 **`02_面试拷问.md`** — 按思考链递进的 12 大类问题(不含答案)

### ItemSystem 关键源文件

**服务端**:
- `Src/Server/GameServer/GameServer/Services/ItemService.cs`
- `Src/Server/GameServer/GameServer/Managers/ItemManager.cs`
- `Src/Server/GameServer/GameServer/Managers/EquipManager.cs`
- `Src/Server/GameServer/GameServer/Managers/ShopManager.cs`
- `Src/Server/GameServer/GameServer/Managers/StatusManager.cs`
- `Src/Server/GameServer/GameServer/Models/Item.cs`
- `Src/Server/GameServer/GameServer/Entities/Character.cs`
- `Src/Server/GameServer/GameServer/TCharacter.cs`
- `Src/Server/GameServer/GameServer/TCharacterItem.cs`
- `Src/Server/GameServer/GameServer/TCharacterBag.cs`
- `Src/Lib/Common/Data/ItemDefine.cs`
- `Src/Lib/Common/Data/EquipDefine.cs`
- `Src/Lib/proto/message.proto`

**客户端**:
- `Src/Client/Assets/Scripts/Managers/ItemManager.cs`
- `Src/Client/Assets/Scripts/Managers/BagManager.cs`
- `Src/Client/Assets/Scripts/Managers/EquipManager.cs`
- `Src/Client/Assets/Scripts/Services/ItemService.cs`
- `Src/Client/Assets/Scripts/Services/StatusService.cs`
- `Src/Client/Assets/Scripts/Models/Item.cs`
- `Src/Client/Assets/Scripts/Models/BagItem.cs`
- `Src/Client/Assets/Scripts/UI/UIBag/UIBag.cs`
- `Src/Client/Assets/Scripts/Managers/DataManager.cs`

### ItemSystem 关键设计要点(一句话总结)

```
ItemDefine(静态) + TCharacterItem(玩家数量) + byte[28](装备槽位)
                ↓
   ItemManager(数量) + BagManager(格子位置) + EquipManager(装备槽)
                ↓
   登录全量 NCharacterInfo,运行时 StatusNotify 增量
```

### ItemSystem 待解决/待深入问题

1. 背包格子位置**只在客户端 BagManager**,服务端没有对应实现
2. `EquipManager` 服务端是**全局单例**,但操作 `character.Data.Equips`,并发场景下需要校验
3. `TCharacterBag.Items byte[]` 字段存在但**未使用**,设计意图未落地
4. `ItemManager.UseItem` 方法签名存在但**功能未接通**(只有 remove,没有真正使用逻辑)
5. 没有拖拽移动 / 拆分堆叠 / 出售给 NPC
6. 没有装备强化 / 耐久度 / 随机属性 / 唯一实例 GUID

---

## 使用方式

1. 先读 `QuestSystem/01_从0设计任务系统.md` 学会思考框架
2. 面试前 1 小时读 `QuestSystem/02_面试口述版.md` 背诵要点
3. 模拟面试时对照 `QuestSystem/03_面试官拷打题.md` 自我追问
4. 任何具体类不记得时,翻 `QuestSystem/04_代码证据.md` 找原文
5. 防止"过度自信",用 `QuestSystem/05_当前实现边界.md` 确认自己没瞎编
