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
       
        public TCharacter Data;
        public ItemManager ItemManager;
        //构造方法
        public Character(CharacterType type,TCharacter cha): base(new Core.Vector3Int(cha.MapPosX, cha.MapPosY, cha.MapPosZ), new Core.Vector3Int(100,0,0))
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
            this.Info.Entity = this.EntityData;
            // 定义数据
            this.Define = DataManager.Instance.Characters[this.Info.Tid];

            this.ItemManager = new ItemManager(this);
            this.ItemManager.GetItemInfos(this.Info.Items);
        }


    }
}
