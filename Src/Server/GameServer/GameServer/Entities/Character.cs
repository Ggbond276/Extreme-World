using Common.Data;
using GameServer.Core;
using GameServer.Manager;
using GameServer.Managers;
using GameServer.Models;
using Network;
using SkillBridge.Message;

namespace GameServer.Entities
{
    /// <summary>
    /// 玩家角色实体 — 业务最复杂的活跃实体。叠加业务大管家（背包/任务/好友等），处理数据库反序列化与状态广播。
    /// </summary>
    public class Character : CharacterBase, IPostResponser
    {
        public TCharacter Data;                                  // 数据库实体引用 — 所有持久化字段的真理来源（来源：数据库 TCharacter）
        public CharacterClass Class { get; set; }                // 职业类型（来源：构造期从数据库 TCharacter.Class 读取）
        public long GoldSnapshot { get; set; }                   // 金币快照（影子字段，避免发包时读到陈旧值；来源：Gold setter 同步）
        public long ExpSnapshot { get; set; }                    // 经验快照（影子字段；来源：Exp setter 同步）
        internal StatusManager statusManager;                   // 状态管理器（记录金币/经验变动并触发广播）
        internal ItemManager ItemManager;                       // 物品管理器（自维护 Items 字典）
        private NBagInfo _bag;                                  // 背包容器
        internal QuestManager questManager;                     // 任务管理器（自维护 Quests 字典）
        internal FriendManager friendManager;                   // 好友管理器（自维护 friends 字典）
        internal Team team;                                     // 当前队伍（运行时动态挂载；无队伍时为 null）
        public int GuildId { get; set; } = 0;                    // 所属公会 ID（运行时动态变化）
        public float teamSyncTime = 0f;                         // 队伍信息最后同步时间戳（用于 PostResponse 节流）


        // ======================== 业务属性 ========================
        public long Gold                                        // 金币（来源：Data.Gold；set 时同步影子 + 触发 statusManager 广播）
        {
            get { return this.Data.Gold; }
            set
            {
                if (value == this.Data.Gold) return;
                this.statusManager.AddGoldChange((int)(value - this.Data.Gold));
                this.Data.Gold = value;
                this.GoldSnapshot = value;
            }
        }
        public long Exp                                         // 经验（来源：Data.EXP；set 时同步影子 + 触发 statusManager 广播）
        {
            get { return this.Data.EXP; }
            set
            {
                if (Exp == value) return;
                this.statusManager.AddExpChange((int)(value - this.Data.EXP));
                this.Data.EXP = value;
                this.ExpSnapshot = value;
            }
        }

        // ======================== 构造与初始化 ========================
        public Character(CharacterType type, TCharacter cha) : base(
            id: cha.ID,
            name: cha.Name,
            type: type,
            configId: cha.ConfigId,
            level: cha.Level,
            mapId: cha.MapID,
            pos: new Core.Vector3Int(cha.MapPosX, cha.MapPosY, cha.MapPosZ),
            dir: new Core.Vector3Int(100, 0, 0))
        {
            this.Data = cha;
            this.Class = (CharacterClass)cha.Class;             // 本类独有字段：职业（数据库 TCharacter.Class）
            this.GoldSnapshot = cha.Gold;
            this.ExpSnapshot = cha.EXP;
            this.GuildId = GuildManager.Instance.GetGuildIdByCharacter(this.Id);
            this.statusManager = new StatusManager(this);       // 业务 Manager 容器（发包时实时拉取）
            this.ItemManager = new ItemManager(this);
            this.questManager = new QuestManager(this);
            this.friendManager = new FriendManager(this);

            // 静态网络快照缓存（构造期一次性写入，生命周期内不变）
            this._bag = new NBagInfo();
            this._bag.Items = this.Data.Bag.Items;
            this._bag.Unlocked = this.Data.Bag.Unlocked;
        }

        // ======================== 网络数据映射 ========================
        public override NCharacterInfo ToCharacterBaseInfo()            // 纯工厂：仅补全本类独有业务字段（父类身份字段已由 base.ToCharacterInfo() 完成）
        {
            NCharacterInfo info = base.ToCharacterBaseInfo();
            info.Class = this.Class;
            info.Gold = this.Gold;
            info.Exp = this.Exp;
            info.Equips = this.Data.Equips;
            info.Bag = this._bag;
            info.Guild = (this.GuildId > 0) ? GuildManager.Instance.GetGuild(this.GuildId)?.ToNGuildInfo() : null;
            this.ItemManager.GetItemInfos(info.Items);
            this.questManager.GetQuestInfo(info.Quests);
            this.friendManager.GetFriendInfo(info.Friends);
            return info;
        }

        // ======================== 响应后处理 ========================
        public void PostResponse(NetMessageResponse response)      // 在主响应组装完成后追加增量广播（来源：各 Manager 缓冲的事件队列）
        {
            statusManager.PostResponse(response);
            friendManager.PostResponse(response);

            if (team != null)
            {
                if (this.teamSyncTime < this.team.timestamp)
                {
                    response.teamInfo = new TeamInfoResponse();
                    response.teamInfo.Team = this.team.ToNTeamInfo();
                    this.teamSyncTime = this.team.timestamp;
                }
            }
            else
            {
                if (this.teamSyncTime > 0)
                {
                    response.teamInfo = new TeamInfoResponse();
                    response.teamInfo.Team = null;
                    this.teamSyncTime = 0f;
                }
            }
        }
    }
}
