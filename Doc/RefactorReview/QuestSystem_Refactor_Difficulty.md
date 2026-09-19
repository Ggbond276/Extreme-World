# 任务系统重构难度评估明细表

> 本表逐项列出**已分类的所有改造点**(MUST / CAN BE DEFERRED),
> 配套给出:难度、文件数、受影响系统、风险、是否可增量执行。
> 用于回答"重构工作量是多少?"和"先做哪一步?"。

---

## 难度评估标准

| 等级 | 工作量 | 影响范围 | 风险 |
|---|---|---|---|
| **Low** | 1~2 小时 | 1 个文件 | 不破坏现有功能 |
| **Medium** | 半天 | ~5 文件 | 局部行为可能微调 |
| **High** | 1 天以上 | 多个模块 | 跨模块改动,容易回归 |

---

## MUST REFACTOR NOW(必须现在重构)

### A1. 抽出 `RewardService`(把奖励发放从 `QuestManager.SubmitQuest` 中拆出来)

| 项 | 内容 |
|---|---|
| **改动类型** | 责任拆分 (Responsibility Split) |
| **难度** | **Low** |
| **预计工作量** | 1.5 小时 |
| **新增文件** | `Src/Server/GameServer/GameServer/Services/RewardService.cs`(1 个新文件) |
| **修改文件** | `Src/Server/GameServer/GameServer/Managers/QuestManager.cs` |
| **受影响系统** | 仅 `QuestManager` |
| **破坏现有功能** | 否(行为完全等价) |
| **风险点** | 极低。`QuestManager.SubmitQuest` 内部顺序(发放奖励 → 改状态 → save)保持不变,只是把"发放奖励"那 6 个 if 块原封不动搬进 `RewardService.Grant` |
| **可增量执行** | ✅ 是。可以在 `RewardService.Grant` 内部保留与原代码完全相同的逻辑,只调整调用方 |
| **前置依赖** | 无 |
| **后续依赖** | 无 |

**具体改动示意**(本报告不修改代码,只描述):
```
QuestManager.SubmitQuest(questId):
  if (!CanSubmit(questId)) return Error
  // ↓↓↓ 新增调用 ↓↓↓
  RewardService.Grant(GetQuestReward(questId), owner)
  // ↑↑↑ 替换原 6 个 if 块 ↑↑↑
  DbQuest.Status = QuestStatus.Finished
  DBService.Instance.save()
  NotifyClient()
```

---

### A2. 加 `QuestCompleted` 事件骨架

| 项 | 内容 |
|---|---|
| **改动类型** | 新增 In-process Event Bus |
| **难度** | **Low** |
| **预计工作量** | 1.5 小时 |
| **新增文件** | `Src/Server/GameServer/GameServer/Gameplay/GameplayEventBus.cs`<br/>`Src/Server/GameServer/GameServer/Gameplay/Events/QuestCompleted.cs` |
| **修改文件** | `Src/Server/GameServer/GameServer/Managers/QuestManager.cs`(在 SubmitQuest 末尾加 `EventBus.Publish`) |
| **受影响系统** | 仅 `QuestManager` |
| **破坏现有功能** | 否(纯新增) |
| **风险点** | 极低。事件 Bus 在第一次实现里只接受一个订阅者(日志记录),不会影响现有路径 |
| **可增量执行** | ✅ 是。可以分两步:先建空 Bus 类 + 空事件类型,再加 Publish 调用 |
| **前置依赖** | 无 |
| **后续依赖** | Phase 4 战斗系统接入 |

**具体改动示意**(本报告不修改代码,只描述):
```csharp
// 1. 新建 GameplayEventBus.cs (~30 行 Dictionary)
public class GameplayEventBus
{
    private Dictionary<Type, List<Delegate>> _handlers = new();
    public void Subscribe<T>(Action<T> handler) where T : IGameplayEvent { ... }
    public void Publish<T>(T evt) where T : IGameplayEvent { ... }
}

// 2. 新建 QuestCompleted.cs
public class QuestCompleted : IGameplayEvent
{
    public int CharacterId { get; set; }
    public int QuestId { get; set; }
}

// 3. QuestManager.SubmitQuest 末尾追加一行
GameplayEventBus.Instance.Publish(new QuestCompleted { ... });
```

---

### A3. 客户端 `QuestManager.Init` 接受 character 参数

| 项 | 内容 |
|---|---|
| **改动类型** | 解耦 (Decouple from `User.Instance`) |
| **难度** | **Low** |
| **预计工作量** | 1 小时 |
| **修改文件** | `Src/Client/Assets/Scripts/Managers/QuestManager.cs`<br/>`Src/Client/Assets/Scripts/Services/UserService.cs`(Init 的调用方) |
| **受影响系统** | 仅客户端 Quest Manager 初始化路径 |
| **破坏现有功能** | 否(签名变更,调用方同时改) |
| **风险点** | 低。`UserService.cs` 在角色进入游戏时调 `QuestManager.Instance.InitQuests(...)`,只多传一个参数 |
| **可增量执行** | ✅ 是 |
| **前置依赖** | 无 |
| **后续依赖** | 无 |

**具体改动示意**(本报告不修改代码,只描述):
```csharp
// before
public void InitQuests(IEnumerable<NQuestInfo> quests)
{
    var character = User.Instance.CurrentCharacter;
    // 过滤逻辑直接读 character
}

// after
public void InitQuests(IEnumerable<NQuestInfo> quests, CharacterInfo character)
{
    // 过滤逻辑用传入的 character 参数
}
```

---

### A4. UI 改用 ViewModel / 事件

| 项 | 内容 |
|---|---|
| **改动类型** | UI / Domain 解耦 |
| **难度** | **Medium** |
| **预计工作量** | 半天 |
| **新增文件** | `Src/Client/Assets/Scripts/Models/QuestViewModel.cs`(只读包装) |
| **修改文件** | `Src/Client/Assets/Scripts/UI/UIQuest/UIQuestSystem.cs`<br/>`Src/Client/Assets/Scripts/UI/UIQuest/UIQuestDialog.cs`<br/>`Src/Client/Assets/Scripts/UI/UIQuest/UIQuestStatus.cs`<br/>`Src/Client/Assets/Scripts/Managers/QuestManager.cs`(暴露事件) |
| **受影响系统** | 客户端 Quest UI |
| **破坏现有功能** | 否(行为等价) |
| **风险点** | 中。UI 行为的微调要靠手动测试保证 |
| **可增量执行** | ✅ 是。建议先做 `UIQuestSystem`(列表刷新),再做 `UIQuestDialog`(接取 / 提交按钮) |
| **前置依赖** | A2(QuestState 暴露 OnQuestChanged) |
| **后续依赖** | 无 |

**具体改动示意**(本报告不修改代码,只描述):
```csharp
// 1. 新增 ViewModel
public class QuestListItem
{
    public int QuestId { get; }
    public string Name { get; }
    public QuestStatus Status { get; }
    public bool IsAvailable { get; }  // 等价于现在的 Info == null 逻辑
}

// 2. QuestManager 暴露事件
public event Action<Quest> OnQuestChanged;

// 3. UIQuestSystem.cs:54 改为订阅
QuestManager.Instance.OnQuestChanged += quest => Refresh();

// 而非在 OnEnable 里 foreach allQuests
```

---

## CAN BE DEFERRED(可以延后)

### B1. 抽象 `IQuestRepository`

| 项 | 内容 |
|---|---|
| **改动类型** | Repository Pattern 抽象 |
| **难度** | **Medium** |
| **预计工作量** | 3 小时 |
| **新增文件** | `Src/Server/GameServer/GameServer/Repositories/QuestRepository.cs`<br/>`Src/Server/GameServer/GameServer/Repositories/IQuestRepository.cs`(可选) |
| **修改文件** | `Src/Server/GameServer/GameServer/Managers/QuestManager.cs`<br/>`Src/Server/GameServer/GameServer/Models/Quest.cs`(可选) |
| **受影响系统** | 任务持久化路径 |
| **破坏现有功能** | 否 |
| **风险点** | 低。但**延后收益更高**:等出现第二个仓储需求(好友、公会、邮件)时再做,可以一次性抽出 `IRepository<T>`,而不是只做 `IQuestRepository` |
| **可增量执行** | ✅ 是。但 ROI 不高,建议推迟到开始写单元测试之前 |
| **前置依赖** | A1(先把 `SubmitQuest` 拆成多个方法,持久化才有"被包"的对象) |
| **后续依赖** | 无 |

---

### B2. 拆 `Character` 聚合根

| 项 | 内容 |
|---|---|
| **改动类型** | Aggregate 拆分 |
| **难度** | **High** |
| **预计工作量** | 1~2 天 |
| **新增文件** | 视方案而定(可能需要 `CharacterAggregates/` 文件夹) |
| **修改文件** | `Src/Server/GameServer/GameServer/Entities/Character.cs`<br/>所有 6 个 `*Manager.cs` 的构造逻辑 |
| **受影响系统** | 全局(任何 new Character 的地方都要改) |
| **破坏现有功能** | 高风险 |
| **风险点** | 高。这种"上帝类"拆分是经典高风险重构,容易引发"找不到当前是哪个 manager 的字段"这类 bug |
| **可增量执行** | ⚠️ 难。6 个 manager 都是 per-character 状态,物理上分开不一定带来清晰度 |
| **前置依赖** | 战斗系统接入后再做,届时会再多一个 manager,一起评估 |
| **后续依赖** | 无 |

**建议**:**推迟到战斗系统接入之后**,届时一起评估 `Character` 包含的所有 manager。

---

### B3. `QuestStatus` 状态机重写

| 项 | 内容 |
|---|---|
| **改动类型** | 枚举名修正 + 状态迁移表 |
| **难度** | **Medium** |
| **预计工作量** | 半天 |
| **修改文件** | `Src/Lib/proto/message.proto`(协议升级)<br/>`Src/Lib/Common/Data/QuestDefine.cs`<br/>所有引用 `QuestStatus` 的 C# 文件 |
| **受影响系统** | 客户端 + 服务器 + 协议 |
| **破坏现有功能** | **是**(拼写 `Complated` → `Completed` 影响 wire 协议) |
| **风险点** | 中。协议升级必须客户端 / 服务器同步发版 |
| **可增量执行** | ❌ 否。需要全局一致性 |
| **前置依赖** | 等"协议升级窗口"或"全局重构"时再做 |
| **后续依赖** | 无 |

**建议**:**推迟**。当前拼写错误不影响功能,等到必须做协议版本号区分时一并修。

---

### B4. 错误处理抽象(`IQuestErrorPresenter`)

| 项 | 内容 |
|---|---|
| **改动类型** | UI 错误展示抽象 |
| **难度** | **Low** |
| **预计工作量** | 1.5 小时 |
| **新增文件** | `Src/Client/Assets/Scripts/Services/IQuestErrorPresenter.cs`(可选) |
| **修改文件** | `Src/Client/Assets/Scripts/Services/QuestService.cs` |
| **受影响系统** | 仅客户端错误提示 |
| **破坏现有功能** | 否 |
| **风险点** | 低 |
| **可增量执行** | ✅ 是 |
| **前置依赖** | 等错误处理需求变复杂(例如要区分"背包满"/"等级不够"/"前置任务未完成"三种文案)再做 |
| **后续依赖** | 无 |

**建议**:**推迟**。目前 `MessageBox.Show` 够用。

---

### B5/B6. 接 `OnQuestStatusChanged` 事件(客户端)

| 项 | 内容 |
|---|---|
| **改动类型** | 事件接入 |
| **难度** | **Low** |
| **预计工作量** | 1 小时 |
| **修改文件** | `Src/Client/Assets/Scripts/Managers/QuestManager.cs`(暴露 `event Action<Quest> OnQuestStatusChanged`)<br/>`Src/Client/Assets/Scripts/UI/UIQuest/UIQuestSystem.cs`(订阅)<br/>`Src/Client/Assets/Scripts/UI/UIWordElement/UIWorldElementManager.cs`(接入 NPC 头顶图标) |
| **受影响系统** | 客户端 Quest UI + NPC UI |
| **破坏现有功能** | 否 |
| **风险点** | 极低。事件已存在,只是无订阅者 |
| **可增量执行** | ✅ 是。建议与 A2 同时做(A2 是服务器侧,这是客户端侧) |
| **前置依赖** | A2 |
| **后续依赖** | 无 |

**建议**:**顺带做**,无额外成本。

---

### B7. `QuestManager` 实现 `IPostResponser`

| 项 | 内容 |
|---|---|
| **改动类型** | RPC 同步推送 |
| **难度** | **Low** |
| **预计工作量** | 1.5 小时 |
| **修改文件** | `Src/Server/GameServer/GameServer/Managers/QuestManager.cs`<br/>`Src/Server/GameServer/GameServer/Entities/Character.cs`(加入 `PostResponse` 列表) |
| **受影响系统** | 服务器 RPC 推送 |
| **破坏现有功能** | 否 |
| **风险点** | 低。目前任务 RPC 只有 Accept / Submit,且响应里已带 NQuestInfo,"搭车"的实际收益不大 |
| **可增量执行** | ✅ 是 |
| **前置依赖** | 等任务 RPC 数量增加(任务列表刷新、进度推送)时再做 |
| **后续依赖** | 无 |

---

## NOT A PROBLEM AT CURRENT STAGE(不算问题)

### C1. 业务规则泄露到 UI(`Info == null` 判断)
- **难度**:**N/A**(不做)
- **理由**:原型期 UI 组件少,直接判断比 ViewModel 快读懂

### C2. Quest Manager 调 ItemManager
- **难度**:**N/A**(不拆)
- **理由**:原型期"显而易见的耦合"反而利于理解

### C3. 一次提交触发 4 次 `save()`
- **难度**:**N/A**(不优化)
- **理由**:性能气味,非 bug,原型期并发 = 1

### C4. `StatusNotify` 不包含任务状态
- **难度**:**N/A**(不改)
- **理由**:`StatusNotify` 设计用于高频小变更;任务状态变化频率低,单独 RPC 更清晰

### C5. `QuestListRequest` / `QuestAbandonRequest` 是死协议
- **难度**:**N/A**(不删)
- **理由**:预留给"将来按状态过滤"和"放弃任务",留着不报错

### C6. God-object `Character`
- **难度**:**N/A**(不拆)
- **理由**:见 B2。所有 manager 都是 per-character,物理上分开不带来清晰度

### C7. `UIWorldElementManager.AddNpcQuestStatus` 无人调用
- **难度**:**N/A**(会接入,不是重构)
- **理由**:这是 UI 层的接入任务(NPC 渲染时调),不是架构问题

---

## 工作量汇总

| 类别 | 项数 | 总工作量 |
|---|---|---|
| **MUST REFACTOR NOW** | 4 项 (A1~A4) | **~1.5 天** |
| **CAN BE DEFERRED**(建议顺带做) | 2 项 (B5, B6) | 1 小时 |
| **CAN BE DEFERRED**(建议延后) | 5 项 (B1, B2, B3, B4, B7) | 视未来需求 |
| **NOT A PROBLEM** | 7 项 | 0 |

**Phase 1 + Phase 2 + Phase 3 总工作量** = A1 + A2 + A3 + A4 + B5 + B6 = **~2 天**,
可以让 `QuestManager.SubmitQuest` 从 45 行缩到 ~15 行,
客户端 UI 不再 poll、`OnQuestStatusChanged` 不再是死事件。

**Phase 4(战斗系统接入)工作量** = ~半天,
但**前提是 Phase 1~3 已经完成**,否则战斗接入会被 `QuestManager` 的混乱逻辑拖慢。

---

## 增量执行清单

```
Phase 1 (清理职责) ─────────────────────────  1 天
  Step 1.1: 抽 RewardService                  1.5 h
  Step 1.2: 加 QuestCompleted 事件骨架         1.5 h
  Step 1.3: 客户端 Init 接受 character 参数    1   h
  Step 1.4: QuestFilter.CanAccept 静态化       0.5 h

  [测试] 跑一遍游戏,确认接取 / 提交仍可用 ──── 30 min

Phase 2 (接入 QuestLogic) ──────────────────  半天
  Step 2.1: 新建 QuestLogic 静态类             2   h
  Step 2.2: QuestState 仅调 QuestLogic 判断     1   h

  [测试] 离线单测 QuestLogic                   30 min

Phase 3 (客户端事件化) ──────────────────────  半天
  Step 3.1: QuestManager 暴露 OnQuestChanged    1   h
  Step 3.2: UIQuestSystem 订阅事件             1   h
  Step 3.3: UIWorldElementManager 接入 NPC 图标  1   h

  [测试] 完整的接取 → 提交 → NPC 图标刷新链路   30 min

Phase 4 (战斗系统接入) ──────────────────────  半天
  Step 4.1: 实现 GameplayEventBus              1   h
  Step 4.2: QuestLogic.Evaluate 加 MonsterKilled 分支  1 h
  Step 4.3: QuestState 构造时 Subscribe         30 min
  Step 4.4: 测试 Kill 任务完整跑通              2   h

Phase 5 (可选,延后) ────────────────────────  视未来需求
  Step 5.1: IQuestRepository (等出现第二个仓储)
  Step 5.2: Character 聚合根拆分 (等战斗稳定后)
  Step 5.3: 状态机重写 (等协议升级窗口)
```

---

## 给作者的"先做哪三件事"清单

如果你只能选 3 件事今天做:

1. **A1 抽 RewardService** —— 1.5 小时,立即让 `SubmitQuest` 短一半
2. **A2 加 QuestCompleted 事件骨架** —— 1.5 小时,为战斗系统接入埋点
3. **B5 接 `OnQuestStatusChanged`** —— 1 小时,客户端 UI 不再需要手动 poll

总耗时:**半天**。
完成后,`QuestManager` 仍然存在但职责清晰,UI 不再穿透存储层,事件链路为战斗系统接入准备好。

**不要做的**:B2 拆 `Character`、B3 改协议、引入第三方 EventBus 库。
