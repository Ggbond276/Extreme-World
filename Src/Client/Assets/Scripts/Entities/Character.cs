using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Data;
using SkillBridge.Message;
using UnityEngine;

namespace Entities
{
    public class Character : CharacterBase
    {
        public CharacterClass Class { get; set; }
        public long Gold { get; set; }
        public long Exp { get; set; }

        public bool IsPlayer
        {
            get { return Models.User.Instance.CurrentCharacter != null && this.entityId == Models.User.Instance.CurrentCharacter.entityId; }
        }

        public Character(NCharacterInfo info) : base(
            pos: new Vector3Int(info.Entity.Position.X, info.Entity.Position.Y, info.Entity.Position.Z),
            dir: new Vector3Int(info.Entity.Direction.X, info.Entity.Direction.Y, info.Entity.Direction.Z))
        {
            this.entityId = info.EntityId;

            this.Id       = info.Id;
            this.ConfigId = info.ConfigId;
            this.Name     = info.Name;
            this.Type     = info.Type;
            this.Level    = info.Level;
            this.MapId    = info.mapId;

            this.Class = info.Class;
            this.Gold  = info.Gold;
            this.Exp   = info.Exp;

            this.Define = DataManager.Instance.Characters[info.ConfigId];
        }

        public void MoveForward()
        {
            Debug.LogFormat("MoveForward");
            this.speed = this.Define.Speed;
        }

        public void MoveBack()
        {
            Debug.LogFormat("MoveBack");
            this.speed = -this.Define.Speed;
        }

        public void Stop()
        {
            Debug.LogFormat("Stop");
            this.speed = 0;
        }

        public void SetDirection(Vector3Int direction)
        {
            Debug.LogFormat("SetDirection:{0}", direction);
            this.direction = direction;
        }

        public void SetPosition(Vector3Int position)
        {
            Debug.LogFormat("SetPosition:{0}", position);
            this.position = position;
        }
    }
}
