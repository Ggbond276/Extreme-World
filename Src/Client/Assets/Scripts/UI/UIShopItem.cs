using Common.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIShopItem : MonoBehaviour, ISelectHandler
{
    // 渲染逻辑所需要的组件
    public Image ItemImage;
    public Text Name;
    public Text Price;
    public Text Count;
    public ItemDefine Item;

    // 选择事件所需要的对象
    private UIShop father;

    // 高亮事件需要的对象
    public Image Bg;
    private bool selected = false;
    public bool Selected { 
        get { return selected;  }
        set
        {
            selected = value;
            Bg.overrideSprite = selected ? activeBg : normalBg;
        }
    }
    public Sprite normalBg;
    public Sprite activeBg;

    public int shopItemId;
    void Start()
    {
        
    }
    // 渲染逻辑
    public void SetShopItem(ShopItemDefine define, UIShop father, int shopItemId)
    {
        // 让孩子知道他爹是谁
        this.father = father;
        this.shopItemId = shopItemId;
        this.Item = DataManager.Instance.Items[define.ItemID];
        // 渲染图片
        this.ItemImage.overrideSprite = Resloader.Load<Sprite>(this.Item.Icon);
        // 渲染价格
        this.Price.text = define.Price.ToString();
        // 渲染数量
        this.Count.text = define.Count.ToString();
        // 渲染名称
        this.Name.text = this.Item.Name;
    }
    // 点击事件
    public void OnSelect(BaseEventData eventData)
    {
        // 将自己扔给管理器进行设置
        this.Selected = true;
        father.selectShopItem(this);
    }

}
