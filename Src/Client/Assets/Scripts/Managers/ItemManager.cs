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
            StatusService.Instance.RegisterStatusNotify(StatusType.Item, OnItemNotify);
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

        private bool OnItemNotify(NStatus status)
        {
            if (status.Action == StatusAction.Add)
                this.AddItem(status.Id, status.Value);
            if (status.Action == StatusAction.Delete)
                this.DeleteItem(status.Id, status.Value);
            return true;
        }

        private void AddItem(int itemId, int count)
        {
            Item item = null;
            if(this.Items.TryGetValue(itemId,out item))
            {
                item.Count += count;
            } else
            {
                item = new Item(itemId, count);
                this.Items.Add(itemId, item);
            }
            BagManager.Instance.AddItem(itemId, count);
        }

        private void DeleteItem(int itemId, int count)
        {
            if (!this.Items.ContainsKey(itemId))
                return;
            Item item = this.Items[itemId];
            if (item.Count < count)
                return;
            item.Count -= count;

            BagManager.Instance.RemoveItem(itemId, count);
        }
    }
}