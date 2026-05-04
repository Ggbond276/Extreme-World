using Assets.Scripts.Models;
using Common.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIQuestInfo : MonoBehaviour
{
    [Header("任务描述")]
    public Text description;

    [Header("任务目标")]
    public Text target;

    [Header("任务奖励物品")]
    public Image reward1;
    public Image reward2;
    public Image reward3;

    [Header("列表容器")]
    public Text rewardMoney;
    public Text rewardEXP;

    internal void SetQuestInfo(Quest quest)
    {
        // 1. 基础排空：防止任务本身为空，或者策划把整行配表删了导致 Define 为空
        if (quest == null || quest.Define == null) return;

        QuestDefine questDefine = quest.Define;

        // 2. 文本排空：使用 ?? "" 兜底。如果策划没填，就显示空白，绝对不会报空指针
        this.description.text = questDefine.Name ?? "";
        this.target.text = questDefine.OverView ?? "";

        // 3. 数值显示：数值类型（int）就算没填也是 0，ToString() 是绝对安全的
        this.rewardMoney.text = questDefine.RewardGold.ToString();
        this.rewardEXP.text = questDefine.RewardExp.ToString();

        // 4. 道具安全加载：提取成独立方法，既防报错，又能自动隐藏没配奖励的空框框
        SetRewardItem(this.reward1, questDefine.RewardItem1);
        SetRewardItem(this.reward2, questDefine.RewardItem2);
        SetRewardItem(this.reward3, questDefine.RewardItem3);
    }

    /// <summary>
    /// 安全加载奖励图标，并处理 UI 显隐
    /// </summary>
    private void SetRewardItem(Image itemImage, int itemId)
    {
        // 防呆设计：如果 Unity 面板里忘了拖拽这个 Image 组件，直接跳过，不报错
        if (itemImage == null) return;

        // 核心安全逻辑：道具 ID 大于 0，且在字典中安全查到数据
        if (itemId > 0 && DataManager.Instance.Items.TryGetValue(itemId, out var itemDefine))
        {
            itemImage.overrideSprite = Resloader.Load<Sprite>(itemDefine.Icon);
            itemImage.gameObject.SetActive(true); // 数据正常，显示该框框
        }
        else
        {
            // 如果策划没配奖励（比如填了0），或者填了错的ID，直接把这个图片隐藏掉！
            itemImage.gameObject.SetActive(false);
        }
    }
}
