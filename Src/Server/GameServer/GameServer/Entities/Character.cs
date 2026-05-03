using Common.Data;
using GameServer.Core;
using GameServer.Manager;
using GameServer.Managers;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entities
{
    class Character : CharacterBase
    {
       // 数据库中拉取的数据是存放有背包相关的数据的
        public TCharacter Data;
        public ItemManager ItemManager;
        public StatusManager statusManager;
        public QuestManager questManager;

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


        //构造方法
        public Character(CharacterType type,TCharacter cha): 
            base(new Core.Vector3Int(cha.MapPosX, cha.MapPosY, cha.MapPosZ), new Core.Vector3Int(100,0,0))
        {
            // 数据库数据
            this.Data = cha;
            // 网络协议数据
            this.Info = new NCharacterInfo();
            this.Info.Type = type;
            this.Info.Id = cha.ID;
            this.Info.Name = cha.Name;  
            this.Info.Level = 1;
            this.Info.Tid = cha.TID;
            this.Info.Class = (CharacterClass)cha.Class;
            this.Info.mapId = cha.MapID;
            this.Info.Gold = cha.Gold;
            this.Info.Exp = cha.EXP;
            this.Info.Equips = cha.Equips;
            this.Info.Entity = this.EntityData;
            // 定义数据
            this.Define = DataManager.Instance.Characters[this.Info.Tid];

            // 道具管理器初始化
            this.ItemManager = new ItemManager(this);
            this.ItemManager.GetItemInfos(this.Info.Items);

            // 背包的初始化
            this.Info.Bag = new NBagInfo();
            this.Info.Bag.Items = this.Data.Bag.Items;
            this.Info.Bag.Unlocked = this.Data.Bag.Unlocked;

            // 任务管理器初始化
            this.questManager = new QuestManager(this);
            this.questManager.GetQuestInfo(this.Info.Quests);

            this.statusManager = new StatusManager(this);
        }

        
    }
}
