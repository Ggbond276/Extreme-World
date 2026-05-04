using Assets.Scripts.Models;
using Common.Data;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIQuestSystem : UIWindow
{
    [Header("列表容器")]
    public ListView listMain;
    public ListView listBranch;
   
    [Header("渲染插槽")]
    public Transform mainContainer;
    public Transform branchContainer;

    [Header("资源预制体")]
    public GameObject questItemPrefab;

    [Header("面板类型")]
    private bool showAvaliableList = true;

    [Header("引用配置")]
    public TabView tabs;
    public UIQuestInfo questInfo;

    void Start()
    {
        this.tabs.OnTabSelected += OnTabSelected;
        this.listMain.OnItemSelected += OnQuestSelected;
        this.listBranch.OnItemSelected += OnQuestSelected;
        OnTabSelected(1);
    }
     void OnDestroy()
    {
        this.tabs.OnTabSelected -= OnTabSelected;
        this.listMain.OnItemSelected -= OnQuestSelected;
        this.listBranch.OnItemSelected -= OnQuestSelected;
    }
    public void UIRefresh()
    {
        ClearAllQuestList();
        InitAllQuestItem();
    }

    /// <summary>
    /// 初始化所有的任务列表
    /// </summary>
    public void InitAllQuestItem()
    {
        // 可接取的任务是没有Info的 已接取的任务是有Info的
        foreach(var kv in QuestManager.Instance.allQuests)
        {
            Quest quest = kv.Value;

            // 新增拦截逻辑：只要任务已经彻底完成了，直接踢出渲染列表！不予显示
            if (quest.Info != null && quest.Info.Status == QuestStatus.Finished)
            {
                continue;
            }

            // 展示可接取任务列表 也就是要展示未被接取的任务列表
            if (showAvaliableList)
            {
                // quest.Info == null 代表任务未被接取 quest.Info != null 代表任务已经被接取
                if (quest.Info != null)
                    continue;
            } else
            {
                if (quest.Info == null)
                    continue;
            }
            
            if(quest.Define.Type == QuestType.Main)
            {
                RenderQuestItem(quest, mainContainer, listMain);

            } else if(quest.Define.Type == QuestType.Branch)
            {
                RenderQuestItem(quest, branchContainer, listBranch);
            }
        }
    }
    private void RenderQuestItem(Quest quest, Transform container, ListView list)
    {
        GameObject go = Instantiate(questItemPrefab, container);
        UIQuestItem ui = go.GetComponent<UIQuestItem>();
        ui.SetQuestItem(quest);
        list.AddItem(ui);
    }

    /// <summary>
    /// 清理所有的任务列表
    /// </summary>
    public void ClearAllQuestList()
    {
        this.ClearContainer(mainContainer);
        this.ClearContainer(branchContainer);
        this.listMain.RemoveAll();
        this.listBranch.RemoveAll();
    }
    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for(int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 处理按钮点击切换列表的
    /// </summary>
    public void OnTabSelected(int index)
    {
        if (index == 0)
            showAvaliableList = false;
        if (index == 1)
            showAvaliableList = true;
        this.UIRefresh();
    }
    public void OnQuestSelected(ListViewItem quest)
    {
        if(quest.Owner == this.listMain)
        {
            this.listBranch.ClearSelection();
        } else if(quest.Owner == listBranch)
        {
            this.listMain.ClearSelection();
        }
        
        UIQuestItem questItem = quest as UIQuestItem;
        if (questItem != null && this.questInfo != null)
        {
            this.questInfo.SetQuestInfo(questItem.quest);
        }
    }
}
