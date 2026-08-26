using Assets.Scripts.Services;
using Assets.Scripts.UI.UIChat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIChatInteract : UIWindow
{
    [Header("")]
    public Transform panelMain;
    public Button buttonChat;
    public Button buttonAddFriend;
    public Button buttonTeam;

    private int targetId;
    private string targetName;

    // 把它移到这里！
    void Start()
    {
        if (buttonChat != null) buttonChat.onClick.AddListener(this.OnClickPrivateChat);
        if (buttonAddFriend != null) buttonAddFriend.onClick.AddListener(this.OnClickAddFriend);
        if (buttonTeam != null) buttonTeam.onClick.AddListener(this.OnClickInviteTeam);
    }

    public void Setup(int id, string name)
    {
        this.targetId = id;
        this.targetName = name;

        if(panelMain != null)
        {
            Vector3 mousePos = Input.mousePosition;
            panelMain.position = new Vector3(mousePos.x + 20f, mousePos.y - 20f, mousePos.z);

            // 强制刷新一下组件
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panelMain);
        }
    }

    /// <summary>
    /// 发起私聊
    /// </summary>
    public void OnClickPrivateChat()
    {
        // 唤醒聊天面板并强制切到私聊频道
        UIChat chatUI = UIManager.Instance.Show<UIChat>();
        if (chatUI != null)
        {
            chatUI.StartPrivateChat(targetId, targetName);
        }
        this.Close(); // 关闭当前交互弹窗
    }

    /// <summary>
    /// 添加好友
    /// </summary>
    public void OnClickAddFriend()
    {
        // 严格遵守日志规范
        Debug.LogFormat("OnClickAddFriend : 发起添加好友请求, TargetId:{0}, TargetName:{1}", targetId, targetName);

        FriendService.Instance.SendAddRequest(targetId, targetName);

        this.Close();
    }

    /// <summary>
    /// 邀请组队
    /// </summary>
    public void OnClickInviteTeam()
    {
        // 严格遵守日志规范
        Debug.LogFormat("OnClickInviteTeam : 发起组队邀请, TargetId:{0}, TargetName:{1}", targetId, targetName);

        TeamService.Instance.SendTeamInviteRequest(targetId, targetName);

        this.Close();
    }
    
}
