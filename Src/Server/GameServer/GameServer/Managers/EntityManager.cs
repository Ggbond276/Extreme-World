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
        /// <summary>
        /// 这里管理的是全局所有的Entity实体
        /// </summary>
        public List<Entity> AllEntities = new List<Entity>();
        /// <summary>
        /// 这里管理的是每张地图中的Entity实体
        /// </summary>
        public Dictionary<int, List<Entity>> MapEntites = new Dictionary<int, List<Entity>>();

        /// <summary>
        /// 这里的entity拿到的实际是一个Character
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="entity"></param>
        public void AddEntity(int mapId, Entity entity)
        {
            // 添加角色数据到Character到Entity管理器
            AllEntities.Add(entity);
            // 赋予Character一个
            entity.EntityData.Id = ++this.idx;

            
            if(!MapEntites.ContainsKey(mapId))
            {
                List<Entity> entites = new List<Entity>();
                MapEntites[mapId] = entites;
            }

            MapEntites[mapId].Add(entity);
        }

        public void RemoveEntity(int mapId, Entity entity)
        {
            AllEntities.Remove(entity);
            MapEntites[mapId].Remove(entity);
        }
    }
}
