using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FriendManager : Singleton<FriendManager>
{
    // Manager持有的friends数据池 
    public List<NFriendInfo> allFriends = new List<NFriendInfo>();

    // 核心解耦事件：数据发生改变时全局广播
    public UnityAction OnFriendDataChanged;

    /// <summary>
    ///  OnGameEnter的时候就把数据全部都传入进来了
    /// </summary>
    /// <param name="friends"></param>
    public void Init(List<NFriendInfo> friends)
    {
        this.allFriends = friends;
    }


    /// <summary>
    /// 全量覆盖核心代码（friends是Service传来的新数据）
    /// </summary>
    /// <param name="friends"></param>
    public void UpdateFriendList(List<NFriendInfo> friends)
    {
        // 粗暴全量覆盖
        this.allFriends = friends;

        // 通知所有监听的组件，好友数据更新了，去做相应的处理
        if(OnFriendDataChanged != null)
        {
            this.OnFriendDataChanged.Invoke();
        }
    }




    /// <summary>
    /// 对方同意了我们的好友添加请求
    /// </summary>
    /// <param name="fromName"></param>
    internal void OnAddFriendSuccess()
    {
        MessageBox.Show("好友添加成功！", "添加好友");
    }

    /// <summary>
    /// 对方拒绝了我们的好友添加请求
    /// </summary>
    /// <param name="fromName"></param>
    internal void OnAddFriendFailed(string reason)
    {
        MessageBox.Show(string.IsNullOrEmpty(reason) ? "添加好友失败" : reason, "添加好友", MessageBoxType.Error);
    }

    /// <summary>
    /// 对方发送好友添加请求给我们
    /// </summary>
    /// <param name="fromName"></param>
    internal void OnReciveFriendRequest(FriendAddRequest request)
    {
        // 选择同意需要让Service发送请求, 选择不同意也需要下放请求
        var confirm = MessageBox.Show(string.Format("{0}请求添加您为好友", request.FromName), "添加好友", MessageBoxType.Confirm, "同意", "拒绝" );
        confirm.OnYes = () =>
        {
            FriendService.Instance.SendAddResponse(true, request);
        };
        confirm.OnNo = () =>
        {
            FriendService.Instance.SendAddResponse(false, request);
        };
    }

    /// <summary>
    ///  删除好友成功
    /// </summary>
    internal void OnFriendRemoveSuccess()
    {
        MessageBox.Show("删除好友成功", "删除好友");
    }

    /// <summary>
    /// 删除好友失败
    /// </summary>
    /// <param name="errormsg"></param>
    internal void OnFriendRemoveFailed(string reason)
    {
        MessageBox.Show(string.IsNullOrEmpty(reason) ? "删除好友成功" : reason, "删除好友", MessageBoxType.Error);
    }
}
