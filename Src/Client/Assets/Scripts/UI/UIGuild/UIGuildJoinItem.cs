using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGuildJoinItem : ListViewItem
{
    [Header("待渲染组件")]
    public TextMeshProUGUI textRankValue;          // 排名/序号
    public TextMeshProUGUI textNameValue;          // 公会名字
    public TextMeshProUGUI textLevelValue;         // 公会等级
    public TextMeshProUGUI textMemberValue;        // 成员人数 (截图显示: 45/200)
    public TextMeshProUGUI textActiveValue;        // 活跃度 (截图显示: 高)
    public TextMeshProUGUI textRequirementValue;   // 需求等级 (截图显示: Lv.30+)


    [Header("状态表现")]
    public Image ImageBg;
    public Sprite normalBg;
    public Sprite selectedBg;

    public NGuildInfo info;

    internal void SetGuildInfo(NGuildInfo guildInfo, int rank)
    {
        this.info = guildInfo;

        if (textRankValue != null) textRankValue.text = rank.ToString();
        if (textNameValue != null) textNameValue.text = guildInfo.Name;
        if (textLevelValue != null) textLevelValue.text = guildInfo.Level.ToString();

        // 成员人数拼接 (假设目前 UI 写死最大人数为 200，或者从配置表读取)
        if (textMemberValue != null)
        {
            textMemberValue.text = $"{guildInfo.MemberCount}/200";
        }

        // 活跃度枚举转换
        if (textActiveValue != null)
        {
            string activityStr;
            switch (guildInfo.ActivityLevel)
            {
                case GuildActivityLevel.GuildActivityLow:
                    activityStr = "低";
                    break;
                case GuildActivityLevel.GuildActivityNormal:
                    activityStr = "中";
                    break;
                case GuildActivityLevel.GuildActivityHigh:
                    activityStr = "高";
                    break;
                default:
                    activityStr = "未知";
                    break;
            }
            textActiveValue.text = activityStr;
        }

        // 等级要求拼接
        if (textRequirementValue != null)
        {
            textRequirementValue.text = $"Lv.{guildInfo.ReqLevel}+";
        }
    }

    public override void OnSelected(bool selected)
    {
        if (this.selectedBg != null && this.ImageBg != null)
        {
            this.ImageBg.overrideSprite = selected ? selectedBg : normalBg;
        }
    }

}
