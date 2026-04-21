using Common.Data;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    class Item
    {
        public int ItemID;
        public int Count;
        public ItemDefine define;
        public Item(NItemInfo item) : 
            this(item.Id, item.Count)
        {
          
        }

        public Item(int id, int count)
        {
            this.ItemID =id;
            this.Count = count;
            this.define = DataManager.Instance.Items[this.ItemID];
        }
    }
}
