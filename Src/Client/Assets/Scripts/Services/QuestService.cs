using Assets.Scripts.Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestService : Singleton<QuestService>, IDisposable
{
    
    public QuestService()
    {
        MessageDistributer.Instance.Subscribe<QuestAcceptResponse>(this.OnQuestAccept);
        MessageDistributer.Instance.Subscribe<QuestSubmitResponse>(this.OnQuestSubmit);
    }

    public void Dispose()
    {
        MessageDistributer.Instance.Unsubscribe<QuestAcceptResponse>(this.OnQuestAccept);
        MessageDistributer.Instance.Unsubscribe<QuestSubmitResponse>(this.OnQuestSubmit);
    }


    public void SendQuestAccept(Quest quest)
    {
        Debug.LogFormat("SendQuestAccept questId:[{0}]",quest.Define.ID);
        NetMessage message = new NetMessage();
        message.Request = new NetMessageRequest();
        message.Request.questAccept = new QuestAcceptRequest();
        message.Request.questAccept.QuestId = quest.Define.ID;

        NetClient.Instance.SendMessage(message);

    }
    public void OnQuestAccept(object sender, QuestAcceptResponse message)
    {
        Debug.LogFormat("OnQuestAccept questId [{0}]",message.Quest.QuestId);
        if(message.Result == Result.Success)
        {
            QuestManager.Instance.OnAcceptQuest(message.Quest);
        } else
        {
            // 如果服务器拒绝（比如背包满了、等级不够），弹系统提示
            MessageBox.Show("接受任务失败：" + message.Errormsg);
        }

    }

    public void SendQuestSubmit(Quest quest)
    {
        Debug.LogFormat("SendQuestSubmit questId:[{0}]", quest.Define.ID);
        NetMessage message = new NetMessage();
        message.Request = new NetMessageRequest();
        message.Request.questSubmit = new QuestSubmitRequest();
        message.Request.questSubmit.QuestId = quest.Define.ID;

        NetClient.Instance.SendMessage(message);
    }
    public void OnQuestSubmit(object sender, QuestSubmitResponse message)
    {
        Debug.LogFormat("OnQuestSubmit questId [{0}]", message.Quest.QuestId);
        if(message.Result == Result.Success)
        {
            QuestManager.Instance.OnSubmitQuest(message.Quest);
        } else
        {
            MessageBox.Show("提交任务失败：" + message.Errormsg);
        }
    }

}
