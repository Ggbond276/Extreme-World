using Common;
using GameServer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class EntityManager : Singleton<EntityManager>
    {
        private int idx = 0;
        public List<Entity> AllEntities = new List<Entity>();
        public Dictionary<int, List<Entity>> MapEntites = new Dictionary<int, List<Entity>>();
        public void AddEntity(int mapId, Entity entity)
        {
            AllEntities.Add(entity);
            // 为什么要给NEntity一个idx 到时候这个EntityId要返回给谁
            entity.EntityData.Id = ++this.idx;

            List<Entity> entites = null;
            if(!MapEntites.TryGetValue(mapId, out entites))
            {
                entites = new List<Entity>();
                MapEntites[mapId] = entites;
            }
        }

        public void RemoveEntity(int mapId, Entity entity)
        {
            AllEntities.Remove(entity);
            MapEntites[mapId].Remove(entity);
        }
    }
}
