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
        public Item(NItemInfo item)
        {
            this.ItemID = item.Id;
            this.Count = item.Count;
            this.define = DataManager.Instance.Items[this.ItemID];
        }
    }
}
