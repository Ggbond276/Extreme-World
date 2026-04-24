using Models;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers
{
    public class EquipManager : Singleton<EquipManager>
    {
        public delegate void OnEquipChangeHandler();
        public event OnEquipChangeHandler OnEquipChanged;

        // 初始化数组大小是7
        public Item[] Equips = new Item[(int)EquipSlot.SlotMax];


        unsafe public void Init(byte[] data) 
        {
            // 将data的数据反序列化到Equips数组里面
            this.ParseEquipData(data);
        }
        unsafe void ParseEquipData(byte[] data)
        {
            fixed(byte* pt = data)
            {
                for(int i = 0; i < this.Equips.Length; i++)
                {
                    int itemId = *(int*)(pt + i * sizeof(int));
                    if (itemId > 0)
                        Equips[i] = ItemManager.Instance.Items[itemId];
                    else
                        Equips[i] = null;
                }
            }
        }

        public bool Contains(int equipId)
        {
            for(int i = 0; i < this.Equips.Length; i++)
            {
                if (this.Equips[i] != null && this.Equips[i].ItemID == equipId)
                    return true;
            }
            return false;
        }
        public Item GetEquip(EquipSlot slot)
        {
            return Equips[(int)slot];
        }

        // 你现在只需要知道调用了Equip之后装备就会装备上了
        public void DoEquip(Item equip)
        {
            ItemService.Instance.SendEquipItem(equip, true);
        }
        // 你现在只需要知道调用了UnEquip之后装备就会卸载了
        public void UnEquip(Item equip)
        {
            ItemService.Instance.SendEquipItem(equip, false);
        }

        // 列表和槽位全都归EquipManager管理
        internal void OnEquipItem(Item equip)
        {
            int slot = (int)equip.EquipInfo.Slot;
            //这里的逻辑不是常规的切换逻辑 而是装备需要卸下才可以装备其他的装备
            if (this.Equips[slot] != null && this.Equips[slot].ItemID != equip.ItemID)
                return;
            this.Equips[slot] = ItemManager.Instance.Items[equip.ItemID];

            // 修改完之后需要调用UI层 让UI层发生变化
            if (OnEquipChanged != null)
                this.OnEquipChanged();
        }
        internal void OnUnEquipItem(Item equip)
        {
            int slot = (int)equip.EquipInfo.Slot;
            if(this.Equips[slot] != null)
            {
                this.Equips[slot] = null;
            }
            if (OnEquipChanged != null)
                this.OnEquipChanged();
        }
    }
}
