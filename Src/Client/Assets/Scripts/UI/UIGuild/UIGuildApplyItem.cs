using Assets.Scripts.Managers;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGuildApplyItem : ListViewItem
{
    [Header("待渲染组件")]
    public TextMeshProUGUI textRankValue;
    public TextMeshProUGUI textNameValue;
    public TextMeshProUGUI textLevelValue;
    public TextMeshProUGUI textClassValue;
    public Image imageClassIcon;
    public TextMeshProUGUI textStatusValue;
    public Image imageStatusIcon;

    [Header("操作按钮")]
    public Button buttonAgree;
    public Button buttonDisagree;

    [Header("状态表现")]
    public Image ImageBg;
    public Sprite normalBg;
    public Sprite selectedBg;

    public NGuildApply info;

    internal void SetApplyInfo(NGuildApply apply, int rank)
    {
        this.info = apply;

        if (textRankValue != null) textRankValue.text = rank.ToString();
        if (textNameValue != null) textNameValue.text = apply.Name;
        if (textLevelValue != null) textLevelValue.text = apply.Level.ToString();

        // 职业表现
        if (textClassValue != null)
        {
            string characterClass;
            switch (apply.ClassType)
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

        if (imageClassIcon != null)
        {
            imageClassIcon.overrideSprite = SpriteManager.Instance.GetClassSprite((CharacterClass)apply.ClassType);
        }

        // 在线状态表现 (复用 Member 的颜色逻辑)
        if (textStatusValue != null && imageStatusIcon != null)
        {
            bool isOnline = apply.IsOnline != 0;

            ColorUtility.TryParseHtmlString("#246C24", out Color onlineColor);
            ColorUtility.TryParseHtmlString("#808080", out Color offlineColor);

            textStatusValue.text = isOnline ? "在线" : "离线";
            textStatusValue.color = isOnline ? onlineColor : offlineColor;
            imageStatusIcon.color = isOnline ? onlineColor : offlineColor;
        }
    }

    public override void OnSelected(bool selected)
    {
        if (this.selectedBg != null && this.ImageBg != null)
        {
            this.ImageBg.overrideSprite = selected ? selectedBg : normalBg;
        }
    }

    // 绑定给同意按钮的 OnClick 事件
    public void OnClickAgree()
    {
        Debug.Log($"同意玩家 {info.Name} 的入会申请");
        // TODO: 向服务器发送 GuildApplyProcessCommand.ACCEPT
    }

    // 绑定给拒绝按钮的 OnClick 事件
    public void OnClickDisagree()
    {
        Debug.Log($"拒绝玩家 {info.Name} 的入会申请");
        // TODO: 向服务器发送 GuildApplyProcessCommand.REJECT
    }

}
