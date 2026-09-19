using Managers;
using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 客户端道具网络服务层（Service层）。
/// 职责：专门负责处理【购买道具】和【穿脱装备】的网络协议发送，并监听服务器返回的处理结果。
/// </summary>
public class ItemService : Singleton<ItemService>, IDisposable
{

    public ItemService()
    {
        MessageDistributer.Instance.Subscribe<ItemBuyResponse>(this.OnItemBuy);  // 订阅：接收服务器下发的【购买道具】回执
        MessageDistributer.Instance.Subscribe<ItemEquipReponse>(this.OnEquipItem); // 订阅：接收服务器下发的【穿脱装备】回执
    }
    public void Dispose()
    {
        MessageDistributer.Instance.Unsubscribe<ItemBuyResponse>(this.OnItemBuy); // 资源释放时注销事件，防止内存泄漏或野指针
        MessageDistributer.Instance.Unsubscribe<ItemEquipReponse>(this.OnEquipItem);
    }

    /// <summary>
    /// 向服务器发送【购买道具】的请求。
    /// </summary>
    public void SendItemBuy(int shopId, int shopItemId)
    {
        // ============================================== 日志记录 ==============================================

        Debug.LogFormat("sendItemBuy : shopId [ {0} ]  shopItemId [ {1} ]", shopId, shopItemId);

        // ============================================== 组装网络包 ==============================================

        NetMessage message = new NetMessage(); 
        message.Request = new NetMessageRequest();
        message.Request.itemBuy = new ItemBuyRequest();
        message.Request.itemBuy.shopId = shopId; // 封装请求参数：商店ID
        message.Request.itemBuy.shopItemId = shopItemId; // 封装请求参数：商店内的具体商品ID
        NetClient.Instance.SendMessage(message);  // 通过底层 Socket 客户端发送给服务器
    }

    /// <summary>
    /// 处理服务器发送【购买道具】的回调
    /// </summary>
    public void OnItemBuy(object sender, ItemBuyResponse response)
    {
        // TODO: 处理购买成功或失败的回执逻辑（目前为空，大概率是因为服务器会通过 OnItemNotify 直接走状态机下发道具，这里无需做额外 UI 处理）
    }

    // ============================================ 装备穿脱防重发机制 ============================================

    // TODO: 这里要思考一下 装备系统写在这里是否会导致系统的耦合度过高的问题
    Item pendingEquip = null; // 核心状态锁：缓存当前正在向服务器请求穿脱的装备（用于防止玩家狂点按钮导致发包风暴）
    bool isEquip; // 状态标记：当前发起的请求是【穿上】(true) 还是 【脱下】(false)

    /// <summary>
    /// 向服务器发送【穿上/脱下 装备】的请求。
    /// 执行后：会将当前请求锁定在 pendingEquip 中，直到服务器返回结果前，拒绝任何新的穿脱请求。
    /// </summary>
    public bool SendEquipItem(Item equip, bool isEquip)
    {
        // ============================================== 拦截校验 ==============================================
        Debug.LogFormat("SendEquipItem ID : [{0}]  NAME : [{1}]", equip.ItemID, equip.define.Name);
        if (pendingEquip != null) // 异步拦截锁：上一件装备的服务器回执还没到，严禁发送新请求，直接返回 false！
            return false;
        this.pendingEquip = equip; // 上锁：缓存正在处理的装备对象
        this.isEquip = isEquip; // 记录当前操作类型

        // ============================================== 组装网络包 ==============================================

        NetMessage message = new NetMessage();
        message.Request = new NetMessageRequest();
        message.Request.itemEquip = new ItemEquipRequest();
        message.Request.itemEquip.Slot = (int)equip.EquipInfo.Slot; // 封装：装备需要装配到哪个槽位
        message.Request.itemEquip.itemId = equip.ItemID; // 封装：要操作的装备 ID
        message.Request.itemEquip.isEquip = isEquip; // 封装：操作类型（穿/脱）

        NetClient.Instance.SendMessage(message); // 将包发送至服务器进行合法性校验
        return true;
    }

    /// <summary>
    /// 接收服务器下发的【穿脱装备】回执。
    /// 执行后：如果成功，真正呼叫 EquipManager 去修改本地数据与 UI；最后释放 pendingEquip 锁。
    /// </summary>
    private void OnEquipItem(object sender, ItemEquipReponse message) 
    {
        if(message.Result == Result.Success) // 仅当服务器校验通过（例如等级达标、确实拥有该装备）才执行
        {
            if(pendingEquip != null) // 确保存在正在等待处理的挂起装备
            {
                // ======================================== 联动 Manager 处理 ========================================
                if (this.isEquip)
                    EquipManager.Instance.OnEquipItem(pendingEquip); // 业务下发：穿上逻辑
                else
                    EquipManager.Instance.OnUnEquipItem(pendingEquip); // 业务下发：脱下逻辑

                // ============================================== 释放锁 ==============================================
                pendingEquip = null; // 核心步骤：彻底释放挂起锁，允许玩家进行下一次穿脱操作
            }
            else
            {
                // TODO: 兜底解锁逻辑，防止服务器返回 Failed 时导致锁死无法换装备
                pendingEquip = null;
            }
        }
    }
}
