using Common;
using Common.Data;
using GameServer.Entities;
using GameServer.Services;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
    class ShopManager : Singleton<ShopManager>
    {
        internal Result BuyItem(NetConnection<NetSession> sender, int shopId, int shopItemId)
        {
            if (!DataManager.Instance.Shops.ContainsKey(shopId))
                return Result.Failed;
            ShopItemDefine define;
            if(DataManager.Instance.ShopItems[shopId].TryGetValue(shopItemId, out define))
            {
                Character character = sender.Session.Character;

                // 因为我们的商店系统还没有做购买多个商品的逻辑 所以先统一只购买一个商品
                int count = 1;
                character.ItemManager.AddItem(define.ItemID, count);
                character.Gold -= define.Price;
                DBService.Instance.save();
                return Result.Success;
            }
            return Result.Failed;
        }
    }
}
