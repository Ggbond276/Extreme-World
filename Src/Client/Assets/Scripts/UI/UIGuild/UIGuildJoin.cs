using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Models;
using SkillBridge.Message;
using System;
using TMPro;
using UnityEngine.UI;


public class UIGuildJoin : UIWindow
{
    [Header("大盘信息区 (左侧联动显示)")]
    public TextMeshProUGUI textGuildName;
    public TextMeshProUGUI textGuildId;
    public TextMeshProUGUI textGuildLevel;  // 对应切图里的等级
    public TextMeshProUGUI textMemberCount;
    public TextMeshProUGUI textNotice;      // 大厅的宗旨只是看，所以用 Text 即可

    [Header("列表容器区")]
    public ListView guildListView;

    [Header("底部操作按钮区")]
    public Button btnCreate;
    public Button btnJoin;

    [Header("预制体")]
    public GameObject guildItemPrefab;

    [Header("搜索组件")]
    public TMP_InputField inputFieldSearch;
    public Button btnSearch;

    [Header("进度条组件")]
    public Image imageProgressFill;
    
    public NGuildInfo selectedGuildInfo;


    // =========== 1. 生命周期与事件注册模块 =============


    /// <summary>
    /// 面板初始化：绑定按钮交互、注册列表选中回调、监听网络数据同步并主动拉取大厅列表
    /// </summary>
    private void Start()
    {
        if (btnCreate != null) btnCreate.onClick.AddListener(OnClickCreate);
        if (btnJoin != null) btnJoin.onClick.AddListener(OnClickJoin);
        if (btnSearch != null) btnSearch.onClick.AddListener(OnClickButtonSearch);

        // 2. 核心联动：监听 ListView 的选中事件，用来刷新左侧面板
        if (guildListView != null)
        {
            guildListView.OnItemSelected += OnGuildSelected;
        }

        // 3. 监听大厅数据下发事件
        GuildManager.Instance.OnGuildHallListChanged += OnGuildHallListChanged;

        UpdateBaseInfo(null);
        GuildManager.Instance.RefreshGuildHallList();
    }

    /// <summary>
    /// 面板销毁：强制注销网络数据同步监听，防止内存泄漏和空引用异常
    /// </summary>
    private void OnDestroy()
    {
        if (GuildManager.Instance != null)
        {
            GuildManager.Instance.OnGuildHallListChanged -= OnGuildHallListChanged;
        }
    }


    // =========== 2. UI渲染与联动更新模块 =============


    /// <summary>
    /// 基础渲染：根据传入的公会信息更新左侧联动面板及进度条
    /// </summary>
    /// <param name="info">当前选中的公会信息，传 null 则重置为空白默认状态</param>
    private void UpdateBaseInfo(NGuildInfo info)
    {
        selectedGuildInfo = info;

        if(info == null)
        {
            if (textGuildName != null) textGuildName.text = "请先选择公会";
            if (textGuildId != null) textGuildId.text = "--";
            if (textGuildLevel != null) textGuildLevel.text = "--";
            if (textMemberCount != null) textMemberCount.text = "--/50";
            if (textNotice != null) textNotice.text = "";
            return;
        }

        if (textGuildName != null) textGuildName.text = string.Format("[{0}]", info.Name);
        if (textGuildId != null) textGuildId.text = info.Id.ToString();
        if (textGuildLevel != null) textGuildLevel.text = string.Format("Lv.{0}", info.Level); 
        if (textMemberCount != null) textMemberCount.text = string.Format("{0}/50", info.MemberCount);
        if (textNotice != null) textNotice.text = info.Notice;
        
        if(imageProgressFill != null)
        {
            int currentCount = info.MemberCount;
            int maxCount = 50;
            float progress = (float) currentCount / maxCount;
            imageProgressFill.fillAmount = progress;
        }
    }

    /// <summary>
    /// 列表重载：清空当前列表，并根据大厅缓存数据全量实例化公会条目
    /// </summary>
    private void RefreshGuildList()
    {
        ClearList(guildListView.transform);
        if (guildListView != null) guildListView.RemoveAll();

        var guilds = GuildManager.Instance.GuildHallList;

        foreach(var guild in guilds)
        {
            GameObject go = Instantiate(guildItemPrefab, guildListView.transform);
            UIGuildJoinItem item = go.GetComponent<UIGuildJoinItem>();
            if(item != null)
            {
                item.SetGuildInfo(guild, guild.Id);
                if (guildListView != null) guildListView.AddItem(item);
            }
        }
    }
    /// <summary>
    /// 渲染工具：物理销毁目标节点下的所有子物体
    /// </summary>
    /// <param name="root">需要清空的父节点 Transform</param>
    private void ClearList(Transform root)
    {

        for(int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }


    // =========== 3. 交互操作与事件响应模块 =============


    /// <summary>
    /// 列表回调：当右侧列表中的某一项被选中时触发，驱动左侧面板刷新
    /// </summary>
    /// <param name="item">被选中的通用 ListViewItem 组件</param>
    private void OnGuildSelected(ListViewItem item)
    {
        UIGuildJoinItem joinItem = item as UIGuildJoinItem;
        if (joinItem != null && joinItem.info != null)
            UpdateBaseInfo(joinItem.info);
    }

    /// <summary>
    /// 交互事件：点击创建公会，打开创建公会弹窗
    /// </summary>
    private void OnClickCreate()
    {
        UIManager.Instance.Show<UIGuildCreate>();
    }
    /// <summary>
    /// 交互事件：点击申请加入，向服务端投递入会申请
    /// </summary>
    private void OnClickJoin()
    {
        if(selectedGuildInfo == null)
        {
            MessageBox.Show("请现在列表中选择一个公会");
            return;
        }

        GuildManager.Instance.ApplyGuild(selectedGuildInfo.Id);
    }


    // =========== 4. 本地搜索系统模块 =============

    /// <summary>
    /// 交互事件：点击搜索按钮，拦截空输入并分发搜索逻辑
    /// </summary>
    private void OnClickButtonSearch()
    {
        string keyword = inputFieldSearch.text.Trim();
        if (string.IsNullOrEmpty(keyword))
        {
            RefreshGuildList();
            return;
        }

        SearchGuilds(keyword);
    }
    /// <summary>
    /// 搜索过滤：在公会大厅列表中匹配关键字，并局部实例化符合条件的条目
    /// </summary>
    /// <param name="keyword">玩家输入的搜索关键字</param>
    private void SearchGuilds(string keyword)
    {
        ClearList(guildListView.transform);
        if (guildListView != null) guildListView.RemoveAll();

        var guilds = GuildManager.Instance.GuildHallList;

         foreach(var guild in guilds)
        {
            if(guild.Name.Contains(keyword))
            {
                GameObject go = Instantiate(guildItemPrefab, guildListView.transform);
                UIGuildJoinItem item = go.GetComponent<UIGuildJoinItem>();
                if (item != null)
                {
                    item.SetGuildInfo(guild, guild.Id);
                    if (guildListView != null) guildListView.AddItem(item);
                }

            }
        }
    }


    // =========== 5. 全局事件回调监听模块 =============


    /// <summary>
    /// 事件回调：收到服务端大厅列表更新广播，触发本地 UI 刷新
    /// </summary>
    private void OnGuildHallListChanged()
    {
        RefreshGuildList();
    }

  




}
