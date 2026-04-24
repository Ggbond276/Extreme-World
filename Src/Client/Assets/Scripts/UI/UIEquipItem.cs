using Managers;
using Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class UIEquipItem : MonoBehaviour, IPointerClickHandler
{
    public Text Name;
    public Image Icon;
    public Text Class;
    public Text Level;

    private Item item;
    private int idx;
    private UICharEquip onwer;
    private bool isEquiped;

    public Image background;
    public Sprite normalBg;
    public Sprite selectedBg;
    private bool selected;
    public bool Selected
    {
        get { return selected; }
        set
        {
            selected = value;
            this.background.overrideSprite = selected ? selectedBg : normalBg;
        }
    }


    internal void SetEquipItem(int idx, Item item, UICharEquip onwer, bool equiped)
    {
        this.item = item;
        this.idx = idx;
        this.isEquiped = equiped;
        this.onwer = onwer;

        if (this.Name != null)
            this.Name.text = this.item.define.Name;
        if (this.Icon != null)
            this.Icon.overrideSprite = Resloader.Load<Sprite>(item.define.Icon);
        if (this.Class != null)
            this.Class.text = this.item.define.LimitClass;
        if (this.Level != null)
            this.Level.text = this.item.define.Level.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(this.isEquiped)
        {
            UnEquip();
        } else if (this.selected) {
            DoEquip();
            this.selected = false;
        }  else
        {
            this.selected = true;
        }
    }

    private void DoEquip()
    {
        var msg = MessageBox.Show(string.Format("要装备[{0}]吗？", this.item.define.Name), "确认", MessageBoxType.Confirm);
        Debug.Log(msg);
        msg.OnYes = () =>
        {
            var oldEquip = EquipManager.Instance.Equips[(int)item.EquipInfo.Slot];
            if (oldEquip != null)
            {
                var newMsg = MessageBox.Show(string.Format("要替换掉[{0}]吗?", oldEquip.define.Name), "确认", MessageBoxType.Confirm);
                newMsg.OnYes = () =>
                {
                    this.onwer.DoEquip(this.item);
                };
            }
            else
            {
                this.onwer.DoEquip(this.item);
            }
        };
    }

    private void UnEquip()
    {
        var msg = MessageBox.Show(string.Format("要脱下[{0}]吗?", this.item.define.Name), "确定", MessageBoxType.Confirm);
        msg.OnYes = () =>
        {
            this.onwer.UnEquip(this.item);
        };
    }

}
