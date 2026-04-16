using Common.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers
{
    class TestManager:Singleton<TestManager>
    {
        public void Init()
        {
            NPCManager.Instance.RegisterNpcEvent(NpcFunction.InvokeShop, OnNpcIvokeShop);
            NPCManager.Instance.RegisterNpcEvent(NpcFunction.InvokeInsrance, OnNpcIvokeInsrance);
        }
 
        private bool OnNpcIvokeShop(NpcDefine npc)
        {
            Debug.LogFormat("TestManager.OnNpcIvokeShop : NPC[ {0} : {1} ] Type : {2}  Func : {3} ", npc.ID, npc.Name, npc.Type, npc.Function);
            UITest test = UIManager.Instance.Show<UITest>();
            test.setTitle(npc.Name);
            return true;
        }

        private bool OnNpcIvokeInsrance(NpcDefine npc)
        {
            Debug.LogFormat("TestManager.OnNpcIvokeShop : NPC[ {0} : {1} ] Type : {2}  Func : {3} ", npc.ID, npc.Name, npc.Type, npc.Function);
            MessageBox.Show("点击了Npc: " + npc.Name);
            return true;
        }
    }
}
