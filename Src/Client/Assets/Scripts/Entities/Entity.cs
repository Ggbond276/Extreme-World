using SkillBridge.Message;
using UnityEngine;

namespace Entities
{
    /// <summary>
    /// 领域实体基类 — 持有纯物理属性（位置/方向/速度），零业务零网络包长期持有。
    /// </summary>
    public class Entity
    {
        public int entityId;
        public Vector3Int position;
        public Vector3Int direction;
        public int speed;

        protected Entity(Vector3Int pos, Vector3Int dir)
        {
            this.position = pos;
            this.direction = dir;
            this.speed = 0;
        }

        public virtual void OnUpdate(float delta)
        {
            if (this.speed != 0)
            {
                Vector3 dir = this.direction;
                this.position += Vector3Int.RoundToInt(dir * speed * delta / 100f);
            }
        }

        public NEntity ToNEntity()
        {
            NEntity nEntity = new NEntity
            {
                Id = this.entityId,
                Position = new NVector3 { X = position.x, Y = position.y, Z = position.z },
                Direction = new NVector3 { X = direction.x, Y = direction.y, Z = direction.z },
                Speed = this.speed
            };
            return nEntity;
        }
    }
}
