using Assets.Scripts.Managers;
using Models;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIGuildMemberItem : ListViewItem, IPointerClickHandler
{

    [Header("待渲染组件")]
    public TextMeshProUGUI textRankValue;
    public TextMeshProUGUI textNameValue;
    public TextMeshProUGUI textLevelValue;
    public TextMeshProUGUI textClassValue;
    public Image imageClassValue;
    public TextMeshProUGUI textPositionValue;
    public TextMeshProUGUI textStatusValue;
    public Image imageStatusIcon;

    [Header("状态表现")]
    public Image ImageBg;
    public Sprite normalBg;
    public Sprite selectedBg;


    public NGuildMember selectedInfo;

    internal void SetMemberInfo(NGuildMember member, int rank)
    {
        this.selectedInfo = member;
        if (textRankValue != null) textRankValue.text = rank.ToString();
        if (textNameValue != null) textNameValue.text = member.Name;
        if (textLevelValue != null) textLevelValue.text = member.Level.ToString();
        if (textClassValue != null)
        {
            string characterClass;
            switch (member.ClassType)
            {
                case (int)CharacterClass.Archer:
                    characterClass = "弓箭手";
                    break;
                case (int)CharacterClass.Warrior:
                    characterClass = "战士";
                    break;
                case (int)CharacterClass.Wizard:
                    characterClass = "法师";
                    break;
                default:
                    characterClass = "无职业";
                    break;
            }

            textClassValue.text = characterClass;
        }
        if (imageClassValue != null) imageClassValue.overrideSprite = SpriteManager.Instance.GetClassSprite((CharacterClass)member.ClassType);
        if (textPositionValue != null)
        {

            string position;
            switch (member.Position)
            {
                case GuildPosition.GuildPositionLeader:
                    position = "会长";
                    break;
                case GuildPosition.GuildPositionViceLeader:
                    position = "副会长";
                    break;
                case GuildPosition.GuildPositionMember:
                    position = "会员";
                    break;
                default:
                    position = "无";
                    break;
            }
            textPositionValue.text = position;
            
        }
        if(textStatusValue != null && imageStatusIcon != null)
        {
            bool isOnline = true;
            if (member.IsOnline == 0) isOnline = false;
            // 在线颜色：#246C24
            ColorUtility.TryParseHtmlString("#246C24", out Color onlineColor);
            // 离线灰色：#808080（也可以直接用 Color.gray）
            ColorUtility.TryParseHtmlString("#808080", out Color offlineColor);

            textStatusValue.text = isOnline ? "在线" : "离线";
            textStatusValue.color = isOnline ? onlineColor : offlineColor;

            imageStatusIcon.overrideSprite = SpriteManager.Instance.GetStatusSprite(isOnline);
        }
    }

    public override void OnSelected(bool selected)
    {
        if (this.selectedBg != null)
        {
            this.ImageBg.overrideSprite = selected ? selectedBg : normalBg;
        }
    }

    public new void OnPointerClick(PointerEventData eventData)
    {
        //  1. 依然可以让父类去执行它原有的逻辑 (处理左键点击高亮)
        base.OnPointerClick(eventData);

        //  2. 接着执行咱们自己的右键弹出菜单逻辑
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (selectedInfo.CharacterId == User.Instance.CurrentCharacter.Id) return;

            var interacterWindow = UIManager.Instance.Show<UIGuildPlayerInteract>();
            interacterWindow.SetupInteractMenu(this.selectedInfo);
        }
    }

}
