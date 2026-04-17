using Assets.Scripts.Models;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers
{
    class BagManager : Singleton<BagManager>
    {
        public BagItem[] bagItems;
        public ushort unlocked;
        
        public void Init(NBagInfo info)
        {
            this.unlocked = (ushort)info.Unlocked;
            bagItems = new BagItem[this.unlocked];
            this.Reset();
        }
        public void Reset()
        {
            int i = 0;
             foreach(var item in ItemManager.Instance.Items)
            {
                if(item.Value.Count <  item.Value.define.StackLimit)
                {
                    bagItems[i].ItemId = (ushort)item.Value.ItemID;
                    bagItems[i].Count = (ushort)item.Value.Count;
                    i++;
                } else
                {
                    int left = item.Value.Count;
                    while(left > item.Value.define.StackLimit)
                    {
                        bagItems[i].ItemId = (ushort)item.Value.ItemID;   
                        bagItems[i].Count = (ushort)item.Value.define.StackLimit;
                        left = left - item.Value.define.StackLimit;
                        i++;
                    }
                    bagItems[i].ItemId = (ushort)item.Value.ItemID;
                    bagItems[i].Count = (ushort)left;
                    i++;
                }
            }
        }


    }
}
