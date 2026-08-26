using Assets.Scripts.Managers;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Services
{
    class ChatService : Singleton<ChatService>, IDisposable
    {

        // 订阅
        public void Init()
        {
            Debug.Log("Init : 订阅聊天系统网络消息");
            // 订阅服务端推送的 ChatNotify
            MessageDistributer.Instance.Subscribe<ChatNotify>(this.OnChatNotify);
            // 订阅自己发送消息后的 Response 响应
            MessageDistributer.Instance.Subscribe<ChatResponse>(this.OnChatResponse);
        }
        // 解除订阅
        public void Dispose()
        {
            Debug.Log("Dispose : 注销聊天系统网络消息");
            MessageDistributer.Instance.Unsubscribe<ChatNotify>(this.OnChatNotify);
            MessageDistributer.Instance.Unsubscribe<ChatResponse>(this.OnChatResponse);
        }
        // 发送Request
        public void SendChatRequest(ChatChannel channel, string content, int toId = 0, string toName = "" )
        {
            Debug.LogFormat("SendChatRequest : 发送聊天请求, Channel:{0}, Content:{1}, ToId:{2}, ToName:{3}", channel, content, toId, toName);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.chatRequest = new ChatRequest();

            NChatMessage chatMessage = new NChatMessage();
            chatMessage.Channel = channel;
            chatMessage.Message = content;
            chatMessage.toId = toId;
            chatMessage.toName = toName;

            message.Request.chatRequest.Message = chatMessage;
            NetClient.Instance.SendMessage(message);
        }
        // 接收Response
        private void OnChatResponse(object sender, ChatResponse response)
        {
            if(response.Result == Result.Success)
            {
                Debug.Log("OnChatResponse : 收到发送聊天响应, 结果:成功");
                ChatManager.Instance.OnChatSendSuccess?.Invoke();
            } else
            {
                // 发送失败：弹出错误提示
                Debug.LogErrorFormat("OnChatResponse : 收到发送聊天响应, 结果:失败, Errormsg:{0}", response.Errormsg);
            }
        }
        // 接收Notify
        private void OnChatNotify(object sender, ChatNotify notify)
        {
            NChatMessage msg = notify.Message;
            Debug.LogFormat("OnChatNotify : 收到服务器聊天广播, Channel:{0}, Message:{1}", msg.Channel, msg.Message);
            ChatManager.Instance.AddMessage(msg);
        }
    }
}
