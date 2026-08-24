using Assets.Scripts.Managers;
using Models;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGuildPlayerInteract : UIWindow
{
    [Header("操作按钮绑定")]
    public Button btnChat;
    public Button btnTransferLeader;
    public Button btnAppointVice;
    public Button btnAppointMember;
    public Button btnKick;

    [Header("核心面板")]
    public Transform panelMain;

    private NGuildMember targetMember;


    void Start()
    {
        if (btnChat != null) btnChat.onClick.AddListener(OnClickChat);
        if (btnTransferLeader != null) btnTransferLeader.onClick.AddListener(OnClickTransfer);
        if (btnAppointVice != null) btnAppointVice.onClick.AddListener(OnClickAppointVice);
        if (btnAppointMember != null) btnAppointMember.onClick.AddListener(OnClickAppointMember);
        if (btnKick != null) btnKick.onClick.AddListener(OnClickKick);
    }


    public void SetupInteractMenu(NGuildMember target)
    {
        this.targetMember = target;
        RefreshPermissionUI();

        if (panelMain != null)
        {
            Vector3 mousePos = Input.mousePosition;
            panelMain.position = new Vector3(mousePos.x + 20f, mousePos.y - 20f, mousePos.z);
        }
    }

    public void RefreshPermissionUI()
    {
        int myId = User.Instance.CurrentCharacter.Id;
        GuildPosition myPosition = GuildManager.Instance.MyMembers[myId].Position;
        GuildPosition targetPosition = targetMember.Position;

        bool IAmLeader = (myPosition == GuildPosition.GuildPositionLeader);
        bool IAmVice = (myPosition == GuildPosition.GuildPositionViceLeader);

        if (btnChat != null)
            btnChat.gameObject.SetActive(true);
        if (btnTransferLeader != null)
            btnTransferLeader.gameObject.SetActive(IAmLeader);
        if (btnAppointVice != null)
            btnAppointVice.gameObject.SetActive(IAmLeader && targetPosition == GuildPosition.GuildPositionMember);
        if (btnAppointMember != null)
            btnAppointMember.gameObject.SetActive(IAmLeader && targetPosition == GuildPosition.GuildPositionViceLeader);
        if (btnKick != null)
            btnKick.gameObject.SetActive(IAmLeader || (IAmVice && targetPosition == GuildPosition.GuildPositionMember));
    }


    /// <summary>
    /// 交互事件：点击私聊
    /// </summary>
    private void OnClickChat()
    {
        // TODO: 为明天的聊天系统预留的接口
        // 预期逻辑：呼出 UIManager.Instance.Show<UIChat>() -> 自动切到私聊频道 -> 将 targetMember 的名字或 ID 传给聊天系统
        Debug.Log($"准备与 {targetMember.Name} 建立私聊频道...");

        this.Close();
    }

    /// <summary>
    /// 转让会长
    /// </summary>
    private void OnClickTransfer()
    {
        GuildManager.Instance.AdminMember(targetMember.CharacterId, GuildAdminCommand.CommandTransferLeader);
    }

    /// <summary>
    /// 任命副会长
    /// </summary>
    private void OnClickAppointVice()
    {
        GuildManager.Instance.AdminMember(targetMember.CharacterId, GuildAdminCommand.CommandPromoteVice);
        this.Close();
    }

    /// <summary>
    /// 卸任管理层
    /// </summary>
    private void OnClickAppointMember()
    {
        GuildManager.Instance.AdminMember(targetMember.CharacterId, GuildAdminCommand.CommandDemoteNormal);
        this.Close();
    }

    /// <summary>
    /// 踢人
    /// </summary>
    private void OnClickKick()
    {
        GuildManager.Instance.AdminMember(targetMember.CharacterId, GuildAdminCommand.CommandKickMember);
        this.Close();
    }









}
