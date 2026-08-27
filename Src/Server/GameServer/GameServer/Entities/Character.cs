using Common.Data;
using GameServer.Core;
using GameServer.Manager;
using GameServer.Managers;
using GameServer.Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entities
{
    class Character : CharacterBase, IPostResponser
    {
        // ============ 【独立业务属性】纯领域字段 ============
        public CharacterClass Class { get; set; }
        public long GoldSnapshot { get; set; }
        public long ExpSnapshot { get; set; }

        // ============ 【网络快照缓存】构造期一次性写入，发包时读取（Equips/Bag/Guild 生命周期内不变） ============
        // 注：Equips 是 byte[]（数据库与协议均为 byte[]），不需要任何缓存或序列化，按需直接透传 cha.Equips 即可
        private NBagInfo _bag;
        private NGuildInfo _guild;

        // ============ 【保留】原业务属性（带 statusManager 业务逻辑，影子替换期不可删除） ============
        /// <summary>
        /// 金币属性
        /// </summary>
        public long Gold
        {
            get { return this.Data.Gold; }
            set
            {
                if (value == this.Data.Gold)
                    return;
                this.statusManager.AddGoldChange((int)(value - this.Data.Gold));
                this.Data.Gold = value;
                // 影子字段同步（避免发包时取过时数据）
                this.GoldSnapshot = value;
            }
        }
        /// <summary>
        /// 经验属性
        /// </summary>
        public long Exp
        {
            get { return this.Data.EXP; }
            set
            {
                if (Exp == value)
                    return;
                this.statusManager.AddExpChange((int)(value - this.Data.EXP));
                this.Data.EXP = value;
                // 影子字段同步
                this.ExpSnapshot = value;
            }
        }

        /// <summary>
        /// 数据库数据
        /// </summary>
        public TCharacter Data;
        /// <summary>
        /// 物品管理器
        /// </summary>
        public ItemManager ItemManager;
        /// <summary>
        /// 状态管理器
        /// </summary>
        public StatusManager statusManager;
        /// <summary>
        /// 任务管理器
        /// </summary>
        public QuestManager questManager;
        /// <summary>
        /// 好友管理器
        /// </summary>
        public FriendManager friendManager;
        /// <summary>
        /// 队伍
        /// </summary>
        public Team team;
        /// <summary>
        /// 队伍信息的最后同步时间
        /// </summary>
        public float teamSyncTime = 0f;
        /// <summary>
        /// 所属公会Id
        /// </summary>
        public int GuildId { get; set; } = 0;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cha"></param>
        public Character(CharacterType type, TCharacter cha) :
            base(new Core.Vector3Int(cha.MapPosX, cha.MapPosY, cha.MapPosZ), new Core.Vector3Int(100, 0, 0))
        {
            // 数据库数据(子类数据)
            this.Data = cha;

            // ============ 填充独立领域属性 ============
            this.Id = cha.ID;
            this.ConfigId = cha.ConfigId;
            this.Type = type;
            this.Class = (CharacterClass)cha.Class;
            this.MapId = cha.MapID;
            this.Level = 10;
            this.GoldSnapshot = cha.Gold;
            this.ExpSnapshot = cha.EXP;

            // 道具管理器初始化
            this.ItemManager = new ItemManager(this);
            // 任务管理器初始化
            this.questManager = new QuestManager(this);
            // 状态管理器初始化
            this.statusManager = new StatusManager(this);
            // 好友管理器初始化
            this.friendManager = new FriendManager(this);
            // 分配所属公会Id
            this.GuildId = GuildManager.Instance.GetGuildIdByCharacter(this.Id);

            // ============ 构造期初始化网络快照缓存（Bag/Guild 生命周期内不变） ============
            // Equips 字段：数据库侧 byte[] 与协议侧 byte[] 类型一致，发包时直接透传，无需缓存

            // 背包数据
            this._bag = new NBagInfo();
            this._bag.Items = this.Data.Bag.Items;
            this._bag.Unlocked = this.Data.Bag.Unlocked;

            // 公会数据
            if (this.GuildId > 0)
            {
                Guild guild = GuildManager.Instance.GetGuild(this.GuildId);
                this._guild = guild?.ToNGuildInfo();
            }
            else
            {
                this._guild = null;
            }

            // 配置数据
            this.Define = DataManager.Instance.Characters[this.ConfigId];

            // 同步 Name 到基类领域字段
            this.Name = cha.Name;
        }

        /// <summary>
        /// 后处理器
        /// </summary>
        /// <param name="response"></param>
        public void PostResponse(NetMessageResponse response)
        {
            // 状态系统后处理
            statusManager.PostResponse(response);
            // 好友系统后处理
            friendManager.PostResponse(response);
            // 组队系统后处理
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

        /// <summary>
        /// 将当前角色的所有字段打包为一个全新的 NCharacterInfo 网络快照并返回。
        /// - 实体级字段（EntityId/MapId/Entity）：由基类 ToCharacterInfo() 处理
        /// - 静态字段（Equips/Bag/Guild）：Equips 从数据库直接透传（byte[]）；Bag/Guild 从私有缓存读取
        /// - 动态字段（Items/Quests/Friends）：从 Manager 实时拉取（每次 new 新列表）
        /// - 基础业务字段（Id/Name/Class/Level/Gold/Exp）：从独立属性读取
        /// 每次调用都生成全新实例，保证发出去的是最新状态（纯工厂方法）。
        /// </summary>
        public override NCharacterInfo ToCharacterInfo()
        {
            // 第一棒：基类同步实体级字段（EntityId/MapId/Entity）
            NCharacterInfo info = base.ToCharacterInfo();

            // 第二棒：基础业务字段
            info.Id = this.Id;
            info.ConfigId = this.ConfigId;
            info.Name = this.Name;
            info.Type = this.Type;
            info.Class = this.Class;
            info.Level = this.Level;
            info.Gold = this.Gold;
            info.Exp = this.Exp;

            // 第三棒：静态缓存字段（构造期一次性写入，生命周期内不变）
            // Equips：byte[] 直接透传（数据库与协议类型一致，无需转换）
            info.Equips = this.Data.Equips;
            info.Bag = this._bag;
            info.Guild = this._guild;

            // 第四棒：动态字段（每次从 Manager 实时拉取，生成全新列表）
            this.ItemManager.GetItemInfos(info.Items);
            this.questManager.GetQuestInfo(info.Quests);
            this.friendManager.GetFriendInfo(info.Friends);

            return info;
        }
    }
}