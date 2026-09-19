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
    public ListView listMain; // 主线任务的逻辑列表组件
    public ListView listBranch; // 支线任务的逻辑列表组件

    [Header("渲染插槽")]
    public Transform mainContainer; // 主线任务预制体的实例化物理父节点
    public Transform branchContainer; // 支线任务预制体的实例化物理父节点

    [Header("资源预制体")]
    public GameObject questItemPrefab; // 单条任务UI条目的表现预制体

    [Header("面板类型")]
    private bool showAvaliableList = true; // 状态位：当前是否处于“可接取”任务面板页签

    [Header("引用配置")]
    public TabView tabs; // 侧边栏的页签切换组件
    public UIQuestInfo questInfo; // 右侧的任务详细信息展示面板

    void Start()
    {
        this.tabs.OnTabSelected += OnTabSelected; // 注册页签切换事件
        this.listMain.OnItemSelected += OnQuestSelected; // 注册主线列表项选中事件
        this.listBranch.OnItemSelected += OnQuestSelected;  // 注册支线列表项选中事件
        OnTabSelected(1);  // 默认选中索引1的页签（可接取列表）
    }
     void OnDestroy()
    {
        this.tabs.OnTabSelected -= OnTabSelected; // 注销页签切换事件，防止UI销毁后导致内存泄漏/空引用
        this.listMain.OnItemSelected -= OnQuestSelected; // 注销主线选中事件
        this.listBranch.OnItemSelected -= OnQuestSelected; // 注销支线选中事件
    }

    /// <summary>
    /// 触发当前UI面板的全局刷新。
    /// 执行后：会先清理销毁所有旧的UI条目，再根据当前的页签状态（已接/可接）重新生成对应的任务UI列表。
    /// </summary>
    public void UIRefresh()
    {
        ClearAllQuestList(); // 清空当前渲染的所有列表对象
        InitAllQuestItem(); // 重新从数据层拉取数据并渲染
    }

    /// <summary>
    /// 核心渲染逻辑：遍历全局任务池，通过多层条件拦截，筛选出符合当前页签状态的任务并实例化UI。
    /// 执行后：mainContainer 和 branchContainer 下会实例化挂载出 UIQuestItem 节点，并归入对应的逻辑 List 中。
    /// </summary>
    public void InitAllQuestItem()
    {
        foreach(var kv in QuestManager.Instance.allQuests) // 遍历数据层维护的全局任务池字典
        {
            Quest quest = kv.Value; // 提取具体的任务数据实体

            if (quest.Info != null &&  // 拦截 1：只要任务已经彻底完成，直接踢出渲染列表，不予显示
                quest.Info.Status == QuestStatus.Finished)
            {
                continue;
            }

            if (showAvaliableList) // 分支 A：当前处于【可接取任务】页签
            {
                if (quest.Info != null) // 拦截 2：quest.Info != null 代表任务已经被接取，不属于可接取列表，跳过
                    continue;
            }
            else // 分支 B：当前处于【已接取（进行中）任务】页签
            {
                if (quest.Info == null) // 拦截 3：quest.Info == null 代表任务未被接取，不属于已接取列表，跳过
                    continue;
            }
            
            if(quest.Define.Type == QuestType.Main) // 分支 C：属于主线任务
            {
                RenderQuestItem(quest, mainContainer, listMain); // 实例化到主线UI插槽

            } else if(quest.Define.Type == QuestType.Branch) // 分支 D：属于支线任务
            {
                RenderQuestItem(quest, branchContainer, listBranch); // 实例化到支线UI插槽
            }
        }
    }

    /// <summary>
    /// 实例化单条任务 UI 并绑定底层数据。
    /// 执行后：GameObject被创建并挂载到指定父节点，同时 UIQuestItem 脚本被注入了任务数据，并注册到 ListView 的逻辑容器中。
    /// </summary>
    private void RenderQuestItem(Quest quest, Transform container, ListView list)
    {
        GameObject go = Instantiate(questItemPrefab, container); // 克隆表现层预制体并设置父节点
        UIQuestItem ui = go.GetComponent<UIQuestItem>(); // 获取挂载的单条UI表现组件
        ui.SetQuestItem(quest); // 将底层数据实体注入给UI表现层
        list.AddItem(ui); // 将UI组件添加到列表的逻辑管理容器中
    }

    /// <summary>
    /// 清理所有的任务列表物理节点与逻辑缓存。
    /// 执行后：Hierarchy 场景中的旧节点被 Destroy 销毁，ListView 内部维护的逻辑列表也被清空。
    /// </summary>
    public void ClearAllQuestList()
    {
        this.ClearContainer(mainContainer); // 销毁主线物理节点
        this.ClearContainer(branchContainer); // 销毁支线物理节点
        this.listMain.RemoveAll(); // 清空主线逻辑列表缓存
        this.listBranch.RemoveAll(); // 清空支线逻辑列表缓存
    }

    /// <summary>
    /// 清理指定父节点下的所有物理子对象。
    /// </summary>
    private void ClearContainer(Transform container)
    {
        if (container == null) return; // 安全校验
        for (int i = container.childCount - 1; i >= 0; i--) // 采用倒序遍历删除节点，防止由于索引塌陷导致的越界报错
        {
            Destroy(container.GetChild(i).gameObject); // 彻底销毁克隆的UI GameObject
        }
    }

    /// <summary>
    /// 处理侧边栏页签点击切换事件。
    /// 执行后：修改了 showAvaliableList 状态位，并立即触发 UIRefresh 刷新右侧的主列表渲染。
    /// </summary>
    public void OnTabSelected(int index)
    {
        if (index == 0) // 选中索引 0：切换为【已接任务】模式
            showAvaliableList = false;
        if (index == 1) // 选中索引 1：切换为【可接任务】模式
            showAvaliableList = true;
        this.UIRefresh(); // 状态变更后必须呼叫全局UI刷新
    }


    /// <summary>
    /// 处理任务列表中某一条目被点击选中的事件。
    /// 执行后：实现了主/支线列表的互斥单选效果，并将选中任务的数据传递给右侧 UIQuestInfo 面板进行详情渲染。
    /// </summary>
    public void OnQuestSelected(ListViewItem quest)
    {
        if(quest.Owner == this.listMain) // 如果点击的是【主线列表】里的条目
        {
            this.listBranch.ClearSelection(); // 清除【支线列表】的选中高亮状态，达成跨列表互斥
        }
        else if(quest.Owner == listBranch) // 如果点击的是【支线列表】里的条目
        {
            this.listMain.ClearSelection(); // 清除【主线列表】的选中高亮状态，达成跨列表互斥
        }
        
        UIQuestItem questItem = quest as UIQuestItem; // 安全类型转换
        if (questItem != null && this.questInfo != null) // 确保数据条目和右侧详情面板组件就绪
        {
            this.questInfo.SetQuestInfo(questItem.quest); // 将被选中的任务数据实体推送到右侧详情面板进行UI展示
        }
    }
}
