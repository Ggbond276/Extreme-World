using Common;
using Common.Data;
using GameServer.Manager;
using GameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
     class Spawner
    {
        public SpawnRuleDefine define;
        public Map map;

        public SpawnPointDefine spawnPoint = null;

        public bool spawned = false;

        public float spawnTime = 0;
        public float unspawnTime = 0;   

        public Spawner(SpawnRuleDefine define, Map map)
        {
            this.define = define;
            this.map = map;
            if(DataManager.Instance.SpawnPoints.ContainsKey(map.Define.ID))
            {
                if(DataManager.Instance.SpawnPoints[map.Define.ID].ContainsKey(this.define.SpawnPoint)) {
                    spawnPoint = DataManager.Instance.SpawnPoints[map.Define.ID][this.define.SpawnPoint];
                }
                else
                {
                    Log.ErrorFormat("SpawnRule[{0}] SpawnPoint[{1}] not existed", this.define.ID, this.define.SpawnPoint);
                }
            }
        }
        public void Update()
        {
            if (this.CanSpawn())
                this.Spawn();
        }
        private bool CanSpawn()
        {
            if (this.spawned == true)
                return false;
            if (this.unspawnTime + this.define.SpawnPeriod > Time.time)
                return false;
            return true;
        }

        public void Spawn()
        {
            this.spawned = true;
            Log.InfoFormat("Map[{0}]Spawn[{1}]:Mon:{2},Lv:{3}] At Point:{4}", this.define.MapID, this.define.ID, this.define.SpawnMonID, this.define.SpawnLevel, this.spawnPoint.Position);
            this.map.MonsterManager.Create(this.define.SpawnMonID, this.define.SpawnLevel, this.spawnPoint.Position, this.spawnPoint.Direction);
        }
    }
}
