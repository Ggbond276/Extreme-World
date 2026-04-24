using Common;
using GameServer.Entities;
using GameServer.Manager;
using GameServer.Managers;
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
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<ItemEquipRequest>(this.OnItemEquip);
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
        // 根据通讯协议 服务端可以得到三个信息slot itemId isEquip
        private void OnItemEquip(NetConnection<NetSession> sender, ItemEquipRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("OnItemEquip : character:{0} : Slot:{1} Item:{2} Equip:{3}",character.Id, request.Slot, request.itemId, request.isEquip);
            Result result = EquipManager.Instance.EquipItem(sender, request.Slot, request.itemId, request.isEquip);

            sender.Session.Response.itemEquip = new ItemEquipReponse();
            sender.Session.Response.itemEquip.Result = result;

            sender.SendResponse();

        }
    }
}
