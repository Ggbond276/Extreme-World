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
    public Text description; // 任务名称/描述展示文本组件

    [Header("任务目标")]
    public Text target; // 任务目标/概述展示文本组件

    [Header("任务奖励物品")]
    public Image reward1; // 奖励道具插槽 1
    public Image reward2; // 奖励道具插槽 2
    public Image reward3; // 奖励道具插槽 3

    [Header("列表容器")]
    public Text rewardMoney; // 金币奖励展示文本组件
    public Text rewardEXP; // 经验奖励展示文本组件


    /// <summary>
    /// 将任务实体的配置数据绑定并渲染到任务详情面板上。
    /// 执行后：面板文本（名称、目标、金币、经验）被更新赋值，奖励道具槽位根据配置动态加载图标或自动隐藏空白槽位。
    /// </summary>
    internal void SetQuestInfo(Quest quest)
    {
        if (quest == null || quest.Define == null) return; // 基础校验：若任务对象或配表定义为空则直接阻断，防止空引用崩溃

        QuestDefine questDefine = quest.Define; // 提取静态配置数据引用

        this.description.text = questDefine.Name ?? ""; // 渲染任务标题：采用空合并操作符兜底，防止策划未配字段引发异常
        this.target.text = questDefine.OverView ?? "";  // 渲染任务概述：采用空合并操作符兜底，防止空文本显示异常

        this.rewardMoney.text = questDefine.RewardGold.ToString(); // 渲染金币奖励：值类型安全转为字符串
        this.rewardEXP.text = questDefine.RewardExp.ToString(); // 渲染经验奖励：值类型安全转为字符串

        SetRewardItem(this.reward1, questDefine.RewardItem1); // 处理奖励道具槽位 1 的渲染与显隐
        SetRewardItem(this.reward2, questDefine.RewardItem2); // 处理奖励道具槽位 2 的渲染与显隐
        SetRewardItem(this.reward3, questDefine.RewardItem3); // 处理奖励道具槽位 3 的渲染与显隐
    }

    /// <summary>
    /// 安全加载指定奖励道具图标并处理 UI 槽位的显隐状态。
    /// 执行后：若道具合法且能查到配置，对应槽位激活并贴上图标 Sprite；若道具未配置或 ID 非法，槽位 GameObject 自动隐藏。
    /// </summary>
    private void SetRewardItem(Image itemImage, int itemId)
    {
        if (itemImage == null) return; // 容错拦截：Inspector 面板未拖拽绑定对应 Image 组件时跳过，避免报空

        if (itemId > 0 && DataManager.Instance.Items.TryGetValue(itemId, out var itemDefine)) // 业务校验：道具 ID 合法且能在配置字典中检索到静态数据
        {
            itemImage.overrideSprite = Resloader.Load<Sprite>(itemDefine.Icon); // 通过资源加载器同步加载 Sprite 并覆盖展示
            itemImage.gameObject.SetActive(true); // 数据合法且加载成功，激活道具展示节点
        }
        else // 分支：未配置奖励（如 ID 为 0）或配表数据不存在
        {
            itemImage.gameObject.SetActive(false); // 隐藏未生效的道具图标节点，避免界面残留空白占位框
        }
    }
}
