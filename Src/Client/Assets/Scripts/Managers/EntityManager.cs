using Entities;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers
{
    interface IEntityNotify
    {
        void OnEntityRemoved();
        void OnEntityEvent(EntityEvent @event);
        void OnEntityChanged(Entity entity);
    }
    class EntityManager : Singleton<EntityManager>
    {
        Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
        
        /// <summary>
        /// 移动同步分发
        /// </summary>
        Dictionary<int, IEntityNotify> notifiers = new Dictionary<int, IEntityNotify>();
        public void RegisterEntityChangeNotify(int entityId, IEntityNotify notify)
        {
            this.notifiers[entityId] = notify;
        }

        /// <summary>
        /// 添加Entity
        /// </summary>
        /// <param name="entity"></param>
        public void AddEntity(Entity entity)
        {
            entities[entity.entityId] = entity;
        }

        public void RemoveEntity(int entityId)
        {
            this.entities.Remove(entityId);
            if(notifiers.ContainsKey(entityId))
            {
                notifiers[entityId].OnEntityRemoved();
                notifiers.Remove(entityId);
            }
        }

        internal void OnEntitySync(NEntitySync data)
        {
            Entity entity = null;
            entities.TryGetValue(data.Id, out entity);
            if(entity != null)
            {
                if (data.Entity != null)
                {
                    entity.entityId = data.Entity.Id;
                    entity.position = new Vector3Int(data.Entity.Position.X, data.Entity.Position.Y, data.Entity.Position.Z);
                    entity.direction = new Vector3Int(data.Entity.Direction.X, data.Entity.Direction.Y, data.Entity.Direction.Z);
                    entity.speed = data.Entity.Speed;
                }
                if(notifiers.ContainsKey(data.Id))
                {
                    notifiers[entity.entityId].OnEntityChanged(entity);
                    notifiers[entity.entityId].OnEntityEvent(data.Event);
                }
            }
        }
    }
}
