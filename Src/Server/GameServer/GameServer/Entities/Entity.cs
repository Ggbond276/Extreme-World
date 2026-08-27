using GameServer.Core;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entities
{
    /// <summary>
    /// 领域实体基类 — 纯领域模型，零网络层依赖。
    /// 所有物理属性（Id/Position/Direction/Speed）独立维护。
    /// 需要网络传输时通过 ToNEntity() 实时打包。
    /// </summary>
    class Entity
    {
        // 实体ID（独立维护的领域属性）
        public int entityId;

        // 位置
        private Vector3Int position;
        public Vector3Int Position
        {
            get { return position; }
            set { position = value; }
        }

        // 方向
        private Vector3Int direction;
        public Vector3Int Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        // 速度
        private int speed;
        public int Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        public Entity(Vector3Int pos, Vector3Int dir)
        {
            this.position = pos;
            this.direction = dir;
            this.speed = 0;
            // Id 留给 EntityManager 分配
        }

        /// <summary>
        /// 将当前实体的物理属性打包为新的 NEntity 网络包对象。
        /// 每次调用都生成全新实例，避免外部修改污染内部状态。
        /// </summary>
        public virtual NEntity ToNEntity()
        {
            NEntity nEntity = new NEntity();
            nEntity.Id = this.entityId;
            nEntity.Position = this.position;
            nEntity.Direction = this.direction;
            nEntity.Speed = this.speed;
            return nEntity;
        }
    }
}