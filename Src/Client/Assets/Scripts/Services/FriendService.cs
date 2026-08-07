using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FriendService : Singleton<FriendService>, IDisposable
{
    // 好友系统的前端需要处理的服务器响应回来的信息有
    // 添加好友响应回来的Response
    // 同意好友请求返回的Response
    // 删除好友请求返回的Response
    // 请求好友列表返回的Response
    public FriendService()
    {
        // 订阅服务器响应
        Debug.LogFormat("FriendService : 初始化并订阅好友系统网络消息");
        MessageDistributer.Instance.Subscribe<FriendAddResponse>(this.OnAddResponse);
        MessageDistributer.Instance.Subscribe<FriendRemoveResponse>(this.OnRemoveResponse);
        MessageDistributer.Instance.Subscribe<FriendListResponse>(this.OnListResponse);
    }

    /// <summary>
    /// 下线前置操作
    /// </summary>
    public void Dispose()
    {
        // 取消订阅服务器响应
        Debug.LogFormat("FriendService : 释放并取消订阅好友系统网络消息");
        MessageDistributer.Instance.Unsubscribe<FriendAddResponse>(this.OnAddResponse);
        MessageDistributer.Instance.Unsubscribe<FriendRemoveResponse>(this.OnRemoveResponse);
        MessageDistributer.Instance.Unsubscribe<FriendListResponse>(this.OnListResponse);
    }

    public void Init() { }

    /// <summary>
    /// 发送好友添加请求
    /// </summary>
    /// <param name="toId"></param>
    /// <param name="toName"></param>
    /// <param name="fromId"></param>
    /// <param name="fromName"></param>
    public void SendAddRequest(int toId, string toName)
    {
        Debug.LogFormat("SendAddRequest : 向服务器发送添加好友请求，目标 ID:{0} Name:{1}, 发送者 ID:{2} Name:{3}",
            toId, toName, User.Instance.CurrentCharacter.Id, User.Instance.CurrentCharacter.Name);
        // 发送好友添加请求至少需要知道要添加谁 网络层只负责打包信息发送 所以需要尽可能精简
        NetMessage message = new NetMessage();
        message.Request = new NetMessageRequest();
        message.Request.friendAdd = new FriendAddRequest();
        message.Request.friendAdd.ToId = toId;
        message.Request.friendAdd.ToName = toName;
        message.Request.friendAdd.FromId = User.Instance.CurrentCharacter.Id;
        message.Request.friendAdd.FromName = User.Instance.CurrentCharacter.Name;
        NetClient.Instance.SendMessage(message);
    }

    /// <summary>
    /// 发送同意与否给请求添加我们为好友的人
    /// </summary>
    /// <param name="isAccept"></param>
    public void SendAddResponse(bool isAccept, FriendAddRequest originRequest)
    {
        Debug.LogFormat("SendAddResponse : 向服务器发送处理好友请求的结果，是否同意:{0}, 对方 ID:{1} Name:{2}",
            isAccept, originRequest.FromId, originRequest.FromName);

        NetMessage message = new NetMessage();
        message.Response = new NetMessageResponse();
        message.Response.friendAdd = new FriendAddResponse();
        message.Response.friendAdd.Result = isAccept ? Result.Success : Result.Failed;
        message.Response.friendAdd.Request = originRequest;
        NetClient.Instance.SendMessage(message);
    }

    /// <summary>
    /// 处理好友添加响应（客户端在这里需要处理两种响应 一种是别人请求添加 一种是我们想要添加别人服务器给我们的响应）
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="response"></param>
    private void OnAddResponse(object sender, FriendAddResponse response)
    {
        // 这里需要分两种情况来处理
        // message中包含三份数据
        //RESULT result = 1;
        //string errormsg = 2;
        //FriendAddRequest request = 3;

        // 我们主动添加别人服务器发来的响应
        if(response.Request == null)
        {
            // message是纯状态信息 是需要提示是成功还是失败即可
            if(response.Result == Result.Success)
            {
                Debug.LogFormat("OnAddResponse : 玩家同意了您的好友请求");
                // 接下来交给Manager去显示弹窗信息
                FriendManager.Instance.OnAddFriendSuccess();
            }
            else if(response.Result == Result.Failed)
            {
                Debug.LogFormat("OnAddResponse : 添加失败：{0}", response.Errormsg);
                // 接下来交给Manager去显示弹窗信息
                FriendManager.Instance.OnAddFriendFailed(response.Errormsg);

            }
        } else // 别人来添加我们转发得到的响应
        {
            Debug.LogFormat("OnAddResponse : 收到来自 ID: {0} 姓名: {1} 的好友请求", response.Request.FromId, response.Request.FromName);
            // 将信息交给Manager去显示弹窗信息
            FriendManager.Instance.OnReciveFriendRequest(response.Request);
        }
    }



    /// <summary>
    /// 发送好友删除请求
    /// </summary>
    /// <param name="requesterId"></param>
    /// <param name="targetId"></param>
    public void SendRemoveRequest(int requesterId, int targetId)
    {
        Debug.LogFormat("SendRemoveRequest : 向服务器发送删除好友请求，请求者(我方) ID:{0}，目标好友 ID:{1}", requesterId, targetId);
        // 想要删除好友肯定需要两个一个是 谁删除的 一个是要删除谁 所以有两个ID
        NetMessage message = new NetMessage();
        message.Request = new NetMessageRequest();
        message.Request.friendRemove = new FriendRemoveRequest();
        message.Request.friendRemove.Id = requesterId;
        message.Request.friendRemove.friendId = targetId;
        NetClient.Instance.SendMessage(message);
    }

    /// <summary>
    /// 处理好友删除响应
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    private void OnRemoveResponse(object sender, FriendRemoveResponse response)
    {
        if(response.Result == Result.Success)
        {
            Debug.LogFormat("OnRemoveResponse : 删除好友成功 (Result:{0})", response.Result);
            FriendManager.Instance.OnFriendRemoveSuccess();
        } else
        {
            Debug.LogFormat("OnRemoveResponse : 删除好友失败，错误信息:{0}", response.Errormsg);
            FriendManager.Instance.OnFriendRemoveFailed(response.Errormsg);
        }
    }



    /// <summary>
    ///  发送列表请求
    /// </summary>
    public void SendListRequest()
    {
        NetMessage message = new NetMessage();
        message.Request = new NetMessageRequest();
        message.Request.friendList = new FriendListRequest();
        NetClient.Instance.SendMessage(message);
    }

    /// <summary>
    /// 处理列表响应
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    private void OnListResponse(object sender, FriendListResponse response)
    {
        int friendCount = response.Friends != null ? response.Friends.Count : 0;
        Debug.LogFormat("OnListResponse : 成功收到服务器下发的好友列表数据，共计 {0} 名好友", friendCount);
        FriendManager.Instance.UpdateFriendList(response.Friends);
    }

}
