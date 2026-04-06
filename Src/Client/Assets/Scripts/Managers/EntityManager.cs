using Entities;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers
{
    interface IEntityNotify
    {
        void OnEntityRemoved();
    }
    class EntityManager : Singleton<EntityManager>
    {
        Dictionary<int, Entity> entities = new Dictionary<int, Entity>();

        // 这两个部分都是关于接口使用的 研究下这个接口到底有什么作用
        Dictionary<int, IEntityNotify> notifiers = new Dictionary<int, IEntityNotify>();
        public void RegisterEntityChangeNotify(int entityId, IEntityNotify notify)
        {
            this.notifiers[entityId] = notify;
        }

        public void AddEntity(Entity entity)
        {
            entities[entity.entityId] = entity;
        }
        public void RemoveEntity(NEntity entity)
        {
            this.entities.Remove(entity.Id);
            if(notifiers.ContainsKey(entity.Id))
            {
                notifiers[entity.Id].OnEntityRemoved();
                notifiers.Remove(entity.Id);
            }
        }
    }
}
