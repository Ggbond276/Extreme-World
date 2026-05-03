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
        if (targetQuest == null)
            return;
        this.targetQuest = targetQuest;
        dialogText.text = this.targetQuest.Define.Dialog;
        target.text = this.targetQuest.Define.OverView;
        rewardItem1.overrideSprite = Resloader.Load<Sprite>(DataManager.Instance.Items[this.targetQuest.Define.RewardItem1].Icon);
        rewardItem2.overrideSprite = Resloader.Load<Sprite>(DataManager.Instance.Items[this.targetQuest.Define.RewardItem2].Icon);
        rewardItem3.overrideSprite = Resloader.Load<Sprite>(DataManager.Instance.Items[this.targetQuest.Define.RewardItem3].Icon);
        money.text = this.targetQuest.Define.RewardGold.ToString();
        exp.text = this.targetQuest.Define.RewardExp.ToString(); 

        if(targetQuest.Info != null && targetQuest.Info.Status == QuestStatus.Complated)
        {
            openButton.SetActive(false);
            submitButton.SetActive(true);
        }
        if(targetQuest.Info == null)
        {
            openButton.SetActive(true);
            submitButton.SetActive(false);
        }

    }

    public void OnClickRefuseButton()
    {
        this.OnCloseClick();
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
