using Common.Data;
using GameServer.Manager;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    class Quest
    {
        public TCharacterQuest DbQuest { get; private set; }
        public QuestDefine Define { get; private set; }

        // 这里的数据永远只是底层数据的映射 要修改只能修改底层数据
        public int QuestId => Define.ID;
        public string QuestName => Define.Name;
        public QuestStatus Status => (QuestStatus)DbQuest.Status;

        public bool IsCompleted => Status == QuestStatus.Complated;
        public bool IsSubmit => Status == QuestStatus.Finished;
        public bool CanSubmit => IsCompleted && !IsSubmit;

        public Quest(TCharacterQuest dbQuest)
        {
            this.DbQuest = dbQuest;
            this.Define = DataManager.Instance.Quests[dbQuest.QuestID];
        }
        public void InitQuetsDefine()
        {
            if(Define.Target1 == QuestTarget.None)
            {
                this.DbQuest.Status = (int)QuestStatus.Complated;
            }
            else
            {
                this.DbQuest.Status = (int)QuestStatus.InProgress;
            }
        }
        public NQuestInfo ToNQuestInfo()
        {
            return new NQuestInfo()
            {
                QuestId = this.QuestId,
                QuestGuid = this.DbQuest.Id,
                Status = this.Status,
                Targets = new int[3]
                {
                    this.DbQuest.Target1,
                    this.DbQuest.Target2,
                    this.DbQuest.Target3,
                }
            };
        }
        public void OnSubmit()
        {
            this.DbQuest.Status = (int)QuestStatus.Finished;
        }
    }
}
