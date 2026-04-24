using Managers;
using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemService : Singleton<ItemService>, IDisposable
{

    public ItemService()
    {
        MessageDistributer.Instance.Subscribe<ItemBuyResponse>(this.OnItemBuy);
        MessageDistributer.Instance.Subscribe<ItemEquipReponse>(this.OnEquipItem);
    }
    public void Dispose()
    {
        MessageDistributer.Instance.Unsubscribe<ItemBuyResponse>(this.OnItemBuy);
        MessageDistributer.Instance.Unsubscribe<ItemEquipReponse>(this.OnEquipItem);
    }

    public void SendItemBuy(int shopId, int shopItemId)
    {
        Debug.LogFormat("sendItemBuy : shopId [ {0} ]  shopItemId [ {1} ]", shopId, shopItemId);
        NetMessage message = new NetMessage(); 
        message.Request = new NetMessageRequest();
        message.Request.itemBuy = new ItemBuyRequest();
        message.Request.itemBuy.shopId = shopId;
        message.Request.itemBuy.shopItemId = shopItemId;
        NetClient.Instance.SendMessage(message); 
    }
    public void OnItemBuy(object sender, ItemBuyResponse response)
    {

    }

    // 根据我们设计的网络协议来看
    Item pendingEquip = null;
    bool isEquip;

    public bool SendEquipItem(Item equip, bool isEquip)
    {
        Debug.LogFormat("SendEquipItem ID : [{0}]  NAME : [{1}]", equip.ItemID, equip.define.Name);
        if (pendingEquip != null)
            return false;
        this.pendingEquip = equip;
        this.isEquip = isEquip;

        NetMessage message = new NetMessage();
        message.Request = new NetMessageRequest();
        message.Request.itemEquip = new ItemEquipRequest();
        message.Request.itemEquip.Slot = (int)equip.EquipInfo.Slot;
        message.Request.itemEquip.itemId = equip.ItemID;
        message.Request.itemEquip.isEquip = isEquip;

        NetClient.Instance.SendMessage(message);
        return true;
    }

    private void OnEquipItem(object sender, ItemEquipReponse message) 
    {
        if(message.Result == Result.Success)
        {
            if(pendingEquip != null)
            {
                if (this.isEquip)
                    EquipManager.Instance.OnEquipItem(pendingEquip);
                else
                    EquipManager.Instance.OnUnEquipItem(pendingEquip);
                pendingEquip = null;
            }
        }
    }
}
