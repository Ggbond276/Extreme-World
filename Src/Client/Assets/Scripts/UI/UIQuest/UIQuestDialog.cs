using Assets.Scripts.Models;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIQuestDialog : UIWindow
{
    public Quest targetQuest; // 当前对话框正在处理的任务实体引用

    [Header("任务信息渲染")]
    public Text dialogText; // NPC对话内容文本组件
    public Text target; // 任务目标文本组件
    public Image rewardItem1; // 奖励道具插槽 1
    public Image rewardItem2; // 奖励道具插槽 2
    public Image rewardItem3; // 奖励道具插槽 3
    public Text money; // 奖励金币文本组件
    public Text exp; // 奖励经验文本组件
    [Header("按钮组件")]
    public GameObject openButton; // 接取任务按钮节点
    public GameObject submitButton; // 交付任务按钮节点


    /// <summary>
    /// 将传入的任务数据绑定到对话框 UI，并根据任务当前状态决定按钮的显隐。
    /// 执行后：对话文本、奖励槽位被安全刷新。如果是可接取状态，显示接取按钮；如果是可交付状态，显示交付按钮。
    /// </summary>
    public void SetQuest(Quest targetQuest)
    {
        if (targetQuest == null || targetQuest.Define == null) // 第一层拦截：目标为空，或者策划漏配导致 Define 为空，直接阻断
            return;

        this.targetQuest = targetQuest; // 缓存当前任务实体引用

        target.text = this.targetQuest.Define.OverView ?? ""; // 渲染任务目标：使用 ?? 兜底，防止策划漏配导致抛出空引用异常
        money.text = this.targetQuest.Define.RewardGold.ToString(); // 渲染金币奖励：值类型 ToString 绝对安全
        exp.text = this.targetQuest.Define.RewardExp.ToString(); // 渲染经验奖励：值类型 ToString 绝对安全

        SetRewardItem(rewardItem1, this.targetQuest.Define.RewardItem1); // 道具槽 1 安全加载：自动处理显隐与越界
        SetRewardItem(rewardItem2, this.targetQuest.Define.RewardItem2); // 道具槽 2 安全加载：自动处理显隐与越界
        SetRewardItem(rewardItem3, this.targetQuest.Define.RewardItem3); // 道具槽 3 安全加载：自动处理显隐与越界


        if (targetQuest.Info != null && 
            targetQuest.Info.Status == QuestStatus.Complated) // 分支 A：任务已达成，处于可交付状态
        {
            dialogText.text = this.targetQuest.Define.DialogFinish ?? this.targetQuest.Define.Dialog ?? ""; // 优先读取交付文案，若无则用常规对话兜底，最后用空字符串兜底

            openButton.SetActive(false); // 隐藏接取按钮
            submitButton.SetActive(true); // 显示交付按钮
        }
        else if (targetQuest.Info == null) // 分支 B：任务未接取，处于可接取状态
        {
            dialogText.text = this.targetQuest.Define.Dialog ?? ""; // 读取配置表中的接取常规对话文案

            openButton.SetActive(true); // 显示接取按钮
            submitButton.SetActive(false); // 隐藏交付按钮
        }
        else // 分支 C：兜底状态拦截（如 InProgress 任务中途意外触发了对话框
        {
            openButton.SetActive(false); // 强制隐藏接取按钮
            submitButton.SetActive(false); // 强制隐藏交付按钮
        }
    }

    /// <summary>
    /// 安全加载指定奖励道具图标并处理 UI 槽位的显隐状态。
    /// 执行后：若道具合法，对应槽位激活并贴上图标；若未配置，该 Image 节点自动隐藏。
    /// </summary>
    private void SetRewardItem(Image itemImage, int itemId)
    {
        if (itemImage == null) return; // 容错拦截：Inspector 面板未拖拽绑定时跳过

        // 只有配了道具ID（假设有效ID > 0），并且在字典里能安全查到，才加载图标
        if (itemId > 0 && DataManager.Instance.Items.TryGetValue(itemId, out var itemDefine)) // 业务校验：ID合法且能在本地字典中检索到静态数据
        {
            itemImage.overrideSprite = Resloader.Load<Sprite>(itemDefine.Icon); // 同步加载 Sprite 资源并覆盖显示
            itemImage.gameObject.SetActive(true); // 激活道具展示节点
        }
        else // 分支：未配奖励或填写了非法 ID
        {
            itemImage.gameObject.SetActive(false); // 隐藏对应的 Image 节点，避免UI残留空白框
        }
    }

    /// <summary>
    /// 玩家点击【拒绝】按钮的回调。
    /// 执行后：当前 UI 面板被关闭，并弹出一个中心飘字提示框播放拒绝文案。
    /// </summary>
    public void OnClickRefuseButton()
    {
        this.OnCloseClick(); // 关闭当前任务对话面板
        string defaultMsg = "等你下次再来哦"; // 设置保底文案
        string msg = string.IsNullOrEmpty(targetQuest.Define.DialogAccept) ? defaultMsg : targetQuest.Define.DialogDeny;
        MessageBox.Show(msg); // 呼出全局飘字 UI 播放提示
    }

    /// <summary>
    /// 玩家点击【接取】按钮的回调。
    /// 执行后：向 Model 层(QuestManager)发起接取请求，随后关闭面板并飘字提示。
    /// </summary>
    public void OnClickAcceptButton()
    {
        if (targetQuest != null) // 安全校验，确保当前绑定了有效任务
        {
            QuestManager.Instance.AcceptQuest(this.targetQuest); // 调用业务逻辑层，向下层网络服务发送接取数据包
            this.OnCloseClick(); // 关闭当前任务对话面板
            string defaultMsg = "你已接受任务 快去完成吧"; // 设置保底文案
            string msg = string.IsNullOrEmpty(targetQuest.Define.DialogAccept) ? defaultMsg : targetQuest.Define.DialogAccept;
            MessageBox.Show(msg); // 呼出全局飘字 UI 播放提示
        }
    }

    /// <summary>
    /// 玩家点击【完成/交付】按钮的回调。
    /// 执行后：向 Model 层(QuestManager)发起交付请求，随后关闭面板并飘字提示。
    /// </summary>
    public void OnClickSubmitButton()
    {
        if (targetQuest != null) // 安全校验，确保当前绑定了有效任务
        {
            QuestManager.Instance.SubmitQuest(this.targetQuest); // 调用业务逻辑层，向下层网络服务发送交付请求（等待服务器验证发奖）
            this.OnCloseClick(); // 关闭当前任务对话面板
            string defaultMsg = "任务已经提交啦 快去接收新任务吧"; // 设置保底文案
            string msg = string.IsNullOrEmpty(targetQuest.Define.DialogFinish) ? defaultMsg : targetQuest.Define.DialogFinish;
            MessageBox.Show(msg); // 呼出全局飘字 UI 播放提示
        }
    }
    
}
