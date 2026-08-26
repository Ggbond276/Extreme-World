using Common;
using Common.Utils;
using GameServer.Entities;
using GameServer.Managers;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
    class ChatService : Singleton<ChatService>
    {
        public ChatService()
        {
            // 在构造函数中注册网络分发事件
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<ChatRequest>(this.OnChat);
        }

        public void Init()
        {
            ChatManager.Instance.Init();
        }

        private void OnChat(NetConnection<NetSession> sender, ChatRequest request)
        {
            Character character = sender.Session.Character;
            NChatMessage msg = request.Message;

            msg.fromId = character.Id; 
            msg.fromName = character.Data.Name;
            msg.fromClass = (int)character.Info.Class;
            msg.Time = TimeUtil.timestamp;

            Log.InfoFormat("OnChat: Character:{0} Channel:{1} Message:{2}", character.Id, msg.Channel, msg.Message);

            if(msg.Channel == ChatChannel.Private)
            {
                var targetSession = SessionManager.Instance.GetSession(msg.toId);
                if(targetSession == null)
                {
                    sender.Session.Response.chatResponse = new ChatResponse();
                    sender.Session.Response.chatResponse.Result = Result.Failed;
                    sender.Session.Response.chatResponse.Errormsg = "对方不在线";
                    sender.SendResponse();
                    return;
                }
                targetSession.Session.Response.chatNotify = new ChatNotify();
                targetSession.Session.Response.chatNotify.Message = msg;
            } else
            {
                ChatManager.Instance.AddMessage(character, msg);
                ChatNotify notify = new ChatNotify();
                notify.Message = msg;

                // 1.谁发的 2.哪个频道 3.发送什么信息
                BroadcastMessage(character, msg.Channel, notify);
            }
        }

        private void BroadcastMessage(Character senderCharacter, ChatChannel channel, ChatNotify notify)
        {
            foreach(var target in SessionManager.Instance.Sessions.Values)
            {
                if (target.Session.Character == null) continue;
                bool shouldSend = false;

                switch(channel)
                {
                    case ChatChannel.World:
                    case ChatChannel.System:
                        shouldSend = true;
                        break;
                    case ChatChannel.Local:
                        if (target.Session.Character.Info.mapId == senderCharacter.Info.mapId) shouldSend = true;
                        break;
                    case ChatChannel.Guild:
                        if (target.Session.Character.GuildId != 0 && senderCharacter.GuildId != 0 &&
                            target.Session.Character.GuildId == senderCharacter.GuildId)
                            shouldSend = true;
                        break;
                    case ChatChannel.Team:
                        if (target.Session.Character.team != null && senderCharacter.team != null &&
                            target.Session.Character.team.Id == senderCharacter.team.Id)
                            shouldSend = true;
                        break;
                }

                if (shouldSend)
                {
                    target.Session.Response.chatNotify = notify;
                    target.SendResponse();
                }
            }
        }
    }
}
