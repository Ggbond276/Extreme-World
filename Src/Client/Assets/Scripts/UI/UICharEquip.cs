using Managers;
using Models;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICharEquip : UIWindow
{
    // 左边列表需要挂载的根节点 和需要渲染的预制体
    public GameObject ListEquipItemPrefab;
    public Transform ItemListRoot;

    public GameObject EquipItemPrefab;
    public List<Transform> slots;


    void Start()
    {
        EquipManager.Instance.OnEquipChanged += RefreshUI;
        RefreshUI();
    }

    void RefreshUI()
    {
        ClearAllEquipItemList();
        InitAllEquipItemList();
        ClearEquipedItemList();
        InitEquipedItemList();
    }

    /// <summary>
    /// 初始化装备列表
    /// </summary>
    /// <returns></returns>
    void InitAllEquipItemList()
    {
        foreach (var kv in ItemManager.Instance.Items)
        {
            if (kv.Value.define.Type == ItemType.Equip)
            {
                if (EquipManager.Instance.Contains(kv.Value.ItemID))
                    continue;
                GameObject go = Instantiate(ListEquipItemPrefab, ItemListRoot);
                UIEquipItem ui = go.GetComponent<UIEquipItem>();
                ui.SetEquipItem(kv.Key, kv.Value, this, false);
            }
        }
    }

    /// <summary>
    /// 清理装备列表
    /// </summary>
    void ClearAllEquipItemList()
    {
        foreach (var item in ItemListRoot.GetComponentsInChildren<UIEquipItem>())
        {
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void InitEquipedItemList()
    {
        for (int i = 0; i < (int)EquipSlot.SlotMax; i++)
        {
            var item = EquipManager.Instance.Equips[i];
            if (item != null)
            {
                GameObject go = Instantiate(EquipItemPrefab, slots[i]);
                UIEquipItem ui = go.GetComponent<UIEquipItem>();
                ui.SetEquipItem(i, item, this, true);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void ClearEquipedItemList()
    {
        foreach (var root in slots)
        {
            if (root.childCount > 1)
            {
                Destroy(root.GetChild(1).gameObject);
            }
        }
    }

    //  最难的双击选择逻辑
    public void DoEquip(Item item)
    {
        EquipManager.Instance.DoEquip(item);
    }

    public void UnEquip(Item item)
    {
        EquipManager.Instance.UnEquip(item);
    }
}
