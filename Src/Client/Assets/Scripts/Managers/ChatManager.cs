using Assets.Scripts.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace Assets.Scripts.Managers
{
    class ChatManager : Singleton<ChatManager>
    {
        private const int MAX_CHAT_CACHE_COUNT = 200;

        // 全局本地数据池
        public List<NChatMessage> AllMessages = new List<NChatMessage>(); // 综合频道
        public List<NChatMessage> LocalMessages = new List<NChatMessage>();
        public List<NChatMessage> WorldMessages = new List<NChatMessage>();
        public List<NChatMessage> SystemMessages = new List<NChatMessage>();
        public List<NChatMessage> TeamMessages = new List<NChatMessage>();
        public List<NChatMessage> GuildMessages = new List<NChatMessage>();
        public List<NChatMessage> PrivateMessages = new List<NChatMessage>(); // 私聊本地缓存

        public UnityAction<ChatChannel> OnChatUpdated;
        public UnityAction OnChatSendSuucee;

        public void Init()
        {
            ChatService.Instance.Init();
        }


        //Service上行调用

        /// <summary>
        /// 将服务器推送下来的消息存入本地，触发UI刷新事件
        /// </summary>
        public void AddMessage(NChatMessage message)
        {
            if(message.Channel != ChatChannel.System)
            {
                AddToList(AllMessages, message);
            }

            // 2. 根据具体频道塞入对应列表
            switch (message.Channel)
            {
                case ChatChannel.Local: AddToList(LocalMessages, message); break;
                case ChatChannel.World: AddToList(WorldMessages, message); break;
                case ChatChannel.System: AddToList(SystemMessages, message); break;
                case ChatChannel.Team: AddToList(TeamMessages, message); break;
                case ChatChannel.Guild: AddToList(GuildMessages, message); break;
                case ChatChannel.Private: AddToList(PrivateMessages, message); break;
            }

            // 3. 抛出事件，通知 UI 刷新 (把变化的频道传过去，UI 可以根据当前所处的切页决定要不要刷新)
            OnChatUpdated?.Invoke(message.Channel);
        }

        private void AddToList(List<NChatMessage> list, NChatMessage message)
        {
            list.Add(message);
            if (list.Count > MAX_CHAT_CACHE_COUNT)
                list.RemoveAt(0);
        }

        // UI下行调用
        public void SendChat(ChatChannel channel, string content, int toId = 0, string toName = "")
        {
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("输入内容不可为空");
                return;
            }

            ChatService.Instance.SendChatRequest(channel, content, toId, toName);
        }
    
    }
}
