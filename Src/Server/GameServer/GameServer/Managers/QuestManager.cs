using GameServer.Entities;
using GameServer.Manager;
using GameServer.Models;
using GameServer.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    // 数据库里面存放的全都是已经接收了的任务
    class QuestManager
    {
        public Character Owner; 
        // 这里面存放的都是已经接收的任务
        public Dictionary<int, Quest> Quests = new Dictionary<int, Quest>();

        public QuestManager(Character owner)
        {
            this.Owner = owner;
            foreach(var dbQuest in owner.Data.Quests)
            {
                Quest quest = new Quest(dbQuest);
                Quests.Add(quest.QuestId, quest);
            }
        }

        public void GetQuestInfo(List<NQuestInfo> quests)
        {
            foreach(var quest in this.Quests)
            {
                NQuestInfo questInfo = new NQuestInfo();
                questInfo.QuestId = quest.Value.QuestId;
                questInfo.Status = quest.Value.Status;
                quests.Add(questInfo);
            }
        }
        public Result AcceptQuest(int questId, out Quest quest)
        {
            quest = null;

            // 判断以接取的任务列表中是否包含了这个任务 防止重复接取任务
            if (this.Quests.ContainsKey(questId))
                return Result.Failed;
            // 判断配置表中是否存在这个任务 放置接取不存在的任务
            if (!DataManager.Instance.Quests.ContainsKey(questId))
                return Result.Failed;

            // 创建数据库数据
            TCharacterQuest dbQuest = DBService.Instance.Entities.CharacterQuests.Create();
            dbQuest.TCharacterID = this.Owner.Data.ID;
            dbQuest.QuestID = questId;

            quest = new Quest(dbQuest);
            quest.InitQuetsDefine();
            this.Quests.Add(quest.QuestId, quest);
            this.Owner.Data.Quests.Add(dbQuest);

            DBService.Instance.save();
            return Result.Success;
        }
        public Result SubmitQuest(int questId, out Quest quest)
        {
            quest = null;
            if (!this.Quests.ContainsKey(questId))
                return Result.Failed;
            if (!DataManager.Instance.Quests.ContainsKey(questId))
                return Result.Failed;
            quest = this.Quests[questId];
            if (!quest.CanSubmit)
                return Result.Failed;

            // 发放金币奖励
            if(quest.Define.RewardGold > 0)
            {
                this.Owner.Gold += quest.Define.RewardGold;
            }
            // 发放经验奖励
            if(quest.Define.RewardExp > 0)
            {
                this.Owner.Exp += quest.Define.RewardExp;
            }
            // 发放第一份奖励
            if(quest.Define.RewardItem1 > 0)
            {
                int rewardItemId1 = quest.Define.RewardItem1;
                int count1 = quest.Define.RewardItem1Count;
                this.Owner.ItemManager.AddItem(rewardItemId1, count1);
            }
            // 发放第二份奖励
            if (quest.Define.RewardItem2 > 0)
            {
                int rewardItemId2 = quest.Define.RewardItem2;
                int count2 = quest.Define.RewardItem2Count;
                this.Owner.ItemManager.AddItem(rewardItemId2, count2);
            }
            // 发放第三份奖励
            if (quest.Define.RewardItem3 > 0)
            {
                int rewardItemId3 = quest.Define.RewardItem3;
                int count3 = quest.Define.RewardItem3Count;
                this.Owner.ItemManager.AddItem(rewardItemId3, count3);
            }
            quest.OnSubmit();
            DBService.Instance.save();
            return Result.Success;
        }
    }
}
