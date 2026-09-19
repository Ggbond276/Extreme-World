# 任务系统架构重构审查报告

> 项目:`Extreme-World`(Unity MMO,半成品原型)
> 阶段:角色选择已完成,战斗系统尚未接入
> 性质:**READ-ONLY 审查 + 方案设计**,未修改任何项目源代码
> 输出位置:`Doc/RefactorReview/`
> 设计准则:**简单到能学、干净到能扩**,不追求企业级过度抽象

---

## 标签说明

为了让"哪些结论有代码支撑、哪些只是建议"一眼可辨,本报告统一使用以下标签:

- **Confirmed from code** —— 有代码证据,已在前一份审计报告中标注文件路径与行号
- **Inferred** —— 由代码现状合理推断,但作者意图无法证实
- **Proposed architecture** —— 本报告提出的设计方案,尚未落地

---

# 1. 当前现状(Current Situation)

## 1.1 当前架构的一句话概括

> **一个什么都管的 `QuestManager` + 一个很少用的本地事件 + 一条直接到 DB 的奖励链路**

具体地:

服务器侧的 `QuestManager.SubmitQuest` 同时承担了:
1. 完成判定(`CanSubmit`)
2. 金币 / 经验发放(直接修改 `Owner.Gold`、`Owner.Exp`)
3. 道具奖励发放(直接调用 `Owner.ItemManager.AddItem`)
4. 状态变更(直接修改 `DbQuest.Status`)
5. 持久化(直接调用 `DBService.Instance.save()`)
6. **没有广播任何任务完成事件**(Confirmed from code,见 `Managers/QuestManager.cs:65-110`)

客户端侧的 `QuestManager` 同时承担了:
1. 接取 / 提交的命令转发
2. 本地所有任务的状态镜像
3. NPC 上的任务过滤与排序
4. 表格读取 `DataManager.Instance.Quests`
5. 读 `User.Instance.CurrentCharacter` 做职业 / 等级过滤
6. 直接为 UI 提供原始字典 `allQuests` / `npcQuests`

UI 又做了:
1. 直接读取 `QuestManager.Instance.allQuests`(`UIQuestSystem.cs:54`)
2. 直接调用 `QuestManager.Instance.AcceptQuest` / `SubmitQuest`(`UIQuestDialog.cs:100, 112`)
3. 通过 `using static QuestManager;` 把枚举直接拉进 UI(`UIQuestStatus.cs:5`)

## 1.2 当前架构的实际可行性边界

虽然代码里有 `Kill`、`Item` 两种目标类型(`QuestDefine.cs:16-21`),但:
- 没有任何代码路径会自增 `Target1/2/3` —— **进度更新完全不存在**(Confirmed)
- `CanSubmit` 要求 `IsCompleted`,而唯一能让 `Status` 变成 `Complated` 的代码路径是 `Target1 == QuestTarget.None`(`Models/Quest.cs:31-41`)
- 因此**目前只能提交"无目标"的琐碎任务**

换句话说:在战斗系统加入之前,这个任务系统其实**只支持"接取 → 直接提交 → 拿奖励"**这一条极简路径。这条路径本身能跑通,问题都集中在"扩展性"上,不在"运行时崩溃"上。

## 1.3 已经踩到的、可观察的代码味道

| 味道 | 文件 / 行 | 严重程度 |
|---|---|---|
| `SubmitQuest` 单方法承担 5 件事 | `Server/.../Managers/QuestManager.cs:65-110` | 中(已能跑,加新奖励就会爆) |
| `Owner.Gold` / `Owner.Exp` 直接 += | `Server/.../Managers/QuestManager.cs:79, 84` | 中(setter 会广播,但 Quest Manager 不应"知道"这些 setter 存在) |
| `Owner.ItemManager.AddItem` 直接调用 | `Server/.../Managers/QuestManager.cs:91, 98, 105` | 中(每次 AddItem 都会触发 DBService.save(),一次提交存盘 3~4 次) |
| 客户端 `QuestManager.InitQuests` 读 `User.Instance.CurrentCharacter` | `Client/.../Managers/QuestManager.cs:54, 57` | 低(目前可工作,但 Manager 依赖全局 session 难单测) |
| UI 直接读 `QuestManager.Instance.allQuests` | `Client/.../UI/UIQuest/UIQuestSystem.cs:54` | 低(展示层穿透到存储层) |
| `QuestManager.OnQuestStatusChanged` 触发但零订阅者 | `Client/.../Managers/QuestManager.cs:208, 218`(触发);无任何 `+=` | 低(死事件,但证明作者意图已转向事件式) |
| `UIWorldElementManager.AddNpcQuestStatus` 存在但无人调用 | `Client/.../UI/UIWordElement/UIWorldElementManager.cs:35-55`;Grep 零调用 | 低(纯脚手架) |
| 一次提交 `DBService.save()` 被调 4 次 | `QuestManager.cs:108` + `ItemManager.cs:91` × N | 低(性能气味,不是 bug) |
| 一个 `Character` 持有 6 个 manager | `Entities/Character.cs:20-26` | 中(典型的 God-object,但在原型阶段可接受) |

---

# 2. 现在必须重构的(MUST REFACTOR NOW)

> 标准:即使战斗系统还没接入,这些问题也会导致**接下来任何一次小改动都要碰很多地方**。改的代价 < 不改的代价,所以现在做。

## 2.1 A1. 把奖励发放从 `QuestManager.SubmitQuest` 中拆出来

- **原因**:`SubmitQuest` 现在硬编码了 `RewardItem1/2/3` 的 schema,任何对奖励形式的微小改动(加货币、加声望、加装备位)都要改这个方法。
- **现状证据**:`Server/.../Managers/QuestManager.cs:76-107` 写死了 6 个 `if` 块,每个都直接调用具体系统。
- **必要性**:**高**。即使没有战斗,你想把"完成主线给一件装备"改成"按职业给不同装备",现在就要改 6 行 if 块。拆出来之后只改 Reward 配置。

## 2.2 A2. 增加"任务完成事件"(`QuestCompleted`)的最小骨架

- **原因**:即使今天没有任何订阅者,广播完成这件事是免费的(几个方法调用),不广播这件事是"债"。今天埋一个事件,明天成就系统、日常任务、邮件推送、统计系统就能直接订阅,而不用回头改 `QuestManager`。
- **现状证据**:`QuestManager.SubmitQuest` 一路改状态 + save,但没有任何 `Raise / Publish`。
- **必要性**:**高**。这是接入未来系统的入口,一旦现在不做,后面每次加新依赖都要回头碰 `QuestManager`。

## 2.3 A3. 客户端 `QuestManager.InitQuests` 不要直接读 `User.Instance.CurrentCharacter`

- **原因**:`QuestManager` 的任务是"管理任务状态",不是"判断玩家是否符合接取条件"。它现在被迫去 `User.Instance` 拉数据,导致无法独立构造,也无法在没有登录态的环境(例如测试、GM 工具、离线编辑器)使用。
- **现状证据**:`Client/.../Managers/QuestManager.cs:54, 57` 直接 `User.Instance.CurrentCharacter.Class / .Level`。
- **必要性**:**中**。今天还能跑,但每次想做"按当前等级过滤任务列表"的小优化都要从 UI 入手绕一圈。

## 2.4 A4. UI 不要直接遍历 `QuestManager.Instance.allQuests`

- **原因**:UI 在做"什么是可接取任务"这件事,而这本质是 Quest 业务规则。`UIQuestSystem.cs:59-74` 里的 `quest.Info == null`、`quest.Info.Status == QuestStatus.Finished` 判断是业务规则,不是显示规则。
- **现状证据**:`Client/.../UI/UIQuest/UIQuestSystem.cs:51-85`。
- **必要性**:**中**。今天业务规则简单,还能写;一旦"隐藏"逻辑超过 3 种(InProgress / Available / Complete / DailyOnly / ClassOnly),UI 就会被业务逻辑淹没。

---

# 3. 可以延后处理的(CAN BE DEFERRED)

> 标准:在战斗系统接入之前,这些问题不会阻塞新特性开发。可以等"第一次需要它"时再做。

## 3.1 B1. 抽象 `IQuestRepository`(取代直接 `DBService.Instance.Entities.CharacterQuests.Create`)

- **理由**:目前项目**只有这一个地方**用 EF6 直接操作 `TCharacterQuest`,且 ORM 切换不是当前的优先级。等出现第二个仓储(好友、公会都在用 EF6,迟早会统一抽象),再做。
- **当前证据**:`QuestManager.cs:53`。
- **风险**:不高。延后到"开始做单元测试"那一刻再做,价值最大。

## 3.2 B2. 把 `Character` 拆分成多个聚合根

- **理由**:`Character` 现在聚合了 `statusManager`、`ItemManager`、`questManager`、`friendManager`、`team`、`GuildId`。这是一个 God-object,但**所有这些 manager 都是 per-Character 的**。在战斗系统没接入前,职责边界变化不大,拆分的收益有限。
- **当前证据**:`Entities/Character.cs:20-26`。
- **风险**:不高。等战斗系统的 `CombatManager` 加入,会让 `Character` 再多挂一个 manager,届时一并重新规划。

## 3.3 B3. 完整的 `QuestStatus` 状态机

- **理由**:现在的状态枚举(`InProgress / Complated / Finished / Failed`)够用,`Complated` 拼写错误是历史问题,改名牵涉 proto 协议,要等"全局重构"或"协议升级"窗口期。
- **当前证据**:`message.proto:435-441`。
- **风险**:不影响功能。

## 3.4 B4. 客户端 `QuestService.OnQuestAccept` / `OnQuestSubmit` 直接弹 `MessageBox`

- **理由**:Service 层做 UI 展示不优雅,但目前客户端只有这一处需要做错误提示。等错误处理需求变复杂(例如要区分"背包满"、"等级不够"、"前置任务未完成"三种文案),再考虑抽象出 `IQuestErrorPresenter`。
- **当前证据**:`Client/.../Services/QuestService.cs:45, 68`。

## 3.5 B5. 客户端 `UIQuestSystem.UIRefresh()` 不会自动触发

- **理由**:`UIQuestSystem.UIRefresh()` 现在只能由 Tab 切换触发。这是个 UX 问题,不是架构问题。要做的是接上 `OnQuestStatusChanged` 事件,等 B6 做完自然就解决。

## 3.6 B6. 把 `OnQuestStatusChanged` 事件从"无订阅者"变成"真的有人在监听"

- **理由**:这个事件已存在,只是没接。接它属于"A2 的客户端对偶",建议在 A2 落地的同时一起做。

## 3.7 B7. 让 `QuestManager` 实现 `IPostResponser` 来搭车响应

- **理由**:`Character.PostResponse` 现在调度 `statusManager` + `friendManager`,但不调度 `questManager`(Confirmed:`Entities/Character.cs:97-121`)。这意味着任务变更不会在同一个 RPC 包内被推送给客户端,客户端必须等下一次任务 RPC。
- **风险**:目前只有 Accept / Submit 两个 RPC,且响应里已经包含了更新后的 `NQuestInfo`,所以"搭车"的实际收益不大。等任务 RPC 数量增加(任务列表刷新、进度推送)再做。

---

# 4. 不算问题的(NOT A PROBLEM AT CURRENT PROJECT STAGE)

> 这些在前一份审计报告里被点出来,但放到当前阶段,**它们不是问题**,过早修反而会拖慢开发节奏。

## 4.1 C1. "业务规则泄露到 UI"(显示层判断 `Info == null`)
- **理由**:原型期 UI 组件少,直接判断 `Info == null` 比新增一层 ViewModel 更快读懂。等 UI 组件超过 5 个再说。

## 4.2 C2. "Quest Manager 调了 ItemManager 是耦合"
- **理由**:在原型期,这种"显而易见的耦合"反而**有利于理解**。等你需要做"任务发装备,但玩家装备位满了就不能发"这种条件逻辑时,再抽象。

## 4.3 C3. "一次提交触发 4 次 save"
- **理由**:性能气味,不是 bug。当前并发量为 1 个玩家。原型期优化性能 = 浪费时间。

## 4.4 C4. "`StatusNotify` 不包含任务状态"
- **理由**:`StatusNotify` 是为金币 / 经验 / 道具这种**每次都变化**的状态增量设计的。任务状态变化频率极低(几十秒到几分钟一次),单独走 RPC 包更干净。

## 4.5 C5. "QuestListRequest / QuestAbandonRequest 是死协议"
- **理由**:这两个接口是预留给"将来按状态过滤"和"放弃任务"的,留着不报错,删了将来还得加回来。

## 4.6 C6. "God-object `Character`"
- **理由**:见 B2。原型阶段"一处管所有"的形态比"五处分散配置"的形态更易读。

## 4.7 C7. "`UIWorldElementManager.AddNpcQuestStatus` 无人调用"
- **理由**:这是为 NPC 头顶 "!" / "?" 图标准备的接口,接入工作是"加一个 NPC 渲染回调里",属于 UI 层的任务,不是架构问题。

---

# 5. 目标架构(Target Architecture)

> 设计原则:**5 个角色,1 个事件总线,零循环依赖,零跨层直接调用**。
> 不引入 Repository、不引入 CQRS、不引入 DDD Aggregate,**保持原型阶段的可读性**。

## 5.1 五个角色 + 一个总线

```
                  ┌─────────────────────────────────────────┐
                  │          Gameplay Event Bus             │
                  │  (Domain Event Bus, per-server-process)  │
                  └─────────────────────────────────────────┘
                            ▲                ▲
                            │ publish        │ subscribe
                            │                │
   ┌────────────────┐  ┌───┴────────────┐   │
   │ Combat System  │  │ Quest System   │   │  (未来的)Achievement
   │  (未实现)      │  │                │   │           Daily Quest
   └────────────────┘  └────────────────┘   │           Mail System
                            │               │
                            ▼               ▼
                  ┌─────────────────────────────────────────┐
                  │         其他 Server 系统                 │
                  │  ItemManager / Gold / Exp / DBService    │
                  └─────────────────────────────────────────┘
```

## 5.2 每个角色的职责(用大白话 + 专业术语)

### 5.2.1 `QuestState`(任务状态)
- **负责**:保存某玩家当前所有任务的运行时状态(进度、是否完成、是否已提交)。
- **不负责**:不知道 UI、不直接读写数据库、不修改金币 / 经验。
- **专业术语**:**Domain State / Aggregate Root(领域状态 / 聚合根)**

### 5.2.2 `QuestLogic`(任务业务规则)
- **负责**:回答"这个任务能不能接?""进度到了没有?""现在能不能提交?"。
- **不负责**:不知道 DB、不直接修改 `Owner.Gold`、不弹 UI。
- **专业术语**:**Domain Logic / Domain Service(领域逻辑 / 领域服务)**

### 5.2.3 `QuestService`(任务 RPC / 应用层)
- **负责**:接收客户端请求,调 `QuestLogic` 做校验,然后让 `QuestState` 变更,并通过 Event Bus 广播结果。
- **不负责**:不知道 DB Schema、不计算奖励。
- **专业术语**:**Application Service / Use Case(应用服务 / 用例)**

### 5.2.4 `RewardService`(奖励发放)
- **负责**:拿到一份 `RewardDefine`,把它拆成金币 / 经验 / 道具分别调用对应系统,并负责一次性 `DBService.save()`。
- **不负责**:不知道任务存在、不知道 UI、不知道玩家正在做什么。
- **专业术语**:**Reward Dispatcher(奖励分发器)**

### 5.2.5 `QuestRepository`(任务持久化)
- **负责**:封装 `TCharacterQuest` 表的创建 / 更新 / 查询。
- **不负责**:不知道业务规则、不知道金币 / 道具。
- **专业术语**:**Repository Pattern(仓储模式)**

### 5.2.6 `GameplayEventBus`(领域事件总线)
- **负责**:承载"X 发生了"这类事实(`MonsterKilled`、`ItemCollected`、`QuestCompleted` 等)。
- **不负责**:不知道事件的具体业务含义,只做转发。
- **专业术语**:**In-process Pub/Sub Bus(进程内发布订阅总线)**
- **注意**:不是 `MessageDistributer` 那种网络 RPC 路由器,也不是 `Action<>` 这种局部委托。它是**服务器进程内、单例、key 是事件类型、订阅者按 type 注册**的字典。

## 5.3 客户端对应五个角色

```
QuestState      ← Models/Quest (struct,纯数据) + 客户端 QuestManager 持有的字典
QuestLogic      ← QuestManager 中"判断 NPC 上的任务优先级"等纯函数(可以抽成静态方法)
QuestService    ← Services/QuestService(已经是 RPC 入口)
RewardService   ← 暂时不需要(客户端不发放奖励,等服务器结果直接刷状态)
Repository      ← 暂时不需要(客户端不持久化任务)
UI 层           ← UIQuest* 一组脚本,只通过 ViewModel 访问 QuestState
EventBus(订阅)  ← 客户端监听服务器推送(目前用 MessageDistributer;QuestSystem 自己再暴露本地事件)
```

---

# 6. 职责边界(Responsibility Boundaries)

| 关注点 | 现在的所有者 | 目标的所有者 | 理由 |
|---|---|---|---|
| 谁拥有任务运行时状态? | `QuestManager.Quests`(server),`QuestManager.allQuests`(client) | `QuestState`(server 新),保持 `QuestManager` 单例暴露只读视图(client) | 状态容器只管状态 |
| 谁被允许修改任务状态? | `QuestManager.AcceptQuest` / `SubmitQuest` | `QuestState` 自身(只有 `QuestService` 调用) | 修改入口唯一 |
| 谁判断任务完成? | `Quest.InitQuetsDefine`(只处理 `Target1 == None`) | `QuestLogic.Evaluate(quest)` —— 以后接入 `MonsterKilled` 事件 | 完成判定 = 业务规则 |
| 谁发放奖励? | `QuestManager.SubmitQuest` 硬编码 6 个 if 块 | `RewardService.Grant(reward, character)` | 奖励 schema 改起来不该牵动 Quest |
| 谁保存任务数据? | `QuestManager` 内 `DBService.Instance.save()` | `QuestRepository.Save(quest)` + 上层统一提交 | 持久化收口 |
| 谁和客户端通信? | `QuestService`(server)+ `QuestService`(client) | 不变 | 已经是分层架构 |
| 谁刷新 UI? | UI 自己 `UIRefresh()` 手动调用 | UI 订阅 `QuestState.OnChanged` 事件 | 数据变化驱动 UI |
| 谁发布游戏事件? | 无 | `QuestService` 在状态变更后 `EventBus.Publish(...)` | 接入未来的钩子 |
| 谁订阅游戏事件? | 无 | `QuestLogic` 订阅 `MonsterKilled` / `ItemCollected` / `PlayerLevelChanged` | 战斗系统接入点 |
| 谁播报给客户端? | `QuestAcceptResponse` / `QuestSubmitResponse` | 不变,响应里继续带 `NQuestInfo` | 不破坏 wire 协议 |
| 谁维持 NPC 上的 "?" / "!" 状态? | `QuestManager.GetQuestStatusByNpc`(未被调用) | 同上,但通过 `QuestState` 触发 `OnNpcStatusChanged` 事件 | UI 解耦 |

---

# 7. 客户端架构(Client Architecture)

## 7.1 现状问题(再总结)

- `QuestManager` 同时是:状态容器 + 业务规则 + RPC 客户端 + UI 数据源
- UI 直接 `using static QuestManager;`(`UIQuestStatus.cs:5`)
- UI 直接遍历 `QuestManager.Instance.allQuests`(`UIQuestSystem.cs:54`)
- UI 直接调用 `QuestManager.AcceptQuest`(`UIQuestDialog.cs:100`)

## 7.2 目标分层

```
┌─────────────────────────────────────────────────────┐
│  UI 层 (View)                                       │
│  UIQuestSystem / UIQuestInfo / UIQuestDialog        │
│  UIQuestItem / UIQuestStatus                        │
│  只接触:QuestViewModel + QuestListItem + IQuestUI   │
└──────────────────────┬──────────────────────────────┘
                       │ subscribe: OnQuestChanged, OnNpcQuestChanged
                       │ call:      IQuestUI.Accept(questId)
                       │            IQuestUI.Submit(questId)
                       ▼
┌─────────────────────────────────────────────────────┐
│  ViewModel 层 (新增,薄薄一层)                        │
│  QuestListItem(只读:Id/Name/Status/IsAvailable)     │
│  NpcQuestIndicator(只读:NpcId/Indicator)            │
│  把 QuestManager 内部字典转成"展示需要的数据"          │
└──────────────────────┬──────────────────────────────┘
                       │ read
                       ▼
┌─────────────────────────────────────────────────────┐
│  Domain 层                                          │
│  QuestManager(单例,持有 allQuests / npcQuests)       │
│  Quest(model,只读对外)                               │
│  ★ 通过 Event 通知变化,不再让 UI 来 poll            │
└──────────────────────┬──────────────────────────────┘
                       │ use
                       ▼
┌─────────────────────────────────────────────────────┐
│  Application 层                                     │
│  QuestService(RPC 入口)                              │
│  接收服务器推送 → 转成 Quest 状态变更                 │
│  接收 UI 请求 → 发送 RPC                             │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
                NetClient → 服务器
```

## 7.3 关键改造点

### 7.3.1 `QuestManager` 不再被 UI 主动 poll

**现在**:
```csharp
// UIQuestSystem.cs:54
foreach(var kv in QuestManager.Instance.allQuests) { ... }
```

**目标**:
```csharp
// UIQuestSystem.OnEnable (示意,本报告不修改代码)
QuestManager.Instance.OnQuestChanged += Refresh;

// QuestManager 内部 (示意)
public event Action<Quest> OnQuestChanged;
private void SetQuestInfo(Quest q) {
    q.Info = newInfo;
    OnQuestChanged?.Invoke(q);  // 替代"等用户切 Tab 才刷新"
}
```

### 7.3.2 `QuestManager.InitQuests` 不再读 `User.Instance`

**现在**:
```csharp
// QuestManager.cs:54, 57
if (questDefine.LimitClass != CharacterClass.None && 
    questDefine.LimitClass != User.Instance.CurrentCharacter.Class) continue;
if (questDefine.LimitLevel > User.Instance.CurrentCharacter.Level) continue;
```

**目标**:把过滤规则封装成一个静态方法,接受 character 作为参数:
```csharp
// QuestManager.Init(quests, character) ← 把 character 显式传入
public void Init(List<NQuestInfo> quests, Character character) { ... }
// 过滤逻辑变成:
//   if (!QuestFilter.CanAccept(questDefine, character)) continue;
```
这样 QuestManager 不再依赖 `User.Instance` 这个全局单例,可以独立构造和测试。

### 7.3.3 NPC 上的任务过滤也要走事件

**现在**:`QuestManager.GetQuestStatusByNpc` 被定义但**没有任何调用者**(Grep 零命中)。

**目标**:把"什么时候查 NPC 状态"这件事**委托给 NPC 的渲染层**,而不是让 QuestManager 主动触发。
- 客户端 UIWorldElementManager 在渲染 NPC 时调一次 `QuestManager.GetQuestStatusByNpc(npcId)`
- 当 `OnQuestChanged` 触发时,UIWorldElementManager 重新查询受影响的 NPC
- 这样 QuestManager 不知道"世界 UI"的存在

---

# 8. 服务器架构(Server Architecture)

## 8.1 现状问题

- `QuestManager.SubmitQuest` 一个方法管 5 件事(完成判定 / 金币 / 经验 / 道具 / 持久化)
- `QuestManager` 直接调 `Owner.ItemManager.AddItem`(具体类,具体方法)
- `QuestManager` 直接读写 `DbQuest.Status`(具体 EF6 实体)

## 8.2 目标分层

```
┌─────────────────────────────────────────────────────┐
│  RPC Service 层                                     │
│  QuestService                                       │
│  · OnQuestAccept / OnQuestSubmit                    │
│  · 调 QuestLogic.CanAccept / CanSubmit              │
│  · 调 QuestState.ApplyAccept / ApplySubmit          │
│  · 调 RewardService.Grant(reward, character)       │
│  · 调 EventBus.Publish(new QuestCompleted(...))    │
└──────────────────────┬──────────────────────────────┘
                       │
       ┌───────────────┼────────────────┐
       ▼               ▼                ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────────┐
│ QuestLogic  │ │ QuestState  │ │ RewardService   │
│             │ │             │ │                 │
│ CanAccept   │ │ ApplyAccept │ │ Grant(reward,   │
│ CanSubmit   │ │ ApplySubmit │ │       character)│
│ Evaluate    │ │ AddProgress │ │                 │
│             │ │             │ │ 内部调:         │
│ 纯函数,    │ │ 持有:       │ │ Gold setter     │
│ 无副作用    │ │ Dictionary  │ │ Exp setter      │
│             │ │ <int,Quest> │ │ ItemManager     │
│             │ │             │ │ .AddItem        │
└─────────────┘ └──────┬──────┘ │                 │
                       │        │ 一次 save       │
                       │        └─────────────────┘
                       ▼
                ┌─────────────┐
                │QuestRepo    │
                │             │
                │Create /     │
                │Update /     │
                │Save         │
                └─────────────┘
```

## 8.3 关键改造点

### 8.3.1 `QuestLogic` —— 纯判断,不修改

```csharp
// 概念示意(本报告不修改代码)
public static class QuestLogic
{
    public static bool CanAccept(QuestDefine def, Character owner) { ... }
    public static bool CanSubmit(Quest quest, Character owner) { ... }
    public static QuestTargetResult Evaluate(Quest quest) { ... } // 返回是否完成
}
```
- **可单元测试**
- **没有副作用**(不写 DB,不修改 owner)
- **未来战斗事件接入就发生在这里**:`Evaluate` 看 `MonsterKilled` 事件,更新 `Target1` 计数

### 8.3.2 `QuestState` —— 状态容器 + 修改入口

```csharp
// 概念示意
public class QuestState
{
    public Character Owner { get; }
    public Dictionary<int, Quest> Quests { get; }
    
    public Result ApplyAccept(int questId)         // 单一入口
    public Result ApplySubmit(int questId)         // 单一入口
    public void AddProgress(int questId, int idx, int delta)  // 战斗事件回调
    
    public event Action<Quest> OnQuestChanged;     // 给 UI / EventBus
}
```

### 8.3.3 `RewardService` —— 唯一被允许改 Gold/Exp/Item 的地方

```csharp
// 概念示意
public class RewardService
{
    public void Grant(RewardDefine reward, Character target)
    {
        if (reward.Gold > 0) target.Gold += reward.Gold;   // 走 setter,广播
        if (reward.Exp  > 0) target.Exp  += reward.Exp;
        foreach (var item in reward.Items) target.ItemManager.AddItem(item.id, item.count);
        // 一次 save 在外面统一调用,不要每个 ItemManager.AddItem 各 save
    }
}
```

### 8.3.4 `QuestRepository` —— 包装 EF6

```csharp
// 概念示意
public class QuestRepository
{
    public TCharacterQuest Create(int characterId, int questId);
    public void Update(TCharacterQuest quest);
    public void Save();  // 或 SaveChanges
}
```
**为什么延后到"出现第二个仓储"再做**:目前项目有 6 个 EF 实体表直接被各 Manager 触碰,Quest 不是最严重的;统一抽象的 ROI 要等"开始写单元测试"或"考虑切换 ORM"时才显现。

## 8.4 `Character` 的处理

- **保持** `Character` 是 6 个 manager 的容器,不拆
- **新增** `Character.questState` 字段(替代 `questManager`)
- **新增** `Character.PostResponse` 调用 `questState.PostResponse`(但只在接 B7 时做)

---

# 9. 事件总线边界(Event Bus Boundary)

> 这是为战斗系统接入准备的"骨架",不是当下要落地的功能。

## 9.1 什么是"事件总线"

**用大白话**:
> 一个小本本,上面记着"谁想听什么消息"。谁发生了什么事,就翻本本,把消息告诉所有想听的人。说话的人不需要知道听的人是谁。

**专业术语**:**In-process Domain Event Bus(进程内领域事件总线)** / **Mediator Pattern(中介者模式)** / **Pub/Sub(发布订阅)**

## 9.2 事件 vs 直接调用 —— 现在就要想清楚

**记住一句话**:事件是"我发生了,你愿不愿意知道",不是"我要你干这事"。

| 调用类型 | 什么时候用 | 例子 |
|---|---|---|
| **直接命令 Command** | "请你做这事,我等你回复" | `QuestService.OnQuestAccept(request)` → `QuestState.ApplyAccept` |
| **查询 Query** | "你有什么?" | `QuestManager.OpenNpcQuest(npcId)` → 返回 bool / Quest |
| **事件 Event** | "我做了这事,你愿不愿意知道" | `EventBus.Publish(new MonsterKilled(...))` → 多个系统各自决定 |

| 当前调用 | 应该保持 | 理由 |
|---|---|---|
| `QuestService.OnQuestAccept` → `questManager.AcceptQuest` | ✅ **直接命令** | 这是 RPC 请求 / 响应,不是事件 |
| `UIQuestDialog.OnClickAcceptButton` → `QuestManager.AcceptQuest(quest)` | ✅ **直接命令** | UI 的明确意图 |
| `QuestManager.OpenNpcQuest(npcId)` 查 NPC 上的任务 | ✅ **查询** | 同步返回值,UI 立刻要 |
| `QuestManager.allQuests` 遍历 | ⚠️ 应改为**事件驱动** | UI 不该 poll,应该订阅变更 |
| `QuestManager.SubmitQuest` → `Owner.ItemManager.AddItem` | ❌ **应改为事件** | "我提交了任务"是事实,不是命令 |
| `QuestManager.AcceptQuest` → `DBService.save()` | ❌ **应改为仓储抽象** | 这是持久化,不是事件 |
| (未来的)战斗代码 → `QuestManager.AddProgress` | ❌ **应改为事件** | "我打死了怪物"是事实,不是命令 |
| (未来的)`ItemManager.AddItem` → `QuestManager.AddProgress` | ❌ **应改为事件** | "玩家拿了道具"是事实 |

## 9.3 现在必须埋的事件点(共 3 个)

| 事件 | 发布者 | 订阅者(现在 / 未来) | 载荷 |
|---|---|---|---|
| `QuestAccepted` | `QuestService.OnQuestAccept` | 仅记录日志(未来:成就、日常、统计) | `characterId, questId, timestamp` |
| `QuestCompleted` | `QuestService.OnQuestSubmit`(在判定成功后) | 仅记录日志(未来:成就、日常、统计) | `characterId, questId, reward` |
| `QuestProgressChanged` | `QuestState.AddProgress` | 客户端 RPC / UI 刷新 | `characterId, questId, targetIdx, newValue` |

## 9.4 未来要埋的事件点(战斗系统接入时)

| 事件 | 发布者 | 订阅者 | 载荷 |
|---|---|---|---|
| `MonsterKilled` | 未来的 Combat System | `QuestLogic.Evaluate`(按 quest 模板筛) | `characterId, monsterId, count` |
| `ItemCollected` | 未来的 Combat / Loot System | `QuestLogic.Evaluate` | `characterId, itemId, count` |
| `NpcTalked` | 未来的 NPCService | `QuestLogic.Evaluate` | `characterId, npcId` |
| `PlayerLevelChanged` | 现有 `Character.Exp` setter(改成走 EventBus) | `QuestLogic.Evaluate`(用于 `LimitLevel` 解锁提示) | `characterId, oldLevel, newLevel` |

**为什么这些要事件而不是直接调用**:
- 战斗系统是一个独立的新模块,**不应该让战斗系统 import Quest 命名空间**
- 同一类事件(例如 MonsterKilled)会被成就系统、日常任务、统计系统都关心 —— 战斗系统不可能 import 所有这些
- 直接调用会让 Combat 和 Quest 互相耦合,加一个新系统就要改两边
- 用事件,Combat 只发布"怪物死了",所有关心的人各自订阅

**为什么不是全局的**:
- 玩家 A 打死怪物不会影响玩家 B 的任务进度
- 所以事件是 **character-scoped(按 character 范围化)**
- 事件总线收到 `MonsterKilled { characterId = A.Id, ... }`,只通知订阅了 `A` 的 QuestLogic
- 实现方式:`EventBus.Publish<T>(T evt)` 让订阅者从 `evt.CharacterId` 自己过滤

## 9.5 不需要事件的(明确)

| 场景 | 为什么不事件 |
|---|---|
| 玩家点击"接取"按钮 | 用户意图,需要同步返回值(成功 / 失败 / 错误消息) |
| 客户端查 NPC 上的任务 | 查询,同步返回值 |
| 服务器 RPC 响应 | 协议层已经是事件路由(`MessageDistributer`),不要再套一层 |
| UI 切 Tab 刷新 | 直接调用 `UIRefresh()` 是 UI 内部命令 |
| 启动时 Init 任务列表 | 一次性 bootstrap |

## 9.6 事件总线的最小实现(设计,本报告不修改代码)

> 给代码作者一个明确的设计参考,让他知道"如果将来要加 EventBus,长什么样"。

```csharp
// 概念示意,本报告不修改代码
public class GameplayEventBus
{
    private Dictionary<Type, List<Delegate>> _handlers = new();
    
    public void Subscribe<T>(Action<T> handler) where T : IGameplayEvent
    {
        if (!_handlers.ContainsKey(typeof(T))) _handlers[typeof(T)] = new List<Delegate>();
        _handlers[typeof(T)].Add(handler);
    }
    
    public void Publish<T>(T evt) where T : IGameplayEvent
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
            foreach (var h in list) ((Action<T>)h)(evt);
    }
}

public interface IGameplayEvent { int CharacterId { get; } }
// 事件示例:
public class MonsterKilled : IGameplayEvent { public int CharacterId { get; set; } public int MonsterId; }
public class QuestCompleted : IGameplayEvent { public int CharacterId { get; set; } public int QuestId; }
// ...
```

> **绝对不要**为了加这个 EventBus 而去新建一个第三方依赖库(NuGet / npm)。手写一个 ~30 行的类即可。这就是为什么我说"不追求企业级"。

---

# 10. 战斗系统集成边界(Combat System Integration Boundary)

> 这是本次审查**最重要**的部分,因为它决定了未来 Combat 接入时的工作量。

## 10.1 战斗系统接入前必须存在的 3 个"钩子"

### 钩子 1:`QuestLogic.Evaluate(quest)` —— **纯函数,可测试**

```csharp
// 概念示意
public static class QuestLogic
{
    // 输入:当前任务 + 最近一次事件
    // 输出:任务是否完成 / 进度如何变化
    public static QuestEvaluation Evaluate(Quest quest, IGameplayEvent evt)
    {
        // 遍历 quest.Define.Target1/2/3
        // 检查 evt 是不是相关(Kill 任务关心 MonsterKilled,Item 任务关心 ItemCollected)
        // 如果相关,返回新的进度值
        // 如果所有目标都达到 TargetNum,返回 IsCompleted = true
        return new QuestEvaluation { NewProgress = ..., IsCompleted = ... };
    }
}
```

### 钩子 2:`QuestState.AddProgress(questId, idx, delta)` —— **修改入口**

```csharp
// 概念示意
public class QuestState
{
    public void AddProgress(int questId, int targetIdx, int delta)
    {
        if (!Quests.TryGetValue(questId, out var quest)) return;
        // 修改 quest.DbQuest.TargetN
        // 调 QuestLogic.Evaluate 检查是否完成
        // 如果完成,设置 quest.DbQuest.Status = Complated
        // 触发 OnQuestChanged 事件
        // 不在这里 save —— save 由调用方统一收口
    }
}
```

### 钩子 3:`GameplayEventBus.Subscribe<MonsterKilled>(...)` —— **订阅点**

```csharp
// 在 QuestLogic 注册时(QuestState 构造时):
GameplayEventBus.Instance.Subscribe<MonsterKilled>(OnMonsterKilled);

private void OnMonsterKilled(MonsterKilled evt)
{
    // 遍历该 character 的所有 Quest
    foreach (var quest in Quests.Values)
    {
        if (quest.Info.Status != QuestStatus.InProgress) continue;
        var result = QuestLogic.Evaluate(quest, evt);
        if (result.ProgressChanged) AddProgress(quest.QuestId, result.TargetIdx, result.Delta);
    }
}
```

## 10.2 战斗系统接入时,战斗代码长什么样

```csharp
// 在未来的 Combat System 中(伪代码)
public class CombatManager
{
    public void OnMonsterDied(Monster monster, Character killer)
    {
        // ... 经验 / 掉落逻辑 ...
        GameplayEventBus.Instance.Publish(new MonsterKilled
        {
            CharacterId = killer.Id,
            MonsterId = monster.Define.ID,
            Count = 1
        });
        // ↑ 战斗代码不需要知道 Quest 存在
    }
}
```

## 10.3 接入工作量估算

| 步骤 | 工作量 | 文件数 |
|---|---|---|
| 1. 在 CombatManager 里 `Publish(MonsterKilled)` | 30 分钟 | 1 |
| 2. 在 QuestLogic.Evaluate 里加 `MonsterKilled` 分支 | 1 小时 | 1 |
| 3. 在 QuestState 构造里 `Subscribe<MonsterKilled>` | 30 分钟 | 1 |
| 4. 测试一条 Kill 任务完整跑通 | 2 小时 | 1~2 |
| **总计** | **半天** | **3~4** |

对比"在 Combat 里直接调 QuestManager.AddProgress"的方案,这种事件化做法的额外工作量:
- 多写一个 EventBus 类:30 行,~1 小时
- 多写一个事件类型:`MonsterKilled`,10 行,5 分钟
- 多写一个订阅注册:5 行,5 分钟

**总额外成本:~1 小时**。换取:
- 移除 Combat → Quest 的直接依赖
- 自动支持成就系统、日常任务、统计系统的接入(它们只需要 `Subscribe<MonsterKilled>`)
- Quest Manager 不再因新增任务目标类型而需要修改

---

# 11. 重构难度评估(Refactoring Difficulty)

> 评估基于当前代码现状,以"最小破坏现有功能"为前提。

| ID | 改造项 | 难度 | 文件数 | 风险 | 可增量 |
|---|---|---|---|---|---|
| A1 | 拆 `RewardService` | **Low** | 1 新 + 1 改 | 低(奖励 schema 没变) | ✅ |
| A2 | 加 `QuestCompleted` 事件骨架 | **Low** | 1 新 + 1 改 | 极低(只新增,不修改现有逻辑) | ✅ |
| A3 | 客户端 `Init` 接受 character 参数 | **Low** | 1 改 | 低 | ✅ |
| A4 | UI 改用 ViewModel / 事件 | **Medium** | 3~4 改 | 中(UI 行为要保持一致) | ✅ |
| B1 | 抽象 `IQuestRepository` | **Medium** | 1 新 + 2 改 | 低 | ✅ |
| B2 | 拆 `Character` 聚合 | **High** | 5+ 改 | 高(影响 6 个 manager 的构造) | ❌(建议推迟) |
| B3 | 状态机重写 | **Medium** | 1 改 + 协议更新 | 中(协议升级,客户端要跟着改) | ❌(建议推迟到协议升级窗口) |
| B4 | 错误处理抽象 | **Low** | 1 改 | 低 | ✅(延后) |
| B5/B6 | 接 `OnQuestStatusChanged` 事件 | **Low** | 1~2 改 | 低 | ✅(顺带做) |
| B7 | `QuestManager` 实现 `IPostResponser` | **Low** | 1~2 改 | 低 | ✅ |

**Legend**:
- **Low** = 1~2 小时,单文件
- **Medium** = 半天,~5 文件
- **High** = 1 天以上,跨多个 manager

## 11.1 增量重构计划

> 每一步都**保持现有功能可运行**。

### Phase 1 —— 清理职责(预计 1 天)
- **Step 1.1**: 抽出 `RewardService`,`QuestManager.SubmitQuest` 改为调用它
- **Step 1.2**: 在 `QuestService.OnQuestSubmit` 成功路径末尾,加 `EventBus.Publish(new QuestCompleted(...))` 的空实现(EventBus 此时只是一个空类)
- **Step 1.3**: 客户端 `QuestManager.Init` 接受 character 参数
- **Step 1.4**: 把 `QuestManager.InitQuests` 中的过滤逻辑抽成静态方法 `QuestFilter.CanAccept(questDefine, character)`

**结果**:没有任何可见行为变化,但 `QuestManager.SubmitQuest` 从 45 行缩到 ~15 行。

### Phase 2 —— 接入 QuestLogic(预计半天)
- **Step 2.1**: 新建 `QuestLogic`(静态类),把 `CanSubmit` / `InitQuetsDefine` 里的纯逻辑搬进去
- **Step 2.2**: `QuestState`(原 `QuestManager`)只调 `QuestLogic` 做判断,自己不写判断

**结果**:测试可以脱离 Entity Framework 跑。

### Phase 3 —— 客户端事件化(预计半天)
- **Step 3.1**: 把 `QuestManager.allQuests` / `npcQuests` 改成只读 getter,内部维护一个 `OnQuestChanged` 事件
- **Step 3.2**: `UIQuestSystem` 订阅 `OnQuestChanged`,替代手动 `UIRefresh()`(Tab 切换仍可保留)
- **Step 3.3**: `UIWorldElementManager` 接入 `AddNpcQuestStatus`,NPC 渲染时调一次

**结果**:UI 不再需要手动 poll,接取 / 提交后立即看到状态变化。

### Phase 4 —— 战斗系统接入(预计半天)
- **Step 4.1**: 实现 `GameplayEventBus`(30 行)
- **Step 4.2**: 在 `QuestLogic` 加 `Evaluate(quest, IGameplayEvent)` 分支(处理 `MonsterKilled`)
- **Step 4.3**: `QuestState` 构造时 `Subscribe<MonsterKilled>`
- **Step 4.4**: 未来 Combat 代码 `Publish(MonsterKilled)`(本次不动)

**结果**:Quest 准备好听战斗事件,Combat 一接入就生效。

### Phase 5(可选,延后)—— 仓储抽象 + Character 拆分
- 等出现第二个仓储需求或开始写单元测试时再做
- 见 B1 / B2

---

# 12. 当前 vs 目标架构(Current vs Target)

## 12.1 文字对比

### CURRENT(现在)

```
UI (UIQuestDialog / UIQuestSystem)
 ↓ 直接调用
QuestManager (客户端)                      
 ↓ SendQuestAccept/Submit                 
QuestService (客户端 RPC)                
 ↓ NetMessage                             
 ─── wire ───                             
QuestService (服务器 RPC)                
 ↓ 直接调用                                
QuestManager (服务器)                    
 ├─→ DBService.Instance.save()          ← 持久化混在领域逻辑里
 ├─→ Owner.ItemManager.AddItem()        ← 直接依赖具体 ItemManager
 ├─→ Owner.Gold += RewardGold           ← 直接修改角色字段
 └─→ Owner.Exp  += RewardExp            
                                             
                                          ← 没有完成广播事件
                                          ← 没有 Event Bus
                                          ← 战斗接入时:Combat → QuestManager.AddProgress(...)
                                                                       (紧耦合)
```

### TARGET(目标)

```
UI (UIQuestDialog / UIQuestSystem)
 ↓ "我想接取" (Intent)                    
 ↓                                        
QuestManager (客户端, 持有 State + 暴露事件) 
   ├─ 暴露:OnQuestChanged(订阅)
   └─ 调用:QuestService.SendQuestAccept   
       ↓                                   
       QuestService (客户端 RPC)            
       ↓                                   
       ─── wire ───                        
       QuestService (服务器 RPC)            
       ↓ 调                               
       QuestLogic.CanAccept(quest, owner) ← 纯判断
       ↓ 若通过                            
       QuestState.ApplyAccept(questId)    ← 唯一修改入口
       ↓                                   
       RewardService.Grant(reward, owner) ← 奖励发放,内部调 Gold/Exp/Item
       ↓                                   
       QuestRepository.Save()             ← 持久化收口
       ↓                                   
       GameplayEventBus.Publish(QuestCompleted) ← 广播,供未来系统订阅
       ↓                                   
       ─── wire ───                        
       客户端推送 (QuestAcceptResponse)     
       ↓                                   
       QuestManager.OnAccept(info)        
       ↓ 触发 OnQuestChanged               
       ↓                                   
       UI 自动刷新 (订阅事件)               
       
       ─── 未来 Combat 接入 ───            
       CombatManager.OnMonsterDied        
       ↓ Publish(MonsterKilled)           
       ↓                                   
       QuestLogic.Evaluate(quest, evt)    ← 战斗事件触发进度更新
       ↓                                   
       QuestState.AddProgress(...)        
       ↓                                   
       GameplayEventBus.Publish(QuestProgressChanged) 
       ↓                                   
       客户端收到 → UI 刷新                 
```

## 12.2 Mermaid 对比图

详见 `QuestSystem_Current_vs_Target.mmd`。

---

# 13. 为什么目标架构更容易学(Learning Value)

## 13.1 回答 6 个常见问题,看哪个版本更直接

| 问题 | 当前架构的答案 | 目标架构的答案 |
|---|---|---|
| 谁拥有任务数据? | "看 `QuestManager.Quests` 字典,但是它混在 SubmitQuest 方法里改" | "看 `QuestState`,它只被 `QuestService` 修改" |
| 谁修改任务数据? | "`QuestManager.AcceptQuest` 和 `SubmitQuest` 这两个方法" | "只有 `QuestState.ApplyAccept` / `ApplySubmit` / `AddProgress`,所有修改都要走它们" |
| 谁请求一个操作? | "UI 直接调 `QuestManager.AcceptQuest(quest)`" | "UI 调 `QuestManager` 的 intent 方法(如 `RequestAccept(questId)`),Manager 通过 `QuestService` 发 RPC" |
| 谁保存数据? | "`QuestManager.SubmitQuest` 里有一行 `DBService.Instance.save()`,但道具发放时 `ItemManager.AddItem` 也会 save,一共 4 次" | "`QuestRepository.Save()`,且只有一次,奖励发完统一 save" |
| 谁发布事件? | "没有人,只有死掉的 `OnQuestStatusChanged`" | "`QuestService` 在 RPC 处理成功后 `EventBus.Publish(QuestCompleted)`,由 `QuestState` / `RewardService` 等订阅" |
| 谁监听事件? | "没人监听 `OnQuestStatusChanged`(作者忘了)" | "`QuestState` 监听 `MonsterKilled` / `ItemCollected`,UI 监听 `OnQuestChanged`" |
| 谁依赖谁? | "UI → QuestManager → ItemManager → DBService,中间任何一处都互相知道" | "UI → QuestManager → (QuestLogic / QuestState / RewardService / EventBus),依赖箭头都是单向" |

## 13.2 新人 1 小时入门路径

| 步骤 | 读什么 | 理解什么 |
|---|---|---|
| 1 | `Models/Quest.cs` + `Data/QuestDefine.cs` | 任务是什么(数据模型) |
| 2 | `QuestLogic.cs`(纯函数) | 业务规则(可测试) |
| 3 | `QuestState.cs` | 状态怎么改(单一入口) |
| 4 | `RewardService.cs` | 奖励怎么发(单一入口) |
| 5 | `QuestService.cs` + wire proto | 怎么和客户端对话 |
| 6 | `GameplayEventBus.cs` + 几个 `*Event.cs` | 怎么通知别的系统 |

**当前架构**:新人要读完 `QuestManager.cs:1-112` + `Quest.cs:1-60` + `Models/Character.cs:1-122`,才能拼出一个"任务怎么跑"的模糊图。**而且没有主线**,因为所有逻辑都挤在两个 100+ 行的方法里。

**目标架构**:每个文件只负责一件事,每个方法不超过 30 行。

## 13.3 复杂度对比

| 维度 | 当前 | 目标 |
|---|---|---|
| 一个方法的平均行数 | 30~45 | < 20 |
| 一个文件的平均行数 | 100+ | < 80 |
| 一个类依赖的具体类数 | 4~6 | 1~3 |
| 跨模块调用需要 import 的命名空间数 | 5+ | 2~3 |
| 改一个业务规则需要碰的文件数 | 3~5 | 1~2 |
| 加一个新事件类型需要碰的文件数 | N/A(没有) | 1 |
| 加一个新奖励类型需要碰的文件数 | 3(Quest + Character + UI) | 1(Reward 配置) |

---

# 14. 风险与不要过度设计的事(Risks & Don't Over-Engineer)

## 14.1 不要做这些事

### ❌ 不要现在引入 Repository / UnitOfWork
- **理由**:项目只有 1 处用 EF6 直接操作 `TCharacterQuest`。第二个仓储出现前,抽象没有 ROI。

### ❌ 不要现在做 DDD Aggregate / Bounded Context
- **理由**:项目是一个半成品原型,根本没有"领域建模"可言。DDD 在这里 = 过度抽象。

### ❌ 不要做 CQRS / Event Sourcing
- **理由**:同上。Query 和 Command 当前是同一段代码里的不同 if 分支,分开只会增加调试难度。

### ❌ 不要把 `Character` 拆成 6 个聚合
- **理由**:见 B2。所有 manager 都是 per-Character 的,聚合只是物理上分开,不会带来清晰度。

### ❌ 不要引入第三方 EventBus 库(MassTransit / MediatR / nServiceBus 等)
- **理由**:手写 30 行 `Dictionary<Type, List<Delegate>>` 即可。第三方库带来 NuGet 依赖、版本升级成本、调试难度。

### ❌ 不要重写整个项目
- **理由**:当前架构虽然有气味,但**所有功能可工作**。重写 = 一个月不能玩游戏。**永远不要重写,只增量重构**。

### ❌ 不要把 `OnQuestStatusChanged` 升级成 `IQuestObserver` 接口
- **理由**:`Action<Quest>` 已经够用。接口化只是增加文件数。

### ❌ 不要现在实现"任务链" / "任务依赖图"
- **理由**:作者当前只支持 `PreQuest`(单一前置任务 ID),配置已经够用。"任务图"是未来需求,不是当下需求。

### ❌ 不要把"琐碎任务自动完成"逻辑移除
- **理由**:它在 `Models/Quest.cs:31-41` 的 `InitQuetsDefine` 里,作为"接取时直接完成"的快捷路径存在。这是个产品决策,不是 bug。

## 14.2 风险列表

| 风险 | 描述 | 缓解 |
|---|---|---|
| **破坏现有功能** | 重构 `QuestManager` 时漏改某条路径 | 每步完成后**运行可用的客户端 / 服务器**,先看 AcceptQuest / SubmitQuest 还能跑 |
| **协议破坏** | proto 改了客户端没跟上 | 暂不改 proto;所有改动只在 C# 层面 |
| **调试变难** | 事件链路比直接调用难追踪 | 事件总线实现里加 `Log.Info` 打印 publish / subscribe,在开发期保留日志 |
| **新人不知所措** | 五个角色比一个 Manager 概念多 | 在 `Doc/RefactorReview/` 留这张图;每个文件顶部加 1~2 行的"职责"注释 |
| **过早优化** | 在性能不构成瓶颈的地方花时间 | 性能气味(C3、C4)继续放着,等真实并发压力出现再优化 |

## 14.3 必须保留不变的事

| 项 | 理由 |
|---|---|
| proto 协议 (`message.proto:431-499`) | 客户端 / 服务器共用,改了要同步两端 |
| `MessageDistributer` 的 RPC 路由机制 | 全局消息分发,不能换 |
| `TCharacterQuest` 表结构 | EF6 持久化的真实形式 |
| `QuestDefine.cs` 的 schema | 设计师配置文件,改了策划要重新填表 |
| `NPCManager` 的"任务优先"逻辑 | 是产品决策(任务永远比商店优先) |
| `OnQuestStatusChanged` 事件名 | 已存在于 API,改名会让现有的 `OnQuestStatusChanged` 失效 |

---

# 15. 总结

## 15.1 三个分类清单

### MUST REFACTOR NOW(4 项,~1 天工作量)
- **A1**: 拆 `RewardService`
- **A2**: 加 `QuestCompleted` 事件骨架
- **A3**: 客户端 `QuestManager.Init` 接受 character 参数
- **A4**: UI 改用 ViewModel / 事件

### CAN BE DEFERRED(7 项)
- **B1**: 抽象 `IQuestRepository`(等出现第二个仓储)
- **B2**: 拆 `Character` 聚合(等战斗系统接入)
- **B3**: 状态机重写(等协议升级窗口)
- **B4**: 错误处理抽象(等错误处理变复杂)
- **B5/B6**: 接 `OnQuestStatusChanged` 事件(顺带做,无成本)
- **B7**: `QuestManager` 实现 `IPostResponser`(等 RPC 增加再做)

### NOT A PROBLEM AT CURRENT STAGE(7 项)
- **C1**: 业务规则泄露到 UI
- **C2**: Quest Manager 调 ItemManager
- **C3**: 一次提交触发 4 次 save
- **C4**: `StatusNotify` 不包含任务状态
- **C5**: `QuestListRequest` / `QuestAbandonRequest` 是死协议
- **C6**: God-object `Character`
- **C7**: `UIWorldElementManager.AddNpcQuestStatus` 无人调用

## 15.2 一句话结论

> **任务系统现在能跑,但任务进度更新路径完全缺失,加上战斗系统一定会把 `QuestManager` 撑爆。**
>
> **最优先做 4 件事:拆奖励、加事件、客户端不读 `User.Instance`、UI 不读 `QuestManager` 内部字典。**
>
> **战斗系统接入的工作量应该控制在半天以内 —— 通过 `GameplayEventBus` 而不是直接调 `QuestManager`。**

## 15.3 给作者的一句话建议

> 别急着重构。先把 Phase 1 的 4 步做了,跑一遍游戏确认还能接任务 / 提交任务,再做 Phase 2/3。每次只改一件事,改完立刻测。
>
> **简单到能学、干净到能扩**。这是这个项目的目标,不是"完美架构"。

---

# 附录:配套图表

| 文件名 | 用途 |
|---|---|
| `QuestSystem_Target_Architecture.mmd` | 目标架构总览 |
| `QuestSystem_Current_vs_Target.mmd` | 当前 vs 目标对比 |
| `QuestSystem_Combat_Event_Boundary.mmd` | 战斗系统接入时的边界 |
| `QuestSystem_Refactor_Difficulty.md` | 每项改造的难度 / 风险 / 工作量明细 |

所有图表使用 Mermaid 语法,可用 Mermaid Live Editor / GitHub / VSCode Mermaid 预览插件查看。
