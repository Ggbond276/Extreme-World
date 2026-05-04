using Assets.Scripts.Models;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIQuestDialog : UIWindow
{
    public Quest targetQuest;

    [Header("任务信息渲染")]
    public Text dialogText;
    public Text target;
    public Image rewardItem1;
    public Image rewardItem2;
    public Image rewardItem3;
    public Text money;
    public Text exp;
    [Header("按钮组件")]
    public GameObject openButton;
    public GameObject submitButton;

    public void SetQuest(Quest targetQuest)
    {
        // 第一层排空：目标为空，或者策划把配表整行删了导致 Define 为空
        if (targetQuest == null || targetQuest.Define == null)
            return;

        this.targetQuest = targetQuest;

        // 基础文本排空 (使用 ?? 兜底，防止策划没填文案导致报空)
        target.text = this.targetQuest.Define.OverView ?? "";
        money.text = this.targetQuest.Define.RewardGold.ToString();
        exp.text = this.targetQuest.Define.RewardExp.ToString();

        //  道具安全加载：使用独立方法处理，防报错 + 隐藏空槽位
        SetRewardItem(rewardItem1, this.targetQuest.Define.RewardItem1);
        SetRewardItem(rewardItem2, this.targetQuest.Define.RewardItem2);
        SetRewardItem(rewardItem3, this.targetQuest.Define.RewardItem3);

        // 动态按钮与剧情排空
        if (targetQuest.Info != null && targetQuest.Info.Status == QuestStatus.Complated)
        {
            // 如果完成了，优先读交付对话（如果策划没配交付对话，就拿普通对话兜底，再为空就留白）
            dialogText.text = this.targetQuest.Define.DialogFinish ?? this.targetQuest.Define.Dialog ?? "";

            openButton.SetActive(false);
            submitButton.SetActive(true);
        }
        else if (targetQuest.Info == null)
        {
            // 可接取状态
            dialogText.text = this.targetQuest.Define.Dialog ?? "";

            openButton.SetActive(true);
            submitButton.SetActive(false);
        }
        else
        {
            // 兜底状态（比如 InProgress 催更状态被意外打开了面板）
            openButton.SetActive(false);
            submitButton.SetActive(false);
        }
    }

    /// <summary>
    /// 专门处理奖励图标的安全加载
    /// </summary>
    private void SetRewardItem(Image itemImage, int itemId)
    {
        if (itemImage == null) return;

        // 只有配了道具ID（假设有效ID > 0），并且在字典里能安全查到，才加载图标
        if (itemId > 0 && DataManager.Instance.Items.TryGetValue(itemId, out var itemDefine))
        {
            itemImage.overrideSprite = Resloader.Load<Sprite>(itemDefine.Icon);
            itemImage.gameObject.SetActive(true); // 显示图标
        }
        else
        {
            // 如果配表里没填奖励（比如填了0），或者填了错的ID，直接隐藏这块Image
            itemImage.gameObject.SetActive(false);
        }
    }

    public void OnClickRefuseButton()
    {
        this.OnCloseClick();
        string defaultMsg = "等你下次再来哦";
        string msg = string.IsNullOrEmpty(targetQuest.Define.DialogAccept) ? defaultMsg : targetQuest.Define.DialogDeny;
        MessageBox.Show(msg);
    }

    public void OnClickAcceptButton()
    {
        if (targetQuest != null)
        {
            QuestManager.Instance.AcceptQuest(this.targetQuest);
            this.OnCloseClick();
            string defaultMsg = "你已接受任务 快去完成吧";
            string msg = string.IsNullOrEmpty(targetQuest.Define.DialogAccept) ? defaultMsg : targetQuest.Define.DialogAccept;
            MessageBox.Show(msg);
        }
    }   

    public void OnClickSubmitButton()
    {
        if (targetQuest != null)
        {
            QuestManager.Instance.SubmitQuest(this.targetQuest);
            this.OnCloseClick();
            string defaultMsg = "任务已经提交啦 快去接收新任务吧";
            string msg = string.IsNullOrEmpty(targetQuest.Define.DialogFinish) ? defaultMsg : targetQuest.Define.DialogFinish;
            MessageBox.Show(msg);
        }
    }
    
}
