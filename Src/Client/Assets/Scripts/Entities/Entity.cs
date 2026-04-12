using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SkillBridge.Message;

namespace Entities
{
    public class Entity
    {
        //包含四个属性 ID 位置 方向 速度

        // ID
        public int entityId;
        // 位置
        public Vector3Int position;
        // 方向
        public Vector3Int direction;
        // 速度
        public int speed;
        public NEntity EntityData
        {
            get
            {
                UpdateEntityDate();
                return entityData;
            }
            set
            {
                entityData = value;
                this.SetEntityData(value);
            }
        }
        private NEntity entityData;
        public Entity(NEntity entity)
        {
            this.entityId = entity.Id;
            this.entityData = entity;
            this.SetEntityData(entity);
        }
        public virtual void OnUpdate(float delta)
        {
            if (this.speed != 0)
            {
                Vector3 dir = this.direction;
                // 将浮点数转化为整形 因为游戏使用的是网格坐标系统
                this.position += Vector3Int.RoundToInt(dir * speed * delta / 100f);
            }

            // 将新的数据进行赋值
            entityData.Position.FromVector3Int(this.position);
            entityData.Direction.FromVector3Int(this.direction);
            entityData.Speed = this.speed;
        }
        public void SetEntityData(NEntity entity)
        {
            this.position = this.position.FromNVector3(entity.Position);
            this.direction = this.direction.FromNVector3(entity.Direction);
            this.speed = entity.Speed;
        }
        private void UpdateEntityDate()
        {
            entityData.Position.FromVector3Int(this.position);
            entityData.Direction.FromVector3Int(this.direction);
            entityData.Speed = this.speed;
        }

    }
}
