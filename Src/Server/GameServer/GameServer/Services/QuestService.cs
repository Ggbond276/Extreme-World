using Common;
using GameServer.Entities;
using GameServer.Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
     class QuestService : Singleton<QuestService>
    {
        public QuestService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<QuestAcceptRequest>(this.OnQuestAccept);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<QuestSubmitRequest>(this.OnQuestSubmit);
        }

        public void Init()
        {

        }

        public void OnQuestAccept(NetConnection<NetSession> sender, QuestAcceptRequest request)
        {
            Log.InfoFormat("OnQuestAccept questId : [{0}]", request.QuestId);
            Character character = sender.Session.Character;
            int questId = request.QuestId;
            Result result = character.questManager.AcceptQuest(questId, out Quest quest);
            sender.Session.Response.questAccept = new QuestAcceptResponse();
            if(result == Result.Success && quest != null)
            {
                sender.Session.Response.questAccept.Quest = quest.ToNQuestInfo();
            } else
            {
                sender.Session.Response.questAccept.Errormsg = "条件不足 任务无法接取";
            }
            sender.Session.Response.questAccept.Result = result;
            sender.SendResponse();
        }

        public void OnQuestSubmit(NetConnection<NetSession> sender, QuestSubmitRequest request)
        {
            Log.InfoFormat("OnQuestSubmit quest: [{0}]", request.QuestId);
            Character character = sender.Session.Character;
            int questId = request.QuestId;
            Result result = character.questManager.SubmitQuest(questId, out Quest quest);

            sender.Session.Response.questSubmit = new QuestSubmitResponse();
            if(result == Result.Success)
            {
                sender.Session.Response.questSubmit.Quest = quest.ToNQuestInfo();
            } else
            {
                sender.Session.Response.questSubmit.Errormsg = "条件不足 任务无法提交";
            }
            sender.Session.Response.questSubmit.Result = result;
            sender.SendResponse();
        }

    }
}
