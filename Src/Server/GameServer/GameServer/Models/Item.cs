using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    class Item
    {
        TCharacterItem dbItem;

        public int ItemID;
        public int Count;

        // 构造方法
        public Item(TCharacterItem dbitem)
        {
            this.dbItem = dbitem;
            this.ItemID = (short)dbitem.ItemID;
            this.Count = (short)dbitem.ItemCount;
        }

        // 商品数量加count
         public void Add(int count)
        {
            this.Count += count;
            dbItem.ItemCount = this.Count;
        }

        // 商品数量减count
        public void Remove(int count)
        {
            this.Count -= count;
            dbItem.ItemCount = this.Count;
        }

        // 使用商品
        public bool Use(int count = 1)
        {
            return false;
        }

        // 打印信息
        public override string ToString()
        {
            return string.Format("ID : {0}, Count : {1}", this.ItemID, this.Count);
        }
    }
}
