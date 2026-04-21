using Common.Data;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : Singleton<ShopManager>
{
    
    public void Init()
    {
        NPCManager.Instance.RegisterNpcEvent(NpcFunction.InvokeShop, OnOpenShop);
    }

    // 点击NPC之后会触发的事件
    public bool OnOpenShop(NpcDefine npc)
    {
        // 这里出来的Id是0
        this.ShowShop(npc.Prarm);
        return true;
    }

    public void ShowShop(int shopId)
    {
        ShopDefine shop;
        if(DataManager.Instance.Shops.TryGetValue(shopId, out shop))
        {
            UIShop ui = UIManager.Instance.Show<UIShop>();
            if(ui != null)
            {
                ui.SetShop(shop);
            }
        }
    }

    public bool ItemBuy(int shopId, int ShopItemId)
    {
        int price = DataManager.Instance.ShopItems[shopId][ShopItemId].Price;
        long money = User.Instance.CurrentCharacter.Gold;
        if(price > money)
        {
            return false;
        }
        ItemService.Instance.SendItemBuy(shopId, ShopItemId);
        return true;
    }
  
}
