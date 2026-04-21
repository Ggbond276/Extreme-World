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
    }
    public void Dispose()
    {
        MessageDistributer.Instance.Unsubscribe<ItemBuyResponse>(this.OnItemBuy);
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
}
