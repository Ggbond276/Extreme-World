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
            // 订阅服务端推送的 ChatNotify
            MessageDistributer.Instance.Subscribe<ChatNotify>(this.OnChatNotify);
            // 订阅自己发送消息后的 Response 响应
            MessageDistributer.Instance.Subscribe<ChatResponse>(this.OnChatResponse);
        }
        // 解除订阅
        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<ChatNotify>(this.OnChatNotify);
            MessageDistributer.Instance.Unsubscribe<ChatResponse>(this.OnChatResponse);
        }
        // 发送Request
        public void SendChatRequest(ChatChannel channel, string content, int toId = 0, string toName = "" )
        {
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
                ChatManager.Instance.OnChatSendSuccess?.Invoke();
            } else
            {
                // 发送失败：弹出错误提示
                Debug.LogError($"聊天发送失败: {response.Errormsg}");
            }
        }
        // 接收Notify
        private void OnChatNotify(object sender, ChatNotify notify)
        {
            NChatMessage msg = notify.Message;
            ChatManager.Instance.AddMessage(msg);
        }
    }
}
