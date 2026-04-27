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
        if (quest == null) return;
        QuestDefine questDefine = quest.Define;
        this.description.text = questDefine.Name;
        this.target.text = questDefine.OverView;
        this.reward1.overrideSprite = Resloader.Load<Sprite>(DataManager.Instance.Items[questDefine.RewardItem1].Icon);
        this.reward2.overrideSprite = Resloader.Load<Sprite>(DataManager.Instance.Items[questDefine.RewardItem2].Icon);
        this.reward3.overrideSprite = Resloader.Load<Sprite>(DataManager.Instance.Items[questDefine.RewardItem3].Icon);
        this.rewardMoney.text = questDefine.RewardGold.ToString();
        this.rewardEXP.text = questDefine.RewardExp.ToString();
    }
}
