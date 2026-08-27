using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Data;
using SkillBridge.Message;
using UnityEngine;

namespace Entities
{
    public class Character : Entity
    {
        // ============ 【Step 1 影子字段】独立业务属性（取代 Info.XXX 读取） ============
        public int Id { get; set; }
        public string Name { get; set; }
        public int ConfigId { get; set; }
        public int Level { get; set; }
        public CharacterClass Class { get; set; }
        public int MapId { get; set; }
        public long Gold { get; set; }
        public long Exp { get; set; }

        // ============ 【Step 5 删除】Info 字段拔除；如需 NEntity 数据请用基类 Entity.EntityData ============

        // ============ 【图纸引用】独立 ============
        public CharacterDefine Define;

        // ============ 【实体识别】EntityId 由基类 Entity 提供（entityId 字段） ============

        public bool IsPlayer
        {
            get { return this.entityId == Models.User.Instance.CurrentCharacter.EntityId; }
        }

        public Character(NCharacterInfo info) : base(info.Entity)
        {
            // ============ 【Step 1】拆解 NCharacterInfo → 填充到内部独立属性 ============
            this.Id = info.Id;
            this.Name = info.Name;
            this.ConfigId = info.ConfigId;
            this.Level = info.Level;
            this.Class = info.Class;
            this.MapId = info.mapId;
            this.Gold = info.Gold;
            this.Exp = info.Exp;

            // ============ 【Step 5 删除】this.Info = info; — 全部字段已迁移 ============

            // ============ 【图纸】独立查表 ============
            this.Define = DataManager.Instance.Characters[info.ConfigId];
        }

        //向前移动
        public void MoveForward()
        {
            Debug.LogFormat("MoveForward");
            this.speed = this.Define.Speed;
        }
        //向后移动
        public void MoveBack()
        {
            Debug.LogFormat("MoveBack");
            this.speed = -this.Define.Speed;
        }
        //停止
        public void Stop()
        {
            Debug.LogFormat("Stop");
            this.speed = 0;
        }
        //设置方向
        public void SetDirection(Vector3Int direction)
        {
            Debug.LogFormat("SetDirection:{0}", direction);
            this.direction = direction;
        }
        //设置位置
        public void SetPosition(Vector3Int position)
        {
            Debug.LogFormat("SetPosition:{0}", position);
            this.position = position;
        }
    }
}
