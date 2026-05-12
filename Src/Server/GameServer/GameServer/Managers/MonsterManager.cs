using GameServer.Entities;
using GameServer.Models;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class MonsterManager
    {
        private Map Map;
        /// <summary>
        /// 这里依旧使用EntityId作为字典键值
        /// </summary>
        public Dictionary<int, Monster> Monsters = new Dictionary<int, Monster>();

        public void Init(Map map)
        {
            this.Map = map;
        }

        internal void Create(int spawnMonID, int spawnLevel, NVector3 position, NVector3 direction)
        {
            Monster monster = new Monster(spawnMonID, spawnLevel, position, direction);
            EntityManager.Instance.AddEntity(Map.Define.ID, monster);
            monster.Info.EntityId = monster.entityId;
            monster.Info.mapId = this.Map.Define.ID;
            Monsters[monster.entityId] = monster;
            this.Map.MonsterEnter(monster);
        }
    }
}
