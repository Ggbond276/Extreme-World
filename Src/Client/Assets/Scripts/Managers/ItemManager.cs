using Models;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Data;

namespace Managers
{
    class ItemManager : Singleton<ItemManager>
    {
        public Dictionary<int, Item> Items = new Dictionary<int, Item>();
        // 初始化操作
        public void Init(List<NItemInfo> list)
        {
            this.Items.Clear();
            foreach (var info in list)
            {
                Items.Add(info.Id, new Item(info));
            }
        }

        public ItemDefine GetItem(int itemId)
        {
            return null;
        }

        public bool UseItem(int itemId)
        {
            return false;
        }

        public bool UseItem(ItemDefine item)
        {
            return false;
        } 

    }
}