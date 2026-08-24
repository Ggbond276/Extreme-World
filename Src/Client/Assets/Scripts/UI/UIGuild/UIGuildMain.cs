using Assets.Scripts.Managers;
using Models;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGuildMain : UIWindow
{
    [Header("大盘信息区 (左侧)")]
    public TextMeshProUGUI textGuildName;      // 公会名称 (例如 [苍穹之光])
    public TextMeshProUGUI textGuildId;        // 公会 ID
    public TextMeshProUGUI textLeaderName;     // 会长名字
    public TextMeshProUGUI textMemberCount;    // 人数显示 (例如 88/100)
    public InputField inputNotice;             // 公会宗旨 (只有会长能编辑，普通成员设为 ReadOnly)
    public Button btnSaveSetting;              // 保存设置按钮 (只有会长可见)

    [Header("页签切换区")]
    public TabView tabView;
    public Button btnApplyListTab;

    [Header("列表容器区")]
    public ListView memberListView; // 存放 MemberItem 的 Content 节点
    public ListView applyListView;  // 存放 ApplyItem 的 Content 节点

    [Header("底部操作按钮区")]
    public Button btnLeave;             // 离开/解散公会按钮
    public TextMeshProUGUI textBtnLeave;        // 离开按钮上的文字 (离开公会 / 解散公会)

    [Header("预制体")]
    public GameObject memberItemPrefab;
    public GameObject applyItemPrefab;

    [Header("进度条组件")]
    public Image imageProgressFill;

    [Header("搜索组件")]
    public TMP_InputField inputFieldSearch;
    public Button btnSearch;


    // ====================1. 生命周期与事件注册模块======================


    /// <summary>
    /// 面板初始化：拉取大盘信息、布局权限UI、绑定按钮事件并注册网络数据监听
    /// </summary>
    private void Start()
    {
        UpdateBaseInfo();
        UpdatePermissionUI();

        if (btnLeave != null) btnLeave.onClick.AddListener(OnClickLeave);
        if (btnSaveSetting != null) btnSaveSetting.onClick.AddListener(OnClickSaveSetting);
        if (btnSearch != null) btnSearch.onClick.AddListener(OnClickButtonSearch);
        
        RefreshMemberList();
        RefreshApplyList();

        GuildManager.Instance.OnGuildInfoChanged += OnGuildInfoChanged;
        GuildManager.Instance.OnGuildMemberChanged += OnGuildMemberChanged;
        GuildManager.Instance.OnGuildApplyChanged += OnGuildApplyChanged;
    }
    /// <summary>
    /// 面板销毁：强制注销所有绑定的网络事件，防止野指针和内存泄漏
    /// </summary>
    private void OnDestroy()
     {
        if (GuildManager.Instance != null)
        {
            GuildManager.Instance.OnGuildInfoChanged -= OnGuildInfoChanged;
            GuildManager.Instance.OnGuildMemberChanged -= OnGuildMemberChanged;
            GuildManager.Instance.OnGuildApplyChanged -= OnGuildApplyChanged;
        }
     }


    // =====================2. 权限校验模块=====================


    /// <summary>
    /// 权限判定：检查当前玩家是否为本公会的会长
    /// </summary>
    /// <returns>如果是会长返回 true，否则返回 false</returns>
    private bool IsLeader()
    {
        int myId = User.Instance.CurrentCharacter.Id;

        if (GuildManager.Instance.MyMembers.ContainsKey(myId))
        {
            GuildPosition position = GuildManager.Instance.MyMembers[myId].Position;
            return position == GuildPosition.GuildPositionLeader;
        }

        return false;


    }
    /// <summary>
    /// 权限判定：检查当前玩家是否为管理层 (此处设定为副会长)
    /// </summary>
    /// <returns>如果是副会长返回 true，否则返回 false</returns>
    private bool IsManager()
    {
        if (GuildManager.Instance.MyGuildInfo == null) return false;
        int myId = User.Instance.CurrentCharacter.Id;

        if(GuildManager.Instance.MyMembers.ContainsKey(myId))
        {
            GuildPosition position = GuildManager.Instance.MyMembers[myId].Position;
            return position == GuildPosition.GuildPositionViceLeader;
        }

        return false;
    }


    // ===================3. UI 渲染与数据更新模块=======================


    /// <summary>
    /// 基础渲染：刷新左侧面板的公会大盘信息与进度条显示
    /// </summary>
    private void UpdateBaseInfo()
    {
        var info = GuildManager.Instance.MyGuildInfo;
        if (info == null) return;

        // 基础信息刷新
        if (textGuildName != null) textGuildName.text = string.Format("[{0}]", info.Name);
        if (textGuildId != null) textGuildId.text = info.Id.ToString();
        if (textLeaderName != null) textLeaderName.text = info.LeaderName;
        if (textMemberCount != null) textMemberCount.text = string.Format("{0}/50", info.MemberCount);
        if (inputNotice != null) inputNotice.text = info.Notice;

        // 进度条刷新
        int currentCount = info.MemberCount;
        int maxCount = 50;
        float progress = (float)currentCount / maxCount;
        if (imageProgressFill != null)
            imageProgressFill.fillAmount = progress;

    }
    /// <summary>
    /// 权限路由：根据当前玩家的公会职位，动态隐藏或禁用无权操作的 UI 控件
    /// </summary>
    private void UpdatePermissionUI()
    {
        bool isLeader = IsLeader();
        bool isManager = IsManager();

        if (inputNotice != null) inputNotice.interactable = isLeader;
        if (btnSaveSetting != null) btnSaveSetting.gameObject.SetActive(isLeader);

        if (btnApplyListTab != null) btnApplyListTab.gameObject.SetActive(isManager);

        if(textBtnLeave != null)
            textBtnLeave.text = isLeader ? "解散公会" : "退出公会";

    }
    /// <summary>
    /// 列表重载：销毁现有成员列表条目，并根据本地缓存全量实例化最新条目
    /// </summary>
    private void RefreshMemberList()
    {
        ClearList(memberListView.transform);
        if (memberListView != null) memberListView.RemoveAll();

        var members = GuildManager.Instance.MyMembers;

        foreach (var kv in members)
        {
            GameObject go = Instantiate(memberItemPrefab, memberListView.transform);
            UIGuildMemberItem item = go.GetComponent<UIGuildMemberItem>();
            if(item != null)
            {
                item.SetMemberInfo(kv.Value, kv.Key);
                if (memberListView != null) memberListView.AddItem(item);
            }
        }
    }
    /// <summary>
    /// 列表重载：销毁现有申请列表条目，并根据本地缓存全量实例化最新条目
    /// </summary>
    private void RefreshApplyList()
    {
        ClearList(applyListView.transform);
        if (applyListView != null) applyListView.RemoveAll();
        var applies = GuildManager.Instance.MyApplies;

        foreach(var kv in applies)
        {
            GameObject go = Instantiate(applyItemPrefab, applyListView.transform);
            UIGuildApplyItem item = go.GetComponent<UIGuildApplyItem>();
            if(item != null)
            {
                item.SetApplyInfo(kv.Value, kv.Key);
                if (applyListView != null) applyListView.AddItem(item);
            }
            
        }
    }
    /// <summary>
    /// 渲染工具：物理销毁目标节点下的所有子物体 (用于重置列表容器)
    /// </summary>
    /// <param name="root">需要清空的父节点 Transform</param>
    private void ClearList(Transform root)
    {
        for(int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }


    // ====================4. 交互操作与网络指令下发模块======================


    /// <summary>
    /// 交互事件：点击底部 离开/解散 按钮 (包含二次确认弹窗与权限分流逻辑)
    /// </summary>
    private void OnClickLeave()
    {
        if(IsLeader())
        {
            // 会长调用解散
            UIMessageBox msgBox = MessageBox.Show("您是会长，是否确认【解散】该公会？该操作不可逆转！", "严重警告", MessageBoxType.Confirm, "确认解散", "取消");
            msgBox.OnYes = () =>
            {
                GuildManager.Instance.DisbanGuild();
            };
        } else
        {
            // 普通成员调离开
            UIMessageBox msgbox = MessageBox.Show("确认退出当前公会吗？", "提示", MessageBoxType.Confirm, "退出", "取消");
            msgbox.OnYes = () =>
            {
                GuildManager.Instance.LeaveGuild();
            };
        }
    }
    /// <summary>
    /// 交互事件：点击保存宗旨设置 (仅会长可操作)
    /// </summary>
    private void OnClickSaveSetting()
    {
        string newNotice = inputNotice.text.Trim();
        if (string.IsNullOrEmpty(newNotice))
        {
            MessageBox.Show("宗旨不能为空！");
            return;
        }
        // 注意：这里假设你之前的 reqLevel 在这里不改，先传默认值
        GuildManager.Instance.ModifyGuildSettings(newNotice, GuildManager.Instance.MyGuildInfo.ReqLevel);
    }


    // ====================5. 本地搜索系统模块======================


    /// <summary>
    /// 交互事件：点击放大镜搜索按钮，基于当前激活的 Tab 页签执行不同的搜索域
    /// </summary>
    private void OnClickButtonSearch()
    {
        string keyword = inputFieldSearch.text.Trim();
        if (string.IsNullOrEmpty(keyword))
            MessageBox.Show("请输入要搜索的内容");
        if(tabView.index == 1)
        {
            SearchMembers(keyword);
        }
        else if(tabView.index == 0)
        {
            SearchApplies(keyword);
        }
    }
    /// <summary>
    /// 搜索过滤：根据关键字在本地成员字典中匹配名字，并局部实例化匹配成功的条目
    /// </summary>
    /// <param name="keyword">玩家输入的搜索关键字</param>
    private void SearchMembers(string keyword)
    {
        ClearList(memberListView.transform);
        if (memberListView != null) memberListView.RemoveAll();

        foreach(var kv in GuildManager.Instance.MyMembers)
        {
             if(kv.Value.Name.Contains(keyword))
            {
                GameObject go = Instantiate(memberItemPrefab, memberListView.transform);
                UIGuildMemberItem item = go.GetComponent<UIGuildMemberItem>();
                if(item != null)
                {
                    item.SetMemberInfo(kv.Value, kv.Key);
                    if(memberListView != null) memberListView.AddItem(item);
                }
                
            }
        }
    }
    /// <summary>
    /// 搜索过滤：根据关键字在本地申请字典中匹配名字，并局部实例化匹配成功的条目
    /// </summary>
    /// <param name="keyword">玩家输入的搜索关键字</param>
    private void SearchApplies(string keyword)
    {
        ClearList(applyListView.transform);
        if (applyListView != null) applyListView.RemoveAll();

        foreach(var kv in GuildManager.Instance.MyApplies)
        {
            if(kv.Value.Name.Contains(keyword))
            {
                GameObject go = Instantiate(applyItemPrefab, applyListView.transform);
                UIGuildApplyItem item = go.GetComponent<UIGuildApplyItem>();
                if (item != null)
                {
                    item.SetApplyInfo(kv.Value, kv.Key);
                    if (applyListView != null) applyListView.AddItem(item);
                }
            }
        }
    }


    // ====================6. 全局事件回调监听模块 (Manager 数据源驱动)======================


    /// <summary>
    /// 事件回调：当公会大盘信息变更时触发 (包含自身退出/被踢出公会的边缘自杀情况处理)
    /// </summary>
    private void OnGuildInfoChanged()
    {
      if(!GuildManager.Instance.HasGuild)
        {
            this.Close();
            return;
        }
        UpdateBaseInfo();
        UpdatePermissionUI();
    }
    /// <summary>
    /// 事件回调：当成员列表发生增删改时触发，重新渲染成员列表并刷新总人数大盘
    /// </summary>
    private void OnGuildMemberChanged()
    {
        RefreshMemberList();
        UpdateBaseInfo();
    }
    /// <summary>
    /// 事件回调：当入会申请列表发生增删改时触发，重新渲染审批列表
    /// </summary>
    private void OnGuildApplyChanged()
    {
        RefreshApplyList();
    }

}
