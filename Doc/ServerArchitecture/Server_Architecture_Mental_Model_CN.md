# 服务端架构心智模型（Mental Model）

> 项目：`Extreme-World`（Unity MMO）
> 本文档面向**实习面试**复习：目的是让你关掉代码编辑器后，仍能在脑中完整重建这套服务器的工作方式。
> 性质：**只读分析**。所有结论均基于实际代码；推测部分会明确标注。

---

## 标签约定

- **Confirmed from code** —— 有代码证据
- **Inferred** —— 合理推断，但作者意图无法证实
- **Cannot be confirmed** —— 当前代码无法证实

---

# 0. 一句话先抓住整个架构

> **客户端发一个 `NetMessage` 包 → 服务器的 `NetService` 收到 → `MessageDistributer` 按包内消息类型分发 → 对应的 `XxxService` 处理 → 从网络会话里取出 `Character` → 调到对应的 `Manager`（Quest / Item / Status / Friend 等） → `Manager` 修改内存里的业务数据（同时通过 `Data` 字段修改 `TCharacter` 等 ORM 实体）→ `DBService.save()` 把变更落到 SQL 数据库 → 拼出 `NetMessageResponse` → 通过同一个会话发回客户端 → 客户端 `QuestManager` 等接住响应 → 触发 `OnQuestStatusChanged` 事件 → UI 刷新。**

下面我们**逐层拆解**。

---

# 1. 比喻先于术语：把服务端想成一个"邮局"

| 你看到的代码 | 比喻成 |
|---|---|
| 整个服务器进程 | 一座**邮局大楼**，里面有很多工位 |
| `NetService` | **大门收发室**。所有信件进出都要从这过 |
| `NetSession` | **一个客户专属的窗口柜台**。每位玩家有自己独立的柜台 |
| `MessageDistributer` | **信件分拣机**。读信封面（消息类型），扔到对应处理员的桌子上 |
| `XxxService`（如 `QuestService`） | **专职工种处理员**。任务信 → 任务处理员；好友信 → 好友处理员 |
| `Character` | **这位玩家在邮局里的"临时档案袋"**。袋子里面装着他当前要处理的所有业务 |
| `Character.QuestManager` 等 | **档案袋里的小隔层**。任务格、物品格、好友格 |
| `Character.Data` | **隔层里挂着的"原始档案夹"**（数据库条目） |
| `TCharacter` / `TCharacterQuest` | **档案夹里的具体文件**。一张文件 = 一行数据库记录 |
| `DBService` | **档案室保管员**。所有 `T*` 文件都由他管，需要"存档"就找他 |
| `DataManager` | **公告板 / 字典架**。任务表、NPC 表、物品表等"游戏规则"放这里 |
| `protobuf message` | **标准信封**。所有信件必须套这个信封格式 |
| `NetMessage` / `NetMessageRequest` / `NetMessageResponse` | **大信封里的小信封分装**。一封信可能同时装请求信封、响应信封 |

⚠️ **重要：这是比喻，不是架构真理。** 后面每节我都会把比喻和真实代码对照一遍。

---

# 2. Character vs TCharacter：这是你最大的困惑，必须讲透

> 真实代码位置：
> - `Src/Server/GameServer/GameServer/Entities/Character.cs:16, 55-79`
> - `Src/Server/GameServer/GameServer/TCharacter.cs:15-46`

## 2.1 一句话区分

> **TCharacter = 数据库里的玩家档案（一行 SQL 记录）**
> **Character = 这位玩家当前在线的"运行时实体"**

## 2.2 真实代码长什么样

### TCharacter（数据库那边）

```csharp
// TCharacter.cs（由 EF6 自动生成,partial class）
public partial class TCharacter
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int Class { get; set; }
    public long Gold { get; set; }      // 金币存在这里
    public long EXP { get; set; }       // 经验存在这里
    public int Level { get; set; }
    // ... 装备、坐标、所属 Player ...

    // 关联表：每个玩家有多个任务、多个物品、多个好友
    public virtual ICollection<TCharacterQuest> Quests { get; set; }
    public virtual ICollection<TCharacterItem> Items { get; set; }
    public virtual ICollection<TCharacterFriend> Friends { get; set; }
    public virtual TCharacterBag Bag { get; set; }
}
```

> **专业术语**：TCharacter 是 **ORM Entity**（Entity Framework 6 映射的实体），对应数据库里 `TCharacter` 这张表。

### Character（运行时这边）

```csharp
// Character.cs
public class Character : CharacterBase, IPostResponser
{
    public TCharacter Data;                              // ← 关键:指向数据库档案的引用
    public CharacterClass Class { get; set; }            // 运行时独有的职业类型
    internal StatusManager statusManager;                // ← 状态管理器
    internal ItemManager ItemManager;                    // ← 物品管理器
    internal QuestManager questManager;                  // ← 任务管理器
    internal FriendManager friendManager;                // ← 好友管理器
    internal Team team;                                  // ← 当前队伍(可空)
}
```

> **专业术语**：Character 是 **Runtime Domain Entity**（运行时领域实体）。

## 2.3 谁创建 Character？

> 真实代码：`Src/Server/GameServer/GameServer/Managers/CharacterManager.cs:43-53`

```csharp
// 玩家点"进入游戏"那一刻触发
public Character AddCharacter(TCharacter cha)        // TCharacter 已经在数据库里躺着
{
    Character character = new Character(CharacterType.Player, cha);
    EntityManager.Instance.AddEntity(cha.MapID, character);  // 分配运行时 EntityId
    this.Characters[character.entityId] = character;          // 加入"在线大管家"
    return character;
}
```

## 2.4 关键字段：`Character.Data`

```csharp
// Character.cs:55-79 构造方法
public Character(CharacterType type, TCharacter cha) : base(...)
{
    this.Data = cha;           // ← 就是这一行,把数据库档案塞进运行时对象
    this.Class = (CharacterClass)cha.Class;

    this.statusManager = new StatusManager(this);   // 注意:Manager 拿到的是 Character,不是 Data
    this.ItemManager = new ItemManager(this);
    this.questManager = new QuestManager(this);
    this.friendManager = new FriendManager(this);
}
```

**这意味着什么**：

1. `Character.Data` 是一个**引用**（指针），不是拷贝。
2. 你 `Character.Data.Gold = 100`，改的**就是那个 TCharacter 实例**，也就是 ORM 跟踪的那一行记录。
3. Manager 拿到的是 `Character`（而不是 `TCharacter`），所以它们能改 `Character.Data.Gold`、`Character.ItemManager.AddItem(...)`。

## 2.5 Gold / Exp 的 setter：完整理解这段代码

```csharp
// Character.cs:31-52
public long Gold
{
    get { return this.Data.Gold; }                       // 读 → 直接问 Data
    set
    {
        if (value == this.Data.Gold) return;             // 没变就跳过
        this.statusManager.AddGoldChange((int)(value - this.Data.Gold));  // 1. 通知状态管理器
        this.Data.Gold = value;                          // 2. 改数据库实体
        this.GoldSnapshot = value;                       // 3. 同步影子字段
    }
}
```

**逐行解释**：

| 行 | 通俗解释 | 专业术语 |
|---|---|---|
| `get { return this.Data.Gold; }` | 我没有自己的金币字段，金币存在 Data 里，我只是转一下 | **委托属性 / Delegate Property** |
| `statusManager.AddGoldChange(...)` | 有金币变化了！告诉状态管理员："金币 +X"，它会记下来，等会儿回信时告诉客户端 | **事件缓冲 / Status Event Buffering** |
| `this.Data.Gold = value` | 真正改的是 TCharacter（数据库那一行） | **ORM Property Setter** |
| `GoldSnapshot = value` | 维护一个"上一次发包时的快照"，避免回包时把同样的金币反复推送给客户端 | **Snapshot / Shadow State** |

**结论**：
- 修改金币 → 修改的是 **TCharacter.Gold**（数据库实体）
- Gold setter 还会顺手通知 statusManager，让它在客户端回包里塞一条 "金币 +X"
- Gold 还没真的写到 SQL 数据库里 —— 必须等到 `DBService.Instance.save()` 才真正落盘

## 2.6 为什么不能直接把 TCharacter 当 Character 用？

> 这是你提的问题："为什么不能所有东西都存在 Character 里？"

**真实代码告诉你答案**：

1. **数据库表是关联表**：TCharacter 持有 `ICollection<TCharacterQuest>`、`ICollection<TCharacterItem>`。如果 Character 直接持有这些集合，你就**绕开了 ORM**。EF6 不再知道哪些行被改了、新增了、删除了。
2. **运行时需要业务逻辑**：Character 需要 `QuestManager.Quests` 这种**有方法、有行为**的字典，不是单纯的行集合。Manager 包一层才方便做"接取任务时检查是否已接"、"添加道具时检查背包空位"这种事。
3. **生命周期不同**：TCharacter 是 EF6 上下文（DbContext）跟踪的对象，**有作用域**。Character 是服务器进程内的"在线"对象，**没有作用域**。两者必须分开管理。

> **专业术语总结**：
> - **Runtime Domain Entity**（运行时领域实体）= `Character` —— 进程级生命周期，带业务行为
> - **Persistence / ORM Entity**（持久化实体）= `TCharacter` —— DbContext 作用域，纯粹数据

---

# 3. 双结构心智模型：DATABASE 侧 vs RUNTIME 侧

```
┌────────────────────────────────────────────────────────────────┐
│                  DATABASE SIDE (持久化侧)                       │
│                                                                │
│  TUser (账号)                                                  │
│   └── TPlayer (一个账号下的所有角色集合)                        │
│        └── TCharacter (单个角色的所有持久化字段)                │
│             ├── TCharacterBag  (背包配置:已解锁格子数等)        │
│             ├── TCharacterItem[]  (背包里每一格的物品记录)     │
│             ├── TCharacterQuest[] (每个已接任务的记录行)       │
│             ├── TCharacterFriend[]                              │
│             ├── Equips (装备字节数组)                           │
│             └── Gold, EXP, Level, MapID, MapPosX/Y/Z ...        │
└────────────────────────────────────────────────────────────────┘
                              │
                              │ (登录时由 EF6 反序列化出来)
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                  RUNTIME SIDE (运行时侧)                        │
│                                                                │
│  NetSession (网络会话)                                         │
│    └── TUser (账号引用)                                        │
│    └── Character (玩家当前在线的运行时实体)                     │
│         ├── Data → TCharacter (引用同一个档案)                 │
│         ├── statusManager (金币/经验/道具 增量缓冲)            │
│         ├── ItemManager (运行时物品字典 Dictionary)             │
│         │     └── Item 运行时包装 → DbItem → Data.Items       │
│         ├── questManager (运行时任务字典 Dictionary)            │
│         │     └── Quest 运行时包装 → DbQuest → Data.Quests    │
│         ├── friendManager (运行时好友字典)                      │
│         └── team (可空,当前队伍)                               │
└────────────────────────────────────────────────────────────────┘
```

## 3.1 字段归宿表

| 字段 | 在哪里 | 原因 |
|---|---|---|
| 玩家 ID (ID/ConfigId/Name/Level) | TCharacter 唯一 | 这是数据库主键相关字段，必须持久化 |
| Gold / EXP | TCharacter 唯一 | Gold setter 通过 `Data.Gold = value` 写入数据库；Character 自己不存金币 |
| MapID / MapPosX/Y/Z | TCharacter 唯一 | 玩家退出时需要保存位置 |
| Equips (装备字节) | TCharacter 唯一 | 装备需要持久化 |
| Bag (背包容量配置) | TCharacterBag 唯一 | 容量是持久化数据 |
| Items (背包物品) | TCharacterItem[] + ItemManager 字典 双重存在 | 数据库存"实际数据",运行时字典做 O(1) 查找 |
| Quests (任务) | TCharacterQuest[] + QuestManager 字典 双重存在 | 同上 |
| Friends (好友) | TCharacterFriend[] + FriendManager 字典 双重存在 | 同上 |
| GoldSnapshot / ExpSnapshot | Character 独有 | 影子字段,只在运行时有效,不入库 |
| Class (职业类型) | 双重存在 (TCharacter.Class 是 int,Character.Class 是 CharacterClass 枚举) | 运行时需要枚举语义,数据库存整数 |

---

# 4. 登录流程：完整的"数据库 → 运行时"过程

> 真实代码位置：
> - `Services/UserService.cs:49-90`（OnLogin）
> - `Services/UserService.cs:186-218`（OnGameEnter）
> - `Managers/CharacterManager.cs:43-53`（AddCharacter）
> - `Entities/Character.cs:55-79`（Character 构造）

## 4.1 流程图（带真实代码）

```
[客户端] UserLoginRequest(User, Password)
        ↓
[服务端] UserService.OnLogin 接收
        ↓
[EF6] DBService.Instance.Entities.Users
        .Where(u => u.Username == request.User)
        .FirstOrDefault()
        → 得到 TUser (含 TPlayer, 含 TCharacter[])
        ↓
[Server] 把 TPlayer.Characters 列表转成 NCharacterInfo 列表
        ↓
[Server] sender.Session.User = user;   // 把账号挂到会话上
        ↓
[客户端] 收到 UserLoginResponse,看到角色列表,选一个角色进入游戏
        ↓
[客户端] UserGameEnterRequest(characterIdx=0)
        ↓
[服务端] UserService.OnGameEnter 接收
        ↓
[Server] dbchar = Session.User.Player.Characters.ElementAt(characterIdx)   // TCharacter
        ↓
[Server] character = CharacterManager.Instance.AddCharacter(dbchar)
        │
        ├─→ new Character(Player, dbchar)         // 构造运行时实体
        │       │
        │       ├─ base(...) 初始化 Entity 物理属性(Id, Name, ConfigId, Level, MapId)
        │       │
        │       ├─ this.Data = dbchar             // ★ 数据库档案绑定
        │       │
        │       ├─ new StatusManager(this)        // 各业务 Manager
        │       ├─ new ItemManager(this)          //   内部都会遍历 Data.Quests/Items/Friends
        │       ├─ new QuestManager(this)         //   把自己填充好
        │       ├─ new FriendManager(this)
        │       │
        │       └─ this._bag = new NBagInfo() { Items = Data.Bag.Items, Unlocked = ... }
        │
        ├─→ EntityManager.Instance.AddEntity(dbchar.MapID, character)
        │       └─ character.entityId = ++idx;   // ★ 运行时唯一 ID(与 DB ID 不同!)
        │
        └─→ this.Characters[character.entityId] = character;   // 加入"在线大管家"
        ↓
[Server] character.ToCharacterBaseInfo()           // 拼出 NCharacterInfo
        ↓
[Server] sender.Session.Response.gameEnter.Character = NCharacterInfo
        ↓
[Server] sender.Session.Character = character;     // ★ 把 Character 挂到会话上
        ↓
[Server] MapManager.Instance[dbchar.MapID].CharacterEnter(sender, character)
        │   └─ MapCharacters[character.entityId] = new MapCharacter(conn, character)
        │
        └─ 广播 MapCharacterEnterResponse (该地图所有玩家 + 怪物列表)
        ↓
[Server] SessionManager.Instance.AddSession(character.Id, sender)
        │   └─ Sessions[character.Id] = sender   // 给好友系统查找用
        ↓
[Server] GuildManager.Instance.NotifyOnlineStatus(character.Data.ID, true)  // 通知公会好友
        ↓
[Server] sender.SendResponse()
        ↓
[客户端] UserGameEnterResponse(NCharacterInfo)
        └─ 客户端 UserService 触发角色初始化 → 客户端 QuestManager / ItemManager 初始化
```

## 4.2 每个步骤回答 5 个问题

| 步骤 | 哪个对象存在 | 从哪来 | 谁创建 | 数据流向 | 为什么需要这步 |
|---|---|---|---|---|---|
| TUser 反序列化 | TUser | SQL Server | EF6 DbContext | DB → 内存 | 没有这一步,服务器不知道是谁登录 |
| Session.User = user | TUser 引用 | 反序列化产物 | 赋值 | 内存 → NetSession | 让后续请求能通过 Session 找到是谁 |
| dbchar = ElementAt(idx) | TCharacter | TPlayer.Characters | LINQ 取 | 内存 | 玩家选角 |
| CharacterManager.AddCharacter | Character | 刚刚构造 | new | TCharacter → Character | 把"档案"升级成"在线实体" |
| 构造 Character(...) | Character + Managers | this | 构造方法 | TCharacter → Character | 业务就绪 |
| AddEntity 分配 entityId | character.entityId | 计数器 | EntityManager | 1, 2, 3... | 运行时唯一标识,跨地图广播用 |
| CharacterManager.Characters[id]=c | Character | 内存 | Dictionary | 内存索引 | 别人能 O(1) 查到你 |
| ToCharacterBaseInfo | NCharacterInfo | Character 拼装 | Character 内部 | Character → NCharacterInfo | 网络传输必须用 protobuf 类型 |
| sender.Session.Character = c | Character 引用 | 内存 | 赋值 | Character → Session | 后续这条线的请求都能拿到角色 |
| MapManager.CharacterEnter | MapCharacter | 内存 | Map | 内存索引 | 加入地图,才能被同地图玩家看到 |
| SessionManager.AddSession | NetConnection | sender | 字典 | 内存索引 | 好友系统反向找你 |
| SendResponse | byte[] | protobuf 序列化 | NetConnection | 内存 → socket | 客户端要看到自己进来了 |

---

# 5. 一个完整的任务接取流程（从 UI 到 SQL）

> 真实代码：
> - 客户端：`Src/Client/Assets/Scripts/UI/UIQuest/UIQuestDialog.cs`（按下接取按钮）
> - 客户端：`Src/Client/Assets/Scripts/Services/QuestService.cs`
> - 网络：`message.proto:470-477`
> - 服务端：`Services/QuestService.cs:35-64`
> - 服务端：`Managers/QuestManager.cs:56-82`

## 5.1 客户端发起

```
[UI] 玩家点击 "接取任务"
        ↓
[UIQuestDialog.cs] 拿到 quest.Info,做基础判断
        ↓
[客户端 QuestManager] QuestManager.Instance.AcceptQuest(quest)
        │
        ├─ 如果当前已登录角色满足等级/职业要求 → 调
        │
        └─ QuestService.Instance.SendQuestAccept(questId)
                │
                └─ 通过 NetClient 把 QuestAcceptRequest{ quest_id = X } 发出去
```

## 5.2 网络传输

```
[protobuf] QuestAcceptRequest { int32 quest_id }
        ↓ (protobuf 序列化)
[byte[] / socket]
        ↓ (TCP)
[Server NetService.DataReceived]
        ↓ 收完一个完整包就触发 PackageHandler.ReceiveData
```

## 5.3 服务端接收

```
[PackageHandler] 把字节流反序列化成 NetMessage
        ↓
[NetMessage.Request.questAccept != null]
        ↓
[MessageDistributer] 查表发现订阅者 → QuestService.OnQuestAccept
        ↓
[QuestService.OnQuestAccept(sender, request)]
        │
        ├─ Character character = sender.Session.Character;  ← 从会话拿角色
        ├─ int questId = request.QuestId;                    ← 解析任务 ID
        │
        ├─ Result result = character.questManager.AcceptQuest(questId, out Quest quest)
        │       │
        │       └─ [QuestManager.AcceptQuest]            详见 5.4
        │
        ├─ sender.Session.Response.questAccept = new QuestAcceptResponse();
        │
        ├─ if (result == Success && quest != null)
        │       response.questAccept.Quest = quest.ToNQuestInfo();
        │
        ├─ sender.Session.Response.questAccept.Result = result;
        │
        └─ sender.SendResponse();    ← 序列化整个 NetMessage → byte[] → socket
```

## 5.4 QuestManager.AcceptQuest 真实代码逐行

> `Managers/QuestManager.cs:56-82`

```csharp
public Result AcceptQuest(int questId, out Quest quest)
{
    quest = null;

    // 校验 1:内存里有没有这个任务了(防外挂重复接取)
    if (this.Quests.ContainsKey(questId))
        return Result.Failed;

    // 校验 2:任务 ID 是否在配表里(防外挂伪造 ID)
    if (!DataManager.Instance.Quests.ContainsKey(questId))
        return Result.Failed;

    // 数据库插入:让 EF6 在内存里准备一个新行
    TCharacterQuest dbQuest = DBService.Instance.Entities.CharacterQuests.Create();
    dbQuest.TCharacterID = this.Owner.Data.ID;   // 外键 = 玩家 ID
    dbQuest.QuestID = questId;

    // 包装成运行时实体
    quest = new Quest(dbQuest);
    quest.InitQuetsDefine();                      // 根据 Target1 类型决定初始 Status

    // 三处同步更新(内存、Manager、ORM)
    this.Quests.Add(quest.QuestId, quest);        // ← Manager 字典
    this.Owner.Data.Quests.Add(dbQuest);          // ← EF6 跟踪列表

    DBService.Instance.save();                    // ← 真的写库
    return Result.Success;
}
```

| 行 | 通俗解释 |
|---|---|
| `this.Quests.ContainsKey(questId)` | 我内存里已经有这个任务了,不能再接 |
| `DataManager.Instance.Quests.ContainsKey` | 这个任务在配表里吗?不在就一定是外挂 |
| `DBService.Instance.Entities.CharacterQuests.Create()` | 让 EF6 给我准备一行新记录,但还没存到数据库 |
| `dbQuest.TCharacterID = Owner.Data.ID` | 给这一行加上"属于哪个玩家"的外键 |
| `new Quest(dbQuest)` | 用这一行包装成一个有方法、能做事的运行时实体 |
| `quest.InitQuetsDefine()` | 如果是"无目标"任务,直接置为 Completed;否则 InProgress |
| `this.Quests.Add(...)` | 更新 Manager 内存字典,后面 GetQuestInfo 会从这里取 |
| `this.Owner.Data.Quests.Add(dbQuest)` | 把 EF6 那行也挂进关联表,save 才会真的写 |
| `DBService.Instance.save()` | EF6 → SQL Server,执行 INSERT |

## 5.5 服务端发回响应

```
[QuestAcceptResponse] { Result, Errormsg, Quest: NQuestInfo{...} }
        ↓
[NetSession.Response] 被塞到这辆"大卡车"里
        ↓
[NetSession.GetResponse()]
        │
        ├─ 如果有 Character.PostResponse,先执行(状态变更广播)
        │
        └─ PackageHandler.PackMessage(response) → byte[]
        ↓
[NetConnection.SendResponse] → socket.Send
        ↓
[Client] NetClient 收到 → 反序列化成 NetMessage
        ↓
[Client QuestService.OnQuestAccept]
        │
        └─ 触发 QuestManager.OnQuestAccepted(info) 之类的事件
                ↓
            UI 订阅了 OnQuestStatusChanged → 自动刷新
```

## 5.6 客户端接到响应后做什么

> **Inferred**（客户端 QuestService 实现细节需要看客户端代码确认）
> 推测路径：
> - 客户端 `QuestService` 收到响应 → 调用 `QuestManager` 的某个方法更新本地 `allQuests`
> - 触发 `OnQuestStatusChanged` 事件
> - 订阅了事件的 `UIQuestSystem`、`UIQuestDialog` 等 UI 自动刷新

---

# 6. 网络层：逐个名词讲清楚

> 真实代码：
> - `Network/NetService.cs`
> - `Network/NetConnection.cs`
> - `Network/NetSession.cs`
> - `Network/INetSession.cs`
> - `Network/IPostResponser.cs`
> - `Lib/Common/Network/MessageDistributer.cs`
> - `Lib/proto/message.proto`

## 6.1 网络层先有比喻

```
protobuf  =  标准化快递单 (字段名+类型确定,所有人填同一张单)
network   =  快递运输过程 (TCP,字节流,跨网线)
NetMessage=  最大的包裹箱,里面塞了一个 Request 包裹,可能也塞了 Response 包裹
Request   =  客户端寄给服务器的"我想要干这个"
Response  =  服务器回给客户端的"结果是这样"
NetService=  邮局收发室,所有快递从这里过
NetConnection = 一个具体客户的连接,带 socket
NetSession= 一个客户在邮局的"档案窗口",记着他登录了谁、选了哪个角色
MessageDistributer = 信件分拣机,根据信封类型扔到对应处理员
XxxService= 专职工种处理员,只处理自己订阅的那类信
```

## 6.2 真实代码对照

### protobuf message

> `Src/Lib/proto/message.proto:194-197`
```protobuf
message NetMessage{
    NetMessageRequest Request = 1;
    NetMessageResponse Response = 2;
}
```
> 这是**最大的包裹**。一次网络交互就是来回一个 NetMessage。
> **专业术语**：protobuf contract / 序列化协议。

### NetMessageRequest

> `Src/Lib/proto/message.proto:199-243`
```protobuf
message NetMessageRequest{
    UserLoginRequest userLogin = 2;
    UserGameEnterRequest gameEnter = 4;
    ItemBuyRequest itemBuy = 10;
    QuestAcceptRequest questAccept = 13;
    ...
}
```
> 一个巨大的"任选一"联合体。客户端发包时,实际只填一个字段,其他字段为 null。
> 这就是为什么 `MessageDistributer` 可以根据包内消息类型分发。

### QuestAcceptRequest / QuestSubmitRequest

> `Src/Lib/proto/message.proto:470-489`
```protobuf
message QuestAcceptRequest {
    int32 quest_id = 1;
}
message QuestAcceptResponse {
    RESULT result = 1;
    string errormsg = 2;
    NQuestInfo quest = 3;
}
```
> 一个只有 `quest_id` 的小型 Request。
> Response 除了结果码和错误消息,还带一个 `NQuestInfo`(服务端"主动塞"的当前任务状态)。

### NetService

> `Src/Server/GameServer/GameServer/Network/NetService.cs:18-23`
```csharp
public bool Init(int port)
{
    ServerListener = new TcpSocketListener("127.0.0.1", Settings.Default.ServerPort, 10);
    ServerListener.SocketConnected += OnSocketConnected;
    return true;
}
```
> 邮局大门。启动 TCP 监听,接受客户端 socket 连接。

### NetConnection<NetSession>

> `Src/Server/GameServer/GameServer/Network/NetConnection.cs:40-99`
```csharp
public class NetConnection<T> where T : INetSession
{
    public PackageHandler<NetConnection<T>> packageHandler;
    private T session;          // 每个连接自带一个 Session
    public void SendResponse()  // 把 Session.GetResponse() 包成 byte[] 发出去
    public void SendData(byte[] data, ...)   // 底层 socket 发送
}
```
> **一个客户的连接**。带 socket 和一个 Session。
> **专业术语**：Network Connection / Channel

### NetSession

> `Src/Server/GameServer/GameServer/Network/NetSession.cs:14-104`
```csharp
class NetSession : INetSession
{
    public TUser User { get; set; }              // 当前登录的账号
    public Character Character { get; set; }     // 当前在线的角色
    public NEntity Entity { get; set; }

    public NetMessageResponse Response            // 大卡车:本轮响应要发的所有内容
    {
        get { ... return response.Response; }
    }

    public byte[] GetResponse()                  // 关键方法:发包前最后组装
    {
        if (this.Character != null)
            Character.PostResponse(Response);    // 1. 让 Character 追加增量广播
        byte[] data = PackageHandler.PackMessage(response);
        response = null;
        return data;
    }
}
```
> **客户的"档案窗口"**。所有这个客户的请求/响应都经过这里。
> **专业术语**：Session / Server-side Session

### MessageDistributer

> `Src/Lib/Common/Network/MessageDistributer.cs:42-63`
```csharp
public class MessageDistributer<T> : Singleton<MessageDistributer<T>>
{
    private Dictionary<string, System.Delegate> messageHandlers;
    public delegate void MessageHandler<Tm>(T sender, Tm message);
    // Subscribe<T>(handler) → messageHandlers[typeof(T).Name] = handler
    // 分发时按 NetMessage 内的实际子消息类型查表 → 调用 handler
}
```
> **信件分拣机**。所有 Service 在自己的构造函数里 `Subscribe<XXXRequest>(this.OnXxx)`,把自己登记进去。
> 客户端发来一个 `NetMessage`,分拣机只看里面的 `Request` 字段,比如 `questAccept != null`,就找出所有订阅了 `QuestAcceptRequest` 的处理员,挨个调用。
> **专业术语**：Message Dispatcher / RPC Router

### IPostResponser

> `Src/Server/GameServer/GameServer/Network/IPostResponser.cs`
```csharp
interface IPostResponser {
    void PostResponse(NetMessageResponse response);
}
```
> 一个**"包发出去之前,你要不要再塞点东西进去"的钩子**。
> 比如 StatusManager 实现了它,就可以在金币变化后,自动把 "金币 +X" 塞到回包里,客户端就能收到这条增量更新。

## 6.3 网络层调用顺序

```
Client  →  socket.send(NetMessage{Request.questAccept=...})  →
                                                                  
Server  TCP listener                                             
NetService.OnSocketConnected → 创建 NetConnection + NetSession  
socket.ReceiveAsync 完成 → ReceivedCompleted → DataReceived      
PackageHandler.ReceiveData  → 反序列化成 NetMessage              
MessageDistributer.分发 → QuestService.OnQuestAccept              
QuestService.OnQuestAccept → character.questManager.AcceptQuest  
[业务处理 + DB]                                                   
QuestService → 填 Response.questAccept                           
NetSession.GetResponse → Character.PostResponse → 增量塞入        
PackageHandler.PackMessage → byte[]                              
NetConnection.SendData → socket.BeginSend                         
                                                                  
Client  ←  socket receive → NetClient 反序列化成 NetMessage      
         → QuestService 处理 → UI 更新                           
```

---

# 7. Service / Manager / Model / Entity / DBService —— 这些名词在项目里到底指什么

> **不要把它们想成"教科书 DDD 概念"。** 在这个项目里,它们的实际含义是：

## 7.1 Service

> **网络入口层**,只做一件事：**接 RPC + 转发给 Manager**。

实际样子：
- `QuestService.cs:18-24` 构造方法里 `Subscribe<QuestAcceptRequest>(this.OnQuestAccept)`,把自己注册到分拣机
- `OnQuestAccept` 方法里:
  1. 从 `sender.Session.Character` 取出角色
  2. 调用 `character.questManager.AcceptQuest(questId, ...)`
  3. 把 `result` 和 `quest.ToNQuestInfo()` 填到 `Response.questAccept`
  4. `sender.SendResponse()`

**特点**：
- 几乎都是 `Singleton<XXXService>`
- 每个 Service 对应一类网络消息(Quest 类 / Friend 类 / Item 类)
- Service **不直接读写数据库**,不直接修改金币 / 经验 / 道具
- Service 的方法名通常是 `OnXxxRequest(sender, request)`,签名固定

**看到 Service 时,脑子里应该想**：
> "这是接 RPC 的,接下来它会把请求转给 Manager,我不应该在这里写业务规则"

## 7.2 Manager

> **业务逻辑层**,**每个角色一份**(per-character),挂在 Character 上。

实际样子：
- `QuestManager.cs:18-21`
  ```csharp
  public Character Owner;                           // 我属于哪个玩家
  public Dictionary<int, Quest> Quests = new ...;  // 我的内存数据
  ```
- `QuestManager.AcceptQuest(questId, ...)` 内部做：
  - 校验
  - 调用 `DBService.Instance.Entities.CharacterQuests.Create()`
  - 修改 `Owner.Data.Quests`(EF6 跟踪)
  - 调 `DBService.Instance.save()`

**特点**：
- 不是单例,每个 `Character` 各有自己的 `QuestManager`、`ItemManager`、`StatusManager`、`FriendManager`
- Manager **可以读写数据库**(`DBService.Instance.save()`)
- Manager **可以修改其他 Manager 的宿主字段**(例如 QuestManager 调 `Owner.ItemManager.AddItem(...)`)
- Manager **负责组装回包**(调 `quest.ToNQuestInfo()` 等)

**看到 Manager 时,脑子里应该想**：
> "这是某个玩家的业务逻辑,业务规则都在这里,这里会改数据库"

## 7.3 Model

> **运行时包装** —— 把数据库的一行包装成"有方法、能做事"的对象。

实际样子：
- `Models/Quest.cs:12-30`
  ```csharp
  class Quest
  {
      public TCharacterQuest DbQuest { get; private set; }   // 我包的就是这行数据库
      public QuestDefine Define { get; private set; }        // 我也指向配表
      public bool CanSubmit => IsCompleted && !IsSubmit;     // 我有业务方法
  }
  ```
- `Models/Item.cs`、`Models/Friend.cs`、`Models/Team.cs` 同理

**特点**：
- 不直接写数据库
- 提供"业务便利方法"(`CanSubmit`、`Add`、`Remove` 等)
- 把数据库字段映射成"更易用的属性"
- **通过 DbXxx 字段**反向影响数据库

**看到 Model 时,脑子里应该想**：
> "这是某个具体数据(一个任务/一件物品/一个好友)的运行时外壳,业务方法都在这"

## 7.4 Entity

> **服务器进程里的所有"活物"的基类**(玩家、怪物、NPC 都继承它)。

实际样子：
- `Entities/Entity.cs:9-39`
  ```csharp
  public class Entity
  {
      public int entityId;
      private Vector3Int position, direction;
      private int speed;
      public Vector3Int ToNEntity() { ... }   // 转成 protobuf
  }
  ```
- `Entities/CharacterBase : Entity` 加了身份字段(Id/Name/Level/MapId/...)
- `Entities/Character : CharacterBase` 又加了业务 Manager 容器
- `Entities/Monster : CharacterBase` 是怪物的轻量版(没 Manager)

**特点**：
- Entity 是**纯物理 + 身份**,不带业务
- 继承链:`Entity → CharacterBase → Character/Monster`
- 唯一全局 ID 字段是 `entityId`(运行时由 EntityManager 分配,与数据库 ID 不同)

**看到 Entity 时,脑子里应该想**：
> "这是个能在世界里走来走去的东西。玩家、怪物、NPC 都继承它"

## 7.5 DBService

> **EF6 DbContext 的唯一封装**。所有 ORM 操作都从这里走。

实际样子：
- `Services/DBService.cs:11-34`
  ```csharp
  class DBService : Singleton<DBService>
  {
      ExtremeWorldEntities entities;
      public ExtremeWorldEntities Entities { ... }   // 直接暴露 EF6 上下文
      public void save(bool async = false)           // 唯一封装:SaveChanges
  }
  ```

**特点**：
- 单例
- 内部就一个 `ExtremeWorldEntities`(EF6 DbContext)
- `Entities.Characters`、`Entities.CharacterQuests` 等是 EF6 自动生成的 `DbSet<T>`
- 所有 `DBService.Instance.save()` 调用最终都跑 `entities.SaveChanges()`

**看到 DBService 时,脑子里应该想**：
> "这是数据库的唯一接口,要保存就调 save()"

## 7.6 DataManager

> **静态配置表**(只读、加载一次)。

实际样子：
- `Managers/DataManager.cs:14-77`
  ```csharp
  class DataManager : Singleton<DataManager>
  {
      public Dictionary<int, MapDefine> Maps;
      public Dictionary<int, CharacterDefine> Characters;
      public Dictionary<int, NpcDefine> NPCs;
      public Dictionary<int, ItemDefine> Items;
      public Dictionary<int, QuestDefine> Quests;     // ← 任务配表
      ...
      public void Load() {                            // 从 JSON 文件读
          this.Quests = JsonConvert.DeserializeObject<...>(File.ReadAllText("Data/QuestDefine.txt"));
      }
  }
  ```

**特点**：
- 单例
- 启动时从 `Data/*.txt`(JSON 格式)加载
- **只读**,运行时不会改
- 提供 `Quests[questId]`、`NPCs[npcId]` 这种 `O(1)` 查询

**看到 DataManager 时,脑子里应该想**：
> "这是游戏规则的字典。任务叫什么名字、NPC 在哪、物品多少钱,都在这查"

---

# 7.7 名词对照表(终极版)

| 名词 | 真实含义 | 项目里的具体类 | 通俗比喻 |
|---|---|---|---|
| **Service** | 网络入口,接 RPC 转给 Manager | QuestService, ItemService, ... | 邮局专职工种 |
| **Manager** | 业务逻辑,每个角色一份 | QuestManager, ItemManager, ... | 业务办事员 |
| **Model** | 运行时包装,把一行数据变成可操作对象 | Quest, Item, Friend, Team | 货物包装盒 |
| **Entity** | 活物基类,只管物理和身份 | Entity, CharacterBase, Character, Monster | 邮局里所有会动的东西 |
| **DBService** | EF6 DbContext 唯一封装 | DBService | 档案室唯一窗口 |
| **DataManager** | 静态配置表(JSON 加载) | DataManager | 公告板/字典架 |
| **Txxx** | EF6 自动生成的 ORM 实体(partial class) | TUser, TPlayer, TCharacter, TCharacterQuest, ... | 数据库里的"原始档案" |
| **Nxxx** | protobuf 网络类型 | NCharacterInfo, NQuestInfo, NFriendInfo | 标准化快递单 |
| **Singleton<T>** | 全局单例基类 | 所有 Service 和部分 Manager | 邮局的"全局唯一"工种 |

---

# 8. 为什么 Character 要有 6 个 Manager

> 真实代码：`Entities/Character.cs:20-26`

```csharp
internal StatusManager statusManager;
internal ItemManager ItemManager;
internal QuestManager questManager;
internal FriendManager friendManager;
internal Team team;
public int GuildId { get; set; }
```

## 8.1 通俗解释

**你登录了 A 玩家和 B 玩家,他们两个各有自己的任务列表和背包。** 显然不能把 A 的任务放到 B 的管理器里。所以每个 Character 都"自带"一套 Manager。

## 8.2 为什么不是全局单例

如果 `QuestManager` 是单例:
- 你要管理所有玩家的任务 → 数据结构是 `Dictionary<intPlayerId, Dictionary<intQuestId, Quest>>`
- 每次操作都要先查 playerId
- 玩家下线时要把他的子树清掉
- 容易内存泄漏

每个 Character 一份 Manager 的好处:
- `Owner.Data.ID` 一拿,玩家身份就明确了
- 玩家下线 → `Character` 整个被 GC → 所有 Manager 一起释放
- 操作简单:`questManager.Quests.Add(...)` 不需要传 playerId

## 8.3 每个 Manager 负责什么

| Manager | 拥有的数据 | 提供的方法 |
|---|---|---|
| **statusManager** | `List<NStatus> Status`(增量缓冲区) | `AddGoldChange / AddExpChange / AddItemChange / PostResponse` |
| **ItemManager** | `Dictionary<int, Item> Items` | `AddItem / RemoveItem / UseItem / GetItemInfos` |
| **questManager** | `Dictionary<int, Quest> Quests` | `AcceptQuest / SubmitQuest / GetQuestInfo` |
| **friendManager** | `Dictionary<int, Friend> friends` | `AddFriend / RemoveFriend / NotifyOnlineStatus / GetFriendInfo / PostResponse` |
| **Team** | `List<Character> Members` | `AddMember / RemoveMember / ToNTeamInfo / GetMember` |

## 8.4 `new QuestManager(this)` 是什么意思?

> `Entities/Character.cs:72`

```csharp
this.questManager = new QuestManager(this);
```

> 构造方法签名: `QuestManager(Character owner)` (`Managers/QuestManager.cs:27`)

**通俗**:"给你配一个'任务专员',他专管你这一位的所有任务"。

**专业术语**：**Composition（组合）**。Character **Has-A** QuestManager,而非 **Is-A**。

---

# 9. 数据库持久化：实际发生了什么

> 真实代码：`Services/DBService.cs:26-32`

```csharp
public void save(bool async = false)
{
    if (async)
        entities.SaveChangesAsync();
    else
        entities.SaveChanges();
}
```

## 9.1 通俗解释

```
修改 Character.Data.Gold (内存里)
        │
        │   (这步不会立刻写库!)
        ▼
DBService.Instance.save()  ← 你手动调
        │
        ▼
EF6 扫描所有 DbSet,对比内存和数据库
        │
        ▼
生成 SQL: UPDATE TCharacter SET Gold = X WHERE ID = Y
        │
        ▼
通过连接字符串发给 SQL Server
        │
        ▼
SQL Server 写入,返回受影响行数
```

## 9.2 关键点

### 修改内存 ≠ 修改数据库

你写 `Character.Data.Gold = 100`,改的**只是 EF6 内存里跟踪的那一行**。SQL 数据库里还是旧值。

### save() 之前崩溃会怎样?

**Confirmed from code**:当前代码每个事务点都会调 `DBService.Instance.save()`(例如 `QuestManager.AcceptQuest:80`、`ItemManager.AddItem:91`、`FriendManager.AddFriend:141`)。如果服务器在 `Character.Data.Gold = 100` 之后、`save()` 之前崩溃,**金币就丢了**(内存里的数据没了,数据库里还是旧值)。

> **Inferred**:当前代码没有事务封装,也没有"批量 save"机制。每个 `save()` 都会立即落盘。这是性能气味(见之前的设计审查报告),但不在本心智模型文档的讨论范围。

### 内存和数据库为啥都要?

| 维度 | 内存 | 数据库 |
|---|---|---|
| 访问速度 | 纳秒级 | 毫秒级 |
| 容量 | 受进程内存限制 | GB 级 |
| 持久性 | 进程死就丢 | 永远不丢(理论上) |
| 一致性 | 内存里看到的就是最新的 | 必须 save 才会同步 |

所以:**所有高频操作都在内存做**,只在关键节点(save)同步一次到数据库。

---

# 10. DataManager vs DBService —— 你的另一个困惑

> 这两者**完全不同**,但名字相近,容易混淆。

## 10.1 一句话区分

> **DataManager = 只读的游戏规则(从 JSON 文件来,启动时加载一次)**
> **DBService = 可读可写的玩家数据(从 SQL Server 来,运行时不断改)**

## 10.2 例子对比

```
游戏里一共有多少种任务?
  → DataManager.Instance.Quests.Count  ← DataManager(配表)
  → 这是策划在 QuestDefine.txt 里配的

A 玩家当前接了哪些任务?
  → character.questManager.Quests    ← DBService(数据库)
  → 这是 A 玩家在 SQL Server 的 TCharacterQuest 表里的行

游戏里金币道具 1 号多少钱?
  → DataManager.Instance.Items[1].Price   ← DataManager(配表)

A 玩家身上有多少金币?
  → character.Gold   ← → character.Data.Gold (DB)
```

## 10.3 真实代码对照

> `DataManager.cs:14-77` vs `DBService.cs:11-34`

| 维度 | DataManager | DBService |
|---|---|---|
| 继承 | `Singleton<DataManager>` | `Singleton<DBService>` |
| 数据来源 | `Data/*.txt`(JSON) | SQL Server |
| 加载时机 | 服务器启动时 `Load()` | 每次需要时 LINQ 查询 |
| 是否可变 | **只读** | 可读可写 |
| 内容 | 任务、NPC、地图、装备、商店等"规则" | 用户、玩家、角色、好友、物品等"实例" |
| 字段类型 | `QuestDefine`,`ItemDefine`,`NpcDefine`... | `TUser`,`TCharacter`,`TCharacterQuest`... |

## 10.4 类比

> **DataManager = 字典(规则手册)**:告诉你"任务 1 叫'打败史莱姆',奖励 100 金币"
> **DBService = 账本(玩家档案)**:告诉你"玩家 A 接了任务 1,进度 3/5"

---

# 11. 三种数据类型的对比表

| 类型 | 例子 | 住在哪里 | 谁创建 | 用途 | 运行时可变 |
|---|---|---|---|---|---|
| **静态配置** | `QuestDefine`, `ItemDefine`, `NpcDefine` | `DataManager` 单例,启动时从 JSON 加载 | DataManager.Load() | 游戏规则/数值/文案 | ❌ |
| **持久化实体** | `TUser`, `TCharacter`, `TCharacterQuest`, `TCharacterItem`, `TCharacterBag`, `TCharacterFriend` | EF6 DbContext,数据库镜像 | EF6 反序列化 / Create() | 玩家数据存档 | ✅ (通过 save) |
| **运行时实体** | `Character`, `Quest`, `Item`, `Friend`, `Team`, `Entity`, `Monster`, `StatusManager`, `QuestManager` | 服务器进程内存 | 服务器代码 new | 运行时业务处理 | ✅ 频繁 |
| **网络传输** | `NUserInfo`, `NCharacterInfo`, `NQuestInfo`, `NItemInfo`, `NFriendInfo`, `NTeamInfo` | 临时对象,序列化后变 byte[] | 每次传输时 new | 跨网线传输 | ❌(用完即丢) |
| **网络大包裹** | `NetMessage`, `NetMessageRequest`, `NetMessageResponse` | 同上 | 每次 RPC 时 new | 一来一回的整封信 | ❌ |

---

# 12. 真理来源(Source of Truth)清单

> "如果两个地方都有这个数据,我应该信哪个?"

| 数据 | 真理来源 | 备注 |
|---|---|---|
| **玩家账号密码** | SQL Server `TUser` 表 | 不在内存里 |
| **玩家当前选中的角色** | 服务器内存 `Session.Character` | 每次请求从会话拿 |
| **玩家金币** | `TCharacter.Gold`(由 `Character.Gold` 转发) | 改 setter 时同步 |
| **玩家任务进度** | `TCharacterQuest.Target1/2/3` | 由 `Quest.DbQuest` 包装 |
| **玩家背包物品** | `TCharacterItem.ItemID/Count` + `byte[] Equips` | 装备是字节数组,物品是行 |
| **任务配置/奖励** | `QuestDefine`(从 JSON 来) | **唯一来源** |
| **NPC 位置/对话** | `NpcDefine`(从 JSON 来) | **唯一来源** |
| **任务运行时进度变更** | `statusManager.Status`(增量缓冲)+ `Character.GoldSnapshot`(快照) | 发包前最后合并 |
| **好友在线状态** | `Friend.isOnline`(运行时字典) | 不入库,仅运行时 |
| **队伍组成** | `Team.Members`(运行时)+ 各成员的 `Character.team` | 不入库 |

---

# 13. 完整的服务端架构地图(Mermaid)

详见同目录 `Server_Architecture_Map.mmd`。

---

# 14. 登录时序图(Mermaid)

详见同目录 `Login_Sequence.mmd`。

---

# 15. 接取任务时序图(Mermaid)

详见同目录 `Quest_Accept_Sequence.mmd`。

---

# 16. "如果面试官问我服务端是怎么工作的"——直接能说的口语版

> 关掉编辑器,看着下面这段话,能背出来就够了。

---

**回答示例(可直接背诵)**：

"我们这个项目用的是典型的 MMO 架构,分客户端和服务端。

**玩家登录的过程**是这样的:客户端发一个登录包,服务器用 EF6 从 SQL Server 里把账号查出来,得到 `TUser`,里面挂着 `TPlayer`,`TPlayer` 里有该账号所有 `TCharacter` 记录。玩家从列表里选一个角色'进入游戏'后,服务器把那条 `TCharacter` 反序列化出来,用 `CharacterManager.AddCharacter` 构造一个 `Character`(运行时的实体),把 `TCharacter` 挂到 `Character.Data` 这个引用上,然后实例化各个 per-character 的 Manager:`QuestManager`、`ItemManager`、`StatusManager`、`FriendManager` 等,这些 Manager 都会遍历 `Data.Quests / Data.Items / Data.Friends` 来填充自己的运行时字典。

**网络层**用 TCP + protobuf。客户端每次发一个 `NetMessage`,里面有一个 `Request` 联合体,只填一个字段(比如 `questAccept`)。服务器 `MessageDistributer` 根据这个字段类型,把消息分发给对应的 Service(`QuestService.OnQuestAccept`)。Service **不写业务**,只做三件事:从 `sender.Session.Character` 取出角色,调用 `character.questManager.AcceptQuest(...)`,然后把结果填到 `Response` 里,最后 `sender.SendResponse()`。

**Manager 层**是真正的业务逻辑。每个 Manager 都属于一个具体 `Character`(所以不是单例),内部维护一个运行时字典,同时通过 `Character.Data`(引用同一个 `TCharacter`)反向影响 ORM,调用 `DBService.Instance.save()` 才会真正落库。

**关于 `TCharacter` 和 `Character` 的区别**:`TCharacter` 是 EF6 自动生成的 ORM 实体,对应数据库里的一行;`Character` 是这台服务器上代表'这个玩家当前在线'的运行时实体。`Character.Data` 就是它对 `TCharacter` 的引用。修改金币走 `Character.Gold` 的 setter,内部就是 `Data.Gold = value`,顺便通知 `StatusManager` 记一笔增量,等回包的时候通过 `IPostResponser` 钩子塞进 `StatusNotify`。

**配置数据和玩家数据是两套东西**:`DataManager` 是从 `Data/*.txt` 的 JSON 文件加载的静态规则表,只读,启动时一次性加载;`DBService` 是 EF6 的 DbContext 包装,负责玩家数据的读写。任务接取的合法性校验既要看 `DataManager.Quests[questId]`(任务是否存在),也要看 `character.questManager.Quests`(是否已经接了)。

**简而言之**:网络层负责传信,Service 层负责接信,Manager 层负责办事,DataManager 是字典,DBService 是账本,`Character` 是把玩家档案 + 运行时业务粘合在一起的容器。"

---

# 17. 服务器面试高频问题 → 项目中的答案

> 每次面试前,把这一节过一遍,基本能覆盖 80% 的高频问题。

## Q1:Character 和 TCharacter 有什么区别?

| 项目 | 内容 |
|---|---|
| **通俗理解** | Character 是"现在站在游戏里的玩家",TCharacter 是"他家里档案柜里的档案袋"。Character.Data 就是"档案袋上的提手"。 |
| **专业术语** | Character = Runtime Domain Entity / 运行时领域实体;TCharacter = ORM Entity / Persistence Entity / 持久化实体 |
| **项目代码** | `Entities/Character.cs:16` `public TCharacter Data;`<br/>`TCharacter.cs:15-46` partial class TCharacter |
| **面试回答** | "TCharacter 是 EF6 映射的数据库行,字段是 int、long 这种简单类型;Character 是服务器进程内的运行时实体,持有业务 Manager、影子字段、运行时状态。两者通过 `Character.Data` 这个引用关联,改 setter 时既影响内存也影响 ORM 跟踪,但只有 `DBService.Instance.save()` 才会真正写入 SQL。" |

## Q2:为什么 Character 要持有 TCharacter?

| 项目 | 内容 |
|---|---|
| **通俗理解** | 因为 Character 操作的所有'持久化字段'(金币、等级、坐标)都需要最终写回数据库。如果 Character 自己存一份,就要写两遍代码,还要担心不一致。 |
| **专业术语** | Aggregate Root Pattern / 聚合根引用 Persistence Entity |
| **项目代码** | `Character.cs:65` `this.Data = cha;` |
| **面试回答** | "Character 的运行时数据(比如 Manager 的字典)和持久化字段(比如 Gold、Level)需要互相操作。如果把 Gold 存在 Character 上,改完还要写一行 `Data.Gold = value`,容易漏。让 Character.Data 直接指向 TCharacter,改 setter 时直接改 Data.Gold 即可,ORM 自动跟踪。" |

## Q3:QuestManager 和 TCharacterQuest 有什么区别?

| 项目 | 内容 |
|---|---|
| **通俗理解** | TCharacterQuest 是数据库里的一行(纯数据);QuestManager 是负责"管理我所有任务"的专员,内部用 `Dictionary<int, Quest>` 索引。Quest 又是把 TCharacterQuest 包成有方法的对象(比如 `CanSubmit`、`ToNQuestInfo`)。 |
| **专业术语** | Manager = Per-character Business Component;Model = Runtime Wrapper |
| **项目代码** | `Managers/QuestManager.cs:21` `public Dictionary<int, Quest> Quests = new();`<br/>`Models/Quest.cs:14` `public TCharacterQuest DbQuest { get; private set; }` |
| **面试回答** | "TCharacterQuest 是 EF6 实体,字段就 `Id / QuestID / Target1/2/3 / Status / TCharacterID`;QuestManager 是每个玩家角色各一份的业务管理器,内部维护 `Dictionary<int, Quest>`;Quest 是把 TCharacterQuest 包装成有方法的对象,提供 `CanSubmit`、`ToNQuestInfo` 等便利方法。三层关系:Manager 管所有 Quest,Quest 包一个 DbQuest,DbQuest 反向影响 TCharacter.Quests。" |

## Q4:Service 和 Manager 的区别是什么?

| 项目 | 内容 |
|---|---|
| **通俗理解** | Service 是"接电话的人",Manager 是"办业务的人"。Service 接到电话,转给 Manager,Manager 干完活再回给 Service,Service 把结果告诉客户端。 |
| **专业术语** | Service = RPC Entry Point / Application Service Layer;Manager = Domain Logic / Business Component |
| **项目代码** | `Services/QuestService.cs:18-24` Subscribe 注册 RPC<br/>`Managers/QuestManager.cs:56-82` AcceptQuest 实现业务 |
| **面试回答** | "Service 是单例,负责从 MessageDistributer 接 RPC 消息,把请求转给对应的 per-character Manager,再把 Manager 的返回值塞到 Response 里发回去。Manager 是每个角色一份,负责具体业务逻辑:校验、改数据库、调 save()。Service 不写业务、不直接读写数据库。" |

## Q5:protobuf 是干什么的?

| 项目 | 内容 |
|---|---|
| **通俗理解** | "标准化快递单"。两端都要按同一张单子填,字段名、字段类型都定好。 |
| **专业术语** | protobuf = Protocol Buffers / 序列化协议 |
| **项目代码** | `Src/Lib/proto/message.proto` 定义所有消息;`Src/Lib/Protocol/message.cs` 生成 C# 类 |
| **面试回答** | "protobuf 是 Google 的二进制序列化协议,比 JSON 体积小、解析快。我们项目用 protobuf-net 生成 C# 类,把消息定义在 message.proto 里,客户端服务器共用。每次发包就是把一个 NetMessage 序列化成 byte[],收包再反序列化。" |

## Q6:MessageDistributer 是干什么的?

| 项目 | 内容 |
|---|---|
| **通俗理解** | "信件分拣机"。信来了,看一眼信封(NetMessage.Request.questAccept != null),扔到对应处理员的桌上。 |
| **专业术语** | RPC Router / Message Dispatcher |
| **项目代码** | `Lib/Common/Network/MessageDistributer.cs:42-63`<br/>`Services/QuestService.cs:22` Subscribe |
| **面试回答** | "MessageDistributer 是一个泛型单例,内部维护一个 `Dictionary<string, Delegate>`,Service 在构造方法里 Subscribe<某种 Request>(处理方法),把自己注册进去。分发时按 NetMessage 实际类型查表调用。我们用的是 `MessageDistributer<NetConnection<NetSession>>` 这个泛型实例。" |

## Q7:DBService 是干什么的?

| 项目 | 内容 |
|---|---|
| **通俗理解** | "档案室唯一窗口"。你要保存、要查,都从他那里走。 |
| **专业术语** | EF6 DbContext Wrapper |
| **项目代码** | `Services/DBService.cs:11-32` |
| **面试回答** | "DBService 是 Entity Framework DbContext 的唯一封装,内部就一个 `ExtremeWorldEntities`。提供 `Entities.Characters` 等 DbSet 访问,以及 `save()`(SaveChanges)。所有 Manager 修改 `Owner.Data` 上的字段后,必须调 `DBService.Instance.save()` 才会真正写入 SQL Server。" |

## Q8:为什么 Character 下面有多个 Manager?

| 项目 | 内容 |
|---|---|
| **通俗理解** | 因为一个玩家身上有多种'东西':任务、物品、好友、状态……每样都派一个专员管理,比一个 God-object 干净。 |
| **专业术语** | Composition Over Inheritance / Per-Character Domain Components |
| **项目代码** | `Character.cs:20-26` 6 个 Manager 字段 |
| **面试回答** | "每个 Character 自己持有一组 Manager,而不是全局单例。这样 A 玩家和 B 玩家的数据天然隔离,不用传 playerId;玩家下线整个 Character 被回收,Manager 也一起被 GC。Manager 通过 `this.Owner` 反向引用回 Character,需要时拿 `Owner.Data` 写数据库。" |

## Q9:什么是运行时数据?

| 项目 | 内容 |
|---|---|
| **通俗理解** | "玩家此刻在游戏里的状态"。进程死了就没了。 |
| **专业术语** | Runtime State / In-Memory State |
| **项目代码** | `Character.GoldSnapshot`, `Friend.isOnline`, `Character.team` 都是运行时独有 |
| **面试回答** | "运行时数据是只在内存中存在、不会入库的状态。比如金币快照(GoldSnapshot)用于增量广播对比、好友在线状态(Friend.isOnline)、当前队伍(Character.team)。这些数据进程重启就丢,但能避免每次都查数据库。" |

## Q10:什么是持久化?

| 项目 | 内容 |
|---|---|
| **通俗理解** | "把内存里的东西存到硬盘上"。服务器关了也不丢。 |
| **专业术语** | Persistence / ORM / SaveChanges |
| **项目代码** | `Services/DBService.cs:26-32` `save()` |
| **面试回答** | "持久化是把运行时数据写入 SQL Server 的过程。我们用 EF6,所有 `TCharacter`、`TCharacterQuest` 等都是 ORM 实体,改完字段后调 `DBService.Instance.save()` 才会真正写入。`SaveChanges()` 内部会对比内存和数据库,生成 INSERT/UPDATE/DELETE 语句。" |

## Q11:玩家登录以后服务器发生了什么?

| 项目 | 内容 |
|---|---|
| **通俗理解** | 查账号 → 查角色 → 选一个 → 把数据'组装成在线状态' → 加入地图 → 发回客户端。 |
| **专业术语** | Login Flow / Session Binding / Character Materialization |
| **项目代码** | `Services/UserService.cs:49-218` OnLogin + OnGameEnter |
| **面试回答** | "客户端发 UserLoginRequest,服务器查 EF6 拿到 TUser,返回角色列表。玩家选角发 UserGameEnterRequest,服务器根据 characterIdx 取 TCharacter,调 `CharacterManager.AddCharacter(TCharacter)` 构造运行时 Character,把 TCharacter 挂到 Character.Data 上,实例化 4 个 Manager 填充运行时字典,分配运行时 entityId,加入 EntityManager 和 CharacterManager,然后 `sender.Session.Character = character`,最后 MapManager.CharacterEnter 加入地图,SessionManager 登记会话,SendResponse 发回客户端。" |

## Q12:玩家接取任务以后发生了什么?

| 项目 | 内容 |
|---|---|
| **通俗理解** | UI 调客户端 QuestService → 发包 → 服务器 QuestService 收到 → 调 QuestManager → 创建数据库行 → 保存 → 回包。 |
| **专业术语** | RPC Request / Domain Operation / ORM Insert |
| **项目代码** | `Services/QuestService.cs:35-64`<br/>`Managers/QuestManager.cs:56-82` |
| **面试回答** | "UI 调客户端 `QuestManager.AcceptQuest`,客户端 `QuestService.SendQuestAccept(questId)` 把 QuestAcceptRequest 发出去。服务器 `MessageDistributer` 把它分给 `QuestService.OnQuestAccept`,Service 调 `character.questManager.AcceptQuest(questId)`,Manager 做两步校验(防重 + 配表存在性),用 EF6 创建一行 TCharacterQuest,挂到 Owner.Data.Quests,内存字典也加,最后 `DBService.Instance.save()` 写入 SQL。Service 把 quest 转成 NQuestInfo 塞进 Response,SendResponse 发回客户端。客户端收到后触发 UI 刷新。" |

## Q13:修改 Gold 的时候到底修改了什么?

| 项目 | 内容 |
|---|---|
| **通俗理解** | 改的其实是 TCharacter.Gold(数据库那一行),但还没写库。同时通知 statusManager 记一笔增量,等回包时塞进 StatusNotify。 |
| **专业术语** | ORM Property Setter + Status Event Buffering |
| **项目代码** | `Entities/Character.cs:31-41` |
| **面试回答** | "`Character.Gold` 没有自己的存储字段,get/set 全部委托给 `this.Data.Gold`(即 TCharacter.Gold)。Setter 做了三件事:值变了才往下走;通知 statusManager 加一条 `AddGoldChange`;同步更新 GoldSnapshot 影子字段。所以改 Gold 就是改 ORM 跟踪的实体,save() 时才会写库。" |

## Q14:数据库里的数据什么时候真正发生变化?

| 项目 | 内容 |
|---|---|
| **通俗理解** | 调用 `DBService.Instance.save()` 那一刻。不是赋值那一刻。 |
| **专业术语** | EF6 UnitOfWork / SaveChanges |
| **项目代码** | `Services/DBService.cs:26-32` |
| **面试回答** | "内存里的修改是 lazy 的,只有 `DBService.Instance.save()`(内部调 `entities.SaveChanges()`)才会让 EF6 把内存状态对比生成 SQL,发给 SQL Server 执行。每个 Manager 在自己的'业务闭环'末尾调一次 save。我们的代码没有事务封装,save 之间崩溃就会丢数据。" |

## Q15:为什么不能所有东西都直接存在 Character 里?

| 项目 | 内容 |
|---|---|
| **通俗理解** | 因为有些东西必须落库(等级、坐标),有些东西必须能跨玩家共享(任务配表),不能混在一起。 |
| **专业术语** | Separation of Concerns / Persistence vs Runtime |
| **项目代码** | `TCharacter.cs`(持久化) vs `Character.cs`(运行时) |
| **面试回答** | "如果所有东西都存在 Character 上,就有三个问题:第一,持久化字段没有 ORM 自动跟踪,save 时不知道哪些字段改了;第二,跨玩家的共享数据(比如任务配表)没法共享,每个玩家内存里都要存一份;第三,Character 生命周期由会话决定,持久化数据生命周期由数据库决定,混在一起 GC 时容易出错。所以 ORM 实体负责持久化,运行时实体负责业务,两者通过 Data 引用关联。" |

---

# 18. 一致性自检(Consistency Check)

| 检查项 | 答案 |
|---|---|
| Character → TCharacter 关系对得上代码? | ✅ `Character.Data` 字段引用 TCharacter,见 `Character.cs:16,65` |
| Character → Manager 关系对得上代码? | ✅ 构造方法里 `new StatusManager(this)` 等,见 `Character.cs:70-73` |
| Manager → Data 关系对得上代码? | ✅ `QuestManager.Owner = owner; owner.Data.Quests`,见 `QuestManager.cs:30,78` |
| Service → Manager 关系对得上代码? | ✅ `QuestService` 调 `character.questManager.AcceptQuest`,见 `QuestService.cs:47` |
| Network → Service 关系对得上代码? | ✅ `MessageDistributer.Subscribe<QuestAcceptRequest>(OnQuestAccept)`,见 `QuestService.cs:22` |
| TCharacter → TCharacterQuest 关系对得上代码? | ✅ `TCharacter.Quests` 是 `ICollection<TCharacterQuest>`,见 `TCharacter.cs:43` |
| protobuf → Service 关系对得上代码? | ✅ `message.proto:215` `QuestAcceptRequest questAccept = 13` 与 QuestService.OnQuestAccept 订阅 |
| DBService → EF6 关系对得上代码? | ✅ `entities = new ExtremeWorldEntities()`,见 `DBService.cs:22` |
| 运行时/数据库/配置/网络 四类数据分得清? | ✅ 见本文第 11 节表格 |

---

# 19. 文档使用建议

1. **面试前 1 小时**,只读 #16 和 #17 那两节,反复说几遍。
2. **面试前 30 分钟**,只看 #2 和 #12 两节(最常被问的两个问题)。
3. **面试被追问细节时**,翻 #6 网络层、#8 Manager、#13 架构地图,直接看 Mermaid 图。
4. **不要背代码**,背心智模型。代码细节现场可以推。

