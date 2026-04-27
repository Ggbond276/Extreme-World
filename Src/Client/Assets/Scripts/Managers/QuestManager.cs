using Assets.Scripts.Models;
using Common.Data;
using Models;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : Singleton<QuestManager>
{
    // Dictionary<QuestStatus, Dictionary<Quest_Type, List<Quest>>
    public List<NQuestInfo> quests;
    //  以ID作为键值
    public Dictionary<int, Quest> allQuests = new Dictionary<int, Quest>();

    public void Init(List<NQuestInfo> quests)
    {
        this.quests = quests;
        allQuests.Clear();
        this.InitQuests();
    }

    public void InitQuests()
    {
        // 这里初始化的是已经接取的任务
        foreach(var Info in quests)
        {
            Quest quest = new Quest(Info);
            this.allQuests[quest.Define.ID] = quest;
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
                if(this.allQuests.TryGetValue(questDefine.PreQuest, out preQuest)){
                    if (preQuest.Info.Status != QuestStatus.Finished)
                        continue;
                } else
                {
                    continue;
                }
            }
            Quest quest = new Quest(questDefine);
            allQuests.Add(quest.Define.ID, quest);
        }
    }
}
