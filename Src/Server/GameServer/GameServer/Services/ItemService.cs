using Common;
using GameServer.Entities;
using GameServer.Manager;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
    class ItemService : Singleton<ItemService>
    {
        public ItemService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<ItemBuyRequest>(this.OnItemBuy);
        }

        public void Init()
        {

        }
        
        private void OnItemBuy(NetConnection<NetSession> sender, ItemBuyRequest request)
        {
             
            Log.InfoFormat("OnItemBuy : ShopID [ {0} ]  ShopItemID [ {1} ]", request.shopId, request.shopItemId);
            Result result = ShopManager.Instance.BuyItem(sender, request.shopId, request.shopItemId);
            sender.Session.Response.itemBuy = new ItemBuyResponse();
            sender.Session.Response.itemBuy.Result = result;
            sender.SendResponse();
        }
    }
}
