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

        internal void AddItem(int itemId, int count)
        {
            ushort addCount = (ushort)count;
             for(int i = 0; i < bagItems.Length; i++)
            {
                if(bagItems[i].ItemId == itemId)
                {
                    ushort canAdd = (ushort)(DataManager.Instance.Items[itemId].StackLimit - bagItems[i].Count);
                    if (canAdd == 0)
                        continue;
                    if(canAdd < addCount)
                    {
                        bagItems[i].Count += canAdd;
                        addCount -= canAdd;
                    } else
                    {
                        bagItems[i].Count += addCount;
                        addCount = 0;
                        break;
                    }
                }
            }

             if(addCount > 0)
            {
                for(int i = 0; i < bagItems.Length; i++)
                {
                    if(bagItems[i].ItemId == 0)
                    {
                        ushort canAdd = (ushort)(DataManager.Instance.Items[itemId].StackLimit);
                        if (canAdd < addCount)
                        {
                            bagItems[i].ItemId = (ushort)itemId;
                            bagItems[i].Count += canAdd;
                            addCount -= canAdd;
                        } else
                        {
                            bagItems[i].ItemId = (ushort)itemId;
                            bagItems[i].Count += addCount;
                            addCount = 0;
                            break;
                        }
                    }
                }
            }
        }

        internal void RemoveItem(int itemId, int count)
        {
            throw new NotImplementedException();
        }
    }
}
