using Assets.Scripts.Models;
using Common.Data;
using Models;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : Singleton<QuestManager>
{
    public enum NpcQuestStatus
    {
        None = 0,       // 0: 无状态 (隐藏UI)
        Incomplete = 1, // 1: 银色问号 (催更)
        Available = 2,  // 2: 金色感叹号 (可接取)
        Complete = 3    // 3: 金色问号 (可交付)
    }
    // Dictionary<QuestStatus, Dictionary<Quest_Type, List<Quest>>
    public List<NQuestInfo> quests;
    // 以ID作为键值
    public Dictionary<int, Quest> allQuests = new Dictionary<int, Quest>();
    // 以npcId作为键值 创建每个npc持有的任务列表
    public Dictionary<int, List<Quest>> npcQuests = new Dictionary<int, List<Quest>>();
    // 大喇叭打开任务面板
    public Action<Quest> OnOpenQuestDialog;
    // 状态改变了需要通知
    public Action<Quest> OnQuestStatusChanged;

    public void Init(List<NQuestInfo> quests)
    {
        this.quests = quests;
        allQuests.Clear();
        npcQuests.Clear();
        this.InitQuests();
    }
    public void InitQuests()
    {
        // 这里初始化的是已经接取的任务
        foreach(var Info in quests)
        {
            Quest quest = new Quest(Info);
            this.allQuests[quest.Define.ID] = quest;

            // 将任务添加到npc列表
            this.AddNpcQuest(quest.Define.AcceptNPC, quest);
            this.AddNpcQuest(quest.Define.SubmitNPC, quest);
        }
        // 这里初始化的是可以接取的任务
        foreach(var kv in DataManager.Instance.Quests)
        {
            QuestDefine questDefine = kv.Value;
            if (questDefine.LimitClass != CharacterClass.None && questDefine.LimitClass != User.Instance.CurrentCharacter.Class)
                continue;
            if (questDefine.LimitLevel > User.Instance.CurrentCharacter.Level)
                continue;
            if (allQuests.ContainsKey(questDefine.ID))
                continue;
            if(questDefine.PreQuest > 0)
            {
                Quest preQuest;
                if (!this.allQuests.TryGetValue(questDefine.PreQuest, out preQuest))
                    continue;
                if (preQuest.Info == null)
                    continue;
                if (preQuest.Info.Status != QuestStatus.Finished)
                    continue;
            }

            Quest quest = new Quest(questDefine);
            allQuests.Add(quest.Define.ID, quest);

            // 将任务添加到npc列表
            this.AddNpcQuest(quest.Define.AcceptNPC, quest);
            this.AddNpcQuest(quest.Define.SubmitNPC, quest);
        }
    }
    public void AddNpcQuest(int npcId, Quest quest)
    {
        if (!DataManager.Instance.NPCs.ContainsKey(npcId))
            return;
        if (quest == null)
            return;
        if (!this.npcQuests.ContainsKey(npcId))
            this.npcQuests[npcId] = new List<Quest>();
        if(!this.npcQuests[npcId].Contains(quest))
            this.npcQuests[npcId].Add(quest);
    }

    public bool OpenNpcQuest(int npcId)
    {
        if(this.npcQuests.TryGetValue(npcId, out List<Quest> questList))
        {
            Quest targetQuest = null;
            // 1.首先要看有没有可以领取奖励的任务
            if (targetQuest == null)
            {
                foreach (var quest in questList)
                {
                    if(npcId == quest.Define.SubmitNPC &&quest.Info != null && quest.Info.Status == QuestStatus.Complated)
                    {
                        targetQuest = quest;
                        break;
                    }
                }
            }
            // 2.看有没有可以接的新活动
            if(targetQuest == null)
            {
                foreach(var quest in questList)
                {
                    if(npcId == quest.Define.AcceptNPC && quest.Info == null)
                    {
                        targetQuest = quest;
                        break;
                    }
                }
            }  
            // 3.催你去完成你的任务去
            if(targetQuest == null)
            {
                foreach(var quest in questList)
                {
                    if(npcId == quest.Define.SubmitNPC && quest.Info != null && quest.Info.Status == QuestStatus.InProgress)
                    {
                        targetQuest = quest;
                        break;
                    }
                }
            }
            // 4.找到要quest了就用小喇叭喊一声我的任务完成了
            if(targetQuest != null)
            {
                if(targetQuest.Info != null && targetQuest.Info.Status == QuestStatus.InProgress)
                {
                    string defaultMsg = "还有任务没有完成哦";
                    string msg = string.IsNullOrEmpty(targetQuest.Define.DialogIncomplete) ? defaultMsg : targetQuest.Define.DialogIncomplete;
                    MessageBox.Show(msg);
                    return true;
                }
                OnOpenQuestDialog?.Invoke(targetQuest);
                return true;
            }
        }
        return false;
    }
    public NpcQuestStatus GetQuestStatusByNpc(int npcId)
    {
        if(this.npcQuests.TryGetValue(npcId, out List<Quest> questList))
        {
             foreach(var quest in questList)
            {
                if (quest.Define.SubmitNPC == npcId && quest.Info != null && quest.Info.Status == QuestStatus.Finished)
                    return NpcQuestStatus.Complete;
            }

             foreach(var quest in questList)
            {
                if (quest.Define.AcceptNPC == npcId && quest.Info == null)
                    return NpcQuestStatus.Available;
            }

             foreach(var quest in questList)
            {
                if (quest.Define.SubmitNPC == npcId && quest.Info != null && quest.Info.Status == QuestStatus.InProgress)
                    return NpcQuestStatus.Incomplete;
            }
        }
        return NpcQuestStatus.None;
    }

    /// <summary>
    /// 将接收任务和提交任务的请求发送给服务器
    /// </summary>
    /// <param name="quest"></param>
    public void AcceptQuest(Quest quest)
    {
        if (quest != null)
            QuestService.Instance.SendQuestAccept(quest);
    }
    public void SubmitQuest(Quest quest)
    {
        if (quest != null)
            QuestService.Instance.SendQuestSubmit(quest);
    }
    /// <summary>
    /// 处理接收任务和提交任务之后状态更新后的操作
    /// </summary>
    /// <param name="info"></param>
    public void OnAcceptQuest(NQuestInfo info)
    {
        if(this.allQuests.TryGetValue(info.QuestId, out Quest quest))
        {
            quest.Info = info;
            OnQuestStatusChanged?.Invoke(quest);
        }
    }
    public void OnSubmitQuest(NQuestInfo info)
    {
        if(this.allQuests.TryGetValue(info.QuestId, out Quest quest))
        {
            quest.Info = info;
            OnQuestStatusChanged?.Invoke(quest);
        }
    }

}
