using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.UI;
using Assets.Scripts.UI.UITeam;
using Models;
using Services;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : MonoSingleton<UIMain>
{
    public Text avatarName;
    public Text avatarLevel;

    // 注册监听
    protected override void OnStart()
    {
        this.UpdateAvatar();
        QuestManager.Instance.OnOpenQuestDialog += OnOpenQuestDialog;
        TeamManager.Instance.OnTeamChanged += OnTeamChanged;
        TeamManager.Instance.OnReceiveTeamInvite += OnReceiveTeamInvite;
        TeamManager.Instance.OnShowFloatMessage += OnShowFloatMessage;
    }
    // 注销监听
    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnOpenQuestDialog -= OnOpenQuestDialog;
        if (TeamManager.Instance != null)
        {
            TeamManager.Instance.OnTeamChanged -= OnTeamChanged;
            TeamManager.Instance.OnReceiveTeamInvite -= OnReceiveTeamInvite;
            TeamManager.Instance.OnShowFloatMessage -= OnShowFloatMessage;
        }
           
    }

    void UpdateAvatar()
    {
        this.avatarName.text = string.Format("{0} [{1}]", User.Instance.CurrentCharacter.Name, User.Instance.CurrentCharacter.Id);
        this.avatarLevel.text = User.Instance.CurrentCharacter.Level.ToString();
    }

    public void BackToCharSelect()
    {
        SceneManager.Instance.LoadScene("CharSelect");
        UserService.Instance.SendGameLeave();
    }

    public void OnClickTest()
    {
        UIManager.Instance.Show<UITest>();
    }

    /// <summary>
    /// 打开背包
    /// </summary>
    public void OnClickBag()
    {
        UIManager.Instance.Show<UIBag>();
    }

    /// <summary>
    /// 打开商店1
    /// </summary>
    public void OnClickShop1()
    {
        UIShop shop = UIManager.Instance.Show<UIShop>();
        shop.SetShop(DataManager.Instance.Shops[1]);
    }

    /// <summary>
    /// 打开商店2
    /// </summary>
    public void OnClickShop2()
    {
        UIShop shop = UIManager.Instance.Show<UIShop>();
        shop.SetShop(DataManager.Instance.Shops[2]);
    }

    /// <summary>
    /// 打开装备栏
    /// </summary>
    public void OnClickCharEquip()
    {
        UIManager.Instance.Show<UICharEquip>();
    }

    /// <summary>
    /// 打开任务系统
    /// </summary>
    public void OnClickQuestSystem()
    {
        UIManager.Instance.Show<UIQuestSystem>();
    }

    /// <summary>
    /// 打开任务对话面板
    /// </summary>
    /// <param name="targetQuest"></param>
    public void OnOpenQuestDialog(Quest targetQuest)
    {
         UIQuestDialog dlg = UIManager.Instance.Show<UIQuestDialog>();
         dlg.SetQuest(targetQuest);
    }

    /// <summary>
    /// 打开好友系统面板
    /// </summary>
    public void OnClickFriendSystem()
    {
        UIManager.Instance.Show<UIFriends>();
    }

    /// <summary>
    /// 开关组队面板
    /// </summary>
    public void OnTeamChanged()
    {
         if(TeamManager.Instance.CurrentTeam != null)
        {
            UIManager.Instance.Show<UITeamSystem>();
        } else
        {
            UIManager.Instance.Close(typeof(UITeamSystem));
        }
    }

    /// <summary>
    /// 接收组队系统的全局提示广播，弹出仅包含确认按钮的系统提示框（如：组队成功、离队成功）
    /// </summary>
    /// <param name="message">需要展示的提示文本</param>
    private void OnShowFloatMessage(string message)
    {
        MessageBox.Show(message, "组队", MessageBoxType.Information);
    }

    /// <summary>
    /// 接收服务端的组队邀请广播，弹出带有“同意”和“拒绝”回调选项的交互确认框
    /// 利用 Lambda 表达式的闭包特性，将玩家的选择无缝回传给 TeamManager 
    /// </summary>
    /// <param name="request">包含邀请人名字和ID的原始网络请求包</param>
    private void OnReceiveTeamInvite(TeamInviteRequest request)
    {
        UIMessageBox msgbox = MessageBox.Show($"{request.FromName}邀请您组队", "组队邀请", MessageBoxType.Confirm, "同意", "拒绝");
        msgbox.OnYes = () => {
            TeamManager.Instance.ResponseInvite(true, request);
        };
        msgbox.OnNo = () =>
        {
            TeamManager.Instance.ResponseInvite(false, request);
        };
    }
}   
