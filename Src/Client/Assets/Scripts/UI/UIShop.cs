using Common.Data;
using Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// 我希望打开商店的时候可以传入一个数据来控制到底打开哪儿个商店
public class UIShop : UIWindow
{
    // 需要被挂载的对象
    public Transform[] content;
    private ShopDefine define;
    // 预制体
    public GameObject shopItem;
    // 需要根据Id修改商店的标题 以及渲染商店的金币
    public Text title;
    public Text money;
    void Start()
    {
        
    }
    // the Item you choose to buy
    public UIShopItem selectedItem;
    // 用来控制到底打开什么商店
    public void SetShop(ShopDefine define)
    {
        this.define = define;
        title.text = define.Name;
        money.text = User.Instance.CurrentCharacter.Gold.ToString();
        StartCoroutine(InitShop());
    }
    // 使用协程函数渲染
    IEnumerator InitShop()
    {
        // 数据应该从DataManager中获取
        foreach(var kv in DataManager.Instance.ShopItems[define.ID])
        {
            if(kv.Value.Status > 0)
            {
                // 创建预制体挂载到第一个content上
                GameObject go = Instantiate(shopItem, content[0]);
                // 然后将数据移交给UIShopItem让他去做对应的渲染逻辑
                var ui = go.GetComponent<UIShopItem>();
                ui.SetShopItem(kv.Value, this, kv.Key);
            }
           
        }
        yield return null;
    }
    internal void selectShopItem(UIShopItem shopItem)
    {
        if (selectedItem != null)
            selectedItem.Selected = false;
        this.selectedItem = shopItem;
        Debug.LogErrorFormat("{0}", this.selectedItem.Item.Name);   
    }
    public void OnClickBuy()
    {
        if (this.selectedItem == null)
        {
            MessageBox.Show("Please choose the item you wanna buy");
            return;
        }
        if(!ShopManager.Instance.ItemBuy(this.define.ID, this.selectedItem.shopItemId))
        {
            MessageBox.Show("The money is not enough");
            return;
        }
    }
    private void OnEnable()
    {
        User.OnGoldChanged += OnGoldChanged;
    }
    private void OnDisable()
    {
        User.OnGoldChanged -= OnGoldChanged;
    }
    public void OnGoldChanged(long Gold)
    {
        money.text = Gold.ToString();
    }
}
