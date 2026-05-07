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
     class SpawnManager
    {
        private Map map;
        private List<Spawner> Rules = new List<Spawner>();

        public void Init(Map map)
        {
            if(DataManager.Instance.Maps.ContainsKey(map.Define.ID))
            {
                this.map = map;
                if(DataManager.Instance.SpawnRules.ContainsKey(this.map.Define.ID))
                {
                    foreach (var kv in DataManager.Instance.SpawnRules[this.map.Define.ID])
                    {
                        SpawnRuleDefine rule = kv.Value;
                        Spawner spawner = new Spawner(rule, map);
                        this.Rules.Add(spawner);
                    }
                }
            }
        }

        public void Update()
        {
            if (this.Rules.Count == 0)
                return;
            for(int i =  0; i < this.Rules.Count; i++)
            {
                this.Rules[i].Update();
            }
        }
    }
}
