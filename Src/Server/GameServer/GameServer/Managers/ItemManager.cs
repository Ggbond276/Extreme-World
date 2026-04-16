using Common;
using GameServer.Entities;
using GameServer.Models;
using GameServer.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class ItemManager
    {
        public Character Owner;
        public Dictionary<int, Item> Items = new Dictionary<int, Item>();
        public ItemManager(Character Owner)
        {
            this.Owner = Owner;
            foreach(var Item in Owner.Data.Items)
            {
                Items.Add(Item.ItemID, new Item(Item));
            }
        }
        // 使用物品
        public bool UseItem(int itemId, int count = 1)
        {
            //if(Items.ContainsKey(itemId))
            //{
            //    if(Items[itemId].Count > count)
            //    {
            //        Items[itemId].Remove(count);
            //        // 执行具体的使用逻辑

            //        return true;
            //    }
            //}
            //return false;
            Item item = null;
            if(Items.TryGetValue(itemId, out item))
            {
                if (item.Count < count)
                    return false;

                // 执行具体的使用逻辑
                item.Remove(count);
                return true;
            }
            return false;
        }

        // 判断是否存在物品
        public bool HasItem(int itemId)
        {
            Item item = null;
            if (Items.TryGetValue(itemId, out item))
                return item.Count > 0;
            return false;
        }

        // 获取物品
        public Item GetItem(int itemId)
        {
            Item item = null;
            Items.TryGetValue(itemId, out item);
            return item;
        }
        // 添加物品
        public bool AddItem(int itemId, int count)
        {
            Item item = null;
            if(Items.TryGetValue(itemId, out item))
            {
                item.Add(count);
            }else
            {
                TCharacterItem dbItem = new TCharacterItem();
                dbItem.TCharacterID = this.Owner.Data.ID;
                dbItem.Owner = Owner.Data;
                dbItem.ItemID = itemId;
                dbItem.ItemCount = count;
                // 1.db层面添加
                this.Owner.Data.Items.Add(dbItem);
                // 2.实体层面添加
                item = new Item(dbItem);
                this.Items.Add(itemId, item);
            }
            Log.InfoFormat("[ {0} ]AddItem[ {1} ] addCount : {2}", this.Owner.Data.ID, item.ItemID, item.Count);
            DBService.Instance.save();
            return true;
        }
        // 删除物品
        public bool RemoveItem(int itemId, int count)
        {
            Item item = null;
            if(!Items.TryGetValue(itemId, out item))
            {
                return false;
            }
            item = this.Items[itemId];
            if (item.Count < count)
                return false;
            item.Remove(count);
            Log.InfoFormat("[ {0} ]RemoveItem[ {1} ] removeCount : {2}", this.Owner.Data.ID, item.ItemID, item.Count);
            DBService.Instance.save();
            return true;
        }

        // 将数据转成网络数据
        public void GetItemInfos(List<NItemInfo> list)
        {
            foreach(var item in this.Items)
            {
                list.Add(new NItemInfo() { Id = item.Value.ItemID, Count = item.Value.Count });
            }
        }
    } 
}
