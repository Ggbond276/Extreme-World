using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Data
{
    public enum QuestType
    {
        Main,
        Branch
    }

    public enum QuestTarget
    {
        None,
        Kill,
        Item
    }
    public class QuestDefine
    {
        // 基础信息
        public int ID { get; set; }
        public string Name { get; set; }
        // 限制
        public int LimitLevel { get; set; }
        public CharacterClass LimitClass { get; set; }
        // 前置任务
        public int PreQuest { get; set; }
        // 类型
        public QuestType Type { get; set; }
        // NPC入口
        public int AcceptNPC { get; set; }
        public int SubmitNPC { set; get; }
        //  对话
        public string OverView { set; get; }
        public string Dialog { set; get; }
        public string DialogAccept { set; get; }
        public string DialogDeny { set; get; }
        public string DialogIncomplete { set; get; }
        public string DialogFinish { set; get; }
        // 目标
        public QuestTarget Target1 { get; set; }
        public int Target1ID { get; set; }
        public int Target1Num { get; set; }
        public QuestTarget Target2 { get; set; }
        public int Target2ID { get; set; }
        public int Target2Num { get; set; }
        public QuestTarget Target3 { get; set; }
        public int Target3ID { get; set; }
        public int Target3Num { get; set; }
        // 奖励
        public int RewardGold { get; set; }
        public int RewardExp { get; set; }
        public int RewardItem1 { get; set; }
        public int RewardItem1Count { get; set; }
        public int RewardItem2 { get; set; }
        public int RewardItem2Count { get; set; }
        public int RewardItem3 { get; set; }
        public int RewardItem3Count { get; set; }


    }
}
