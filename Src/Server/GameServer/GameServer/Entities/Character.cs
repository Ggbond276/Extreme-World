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
/* =============================================================================
 Character 终极全景属性字典 (拍扁后的伪代码，仅供编码参考) 
=============================================================================
class Character 
{
    // ---------------------------------------------------------
    // 第一层：爷爷类 (Entity) 传下来的【纯物理属性】
    // ---------------------------------------------------------

    public int entityId  get { return this.entityData.Id; } 
    

    public NEntity EntityData { get; set; } 
    
    public Vector3Int Position { get; set; } // 绝对坐标 (修改它会自动同步到 EntityData)
    public Vector3Int Direction { get; set; } // 朝向 (修改它会自动同步到 EntityData)
    public int Speed { get; set; }           // 速度 (修改它会自动同步到 EntityData)

    // ---------------------------------------------------------
    //  第二层：父类 (CharacterBase) 传下来的【身份与逻辑属性】
    // ---------------------------------------------------------
    public int Id { get; set; } 
    public NCharacterInfo Info; 
    public CharacterDefine Define; 

    // ---------------------------------------------------------
    //  第三层：本类 (Character) 独有的【业务与数据大管家】
    // ---------------------------------------------------------
    public TCharacter Data; 
    
    public ItemManager ItemManager;     // 道具/背包管理
    public StatusManager statusManager; // 状态(钱/经验)变动广播管理
    public QuestManager questManager;   // 任务管理

    public long Gold { get; set; } // set时会同步修改 Data.Gold 并触发 statusManager 广播
    public long Exp { get; set; }  // set时会同步修改 Data.EXP 并触发 statusManager 广播
}
=============================================================================
*/
namespace GameServer.Entities
{
    class Character : CharacterBase, IPostResponser
    {
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
            }
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
            if(team != null)
            {
                if(this.teamSyncTime < this.team.timestamp)
                {
                    response.teamInfo = new TeamInfoResponse();
                    response.teamInfo.Team = this.team.ToNTeamInfo();
                    this.teamSyncTime = this.team.timestamp;
                } 
               
            } else
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
        /// 构造方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cha"></param>
        public Character(CharacterType type,TCharacter cha): 
            base(new Core.Vector3Int(cha.MapPosX, cha.MapPosY, cha.MapPosZ), new Core.Vector3Int(100,0,0))
        {

            // 数据库数据(子类数据)
            this.Data = cha;
            // 道具管理器初始化
            this.ItemManager = new ItemManager(this);
            // 任务管理器初始化
            this.questManager = new QuestManager(this);
            // 状态管理器初始化
            this.statusManager = new StatusManager(this);
            // 好友管理器初始化
            this.friendManager = new FriendManager(this);


            // Id
            this.Id = cha.ID;

            // 网络协议数据
            this.Info = new NCharacterInfo();
            // 基础数据填充
            this.Info.Type = type;
            this.Info.Id = cha.ID;
            this.Info.ConfigId = cha.ConfigId;
            this.Info.Name = cha.Name;
            this.Info.Class = (CharacterClass)cha.Class;
            this.Info.mapId = cha.MapID;
            this.Info.Gold = cha.Gold;
            this.Info.Equips = cha.Equips;
            this.Info.Exp = cha.EXP;
            this.Info.Level = 10;
            this.Info.EntityId = this.entityId;
            this.Info.Entity = this.EntityData;
            // 背包数据填充
            this.Info.Bag = new NBagInfo();
            this.Info.Bag.Items = this.Data.Bag.Items;
            this.Info.Bag.Unlocked = this.Data.Bag.Unlocked;
            // 物品数据填充
            this.ItemManager.GetItemInfos(this.Info.Items);
            // 任务数据填充
            this.questManager.GetQuestInfo(this.Info.Quests);
            // 好友数据填充
            this.friendManager.GetFriendInfo(this.Info.Friends);
            // 配置数据填充
            this.Define = DataManager.Instance.Characters[this.Info.ConfigId];

           
        }

        
    }
}
