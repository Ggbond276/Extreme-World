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
    /// <summary>
    /// 服务器端个人角色任务管理器（Model / Manager层）。
    /// 职责：属于单个玩家 Character 的组件，负责处理任务的内存状态流转、数值奖励下发以及数据库（DB）的持久化落地。
    /// </summary>
    class QuestManager
    {
        public Character Owner;  // 维护对当前所属玩家角色实体的引用
        public Dictionary<int, Quest> Quests = new Dictionary<int, Quest>(); // 内存数据池：当前玩家【已接取】和【已完成】的任务字典（未接取的不会存入数据库和此字典）

        /// <summary>
        /// 构造函数：在玩家角色上线初始化时被调用。
        /// 执行后：读取该玩家从数据库里反序列化出来的所有任务记录，并在服务器内存中构建出该角色的运行时任务树。
        /// </summary>
        public QuestManager(Character owner)
        {
            this.Owner = owner; // 绑定宿主角色
            foreach (var dbQuest in owner.Data.Quests) // 遍历底层数据库层(Entity Framework或类似ORM)加载上来的原始记录
            {
                Quest quest = new Quest(dbQuest); // 将数据库静态条目包装为服务器运行时的业务对象
                Quests.Add(quest.QuestId, quest); // 加入内存字典缓存
            }
        }

        /// <summary>
        /// 提供给角色上线时的网络同步接口。
        /// 执行后：将服务器内存里的字典转化为网络通讯协议 List，交由外层打包发给客户端做首次登录初始化。
        /// </summary>
        public void GetQuestInfo(List<NQuestInfo> quests)
        {
            foreach(var quest in this.Quests) // 遍历内存缓存
            {
                NQuestInfo questInfo = new NQuestInfo(); // 实例化 Protobuf/网络协议 对象
                questInfo.QuestId = quest.Value.QuestId; // 赋值 ID
                questInfo.Status = quest.Value.Status; // 赋值当前状态 (InProgress / Finished 等)
                quests.Add(questInfo); // 压入传递进来的引用列表中
            }
        }

        /// <summary>
        /// 执行任务接取的核心业务逻辑与数据库写入。
        /// 执行后：如果在内存和配表中均合法，则会创建新的数据库记录插入持久化层，更新内存字典，并返回 Success。
        /// </summary>
        public Result AcceptQuest(int questId, out Quest quest)
        {
            quest = null; // 初始化 out 参数

            // ============================================ 校验拦截 ============================================

            if (this.Quests.ContainsKey(questId)) // 防重机制。检查内存中是否已有该任务，严防外挂发包重复接取
                return Result.Failed;
            if (!DataManager.Instance.Quests.ContainsKey(questId))  // 合法性校验。严防外挂篡改协议发送一个不存在的恶意 questId
                return Result.Failed;

            // ========================================== 数据库对象创建 ==========================================

            TCharacterQuest dbQuest = DBService.Instance.Entities.CharacterQuests.Create(); // 调用底层 ORM 框架，在内存中创建一行新的关联表记录
            dbQuest.TCharacterID = this.Owner.Data.ID; // 绑定外键：玩家唯一标识符
            dbQuest.QuestID = questId; // 写入业务字段：任务配置 ID

            // ========================================= 内存更新与持久化 =========================================

            quest = new Quest(dbQuest); // 根据新生成的数据库条目创建业务逻辑对象
            quest.InitQuetsDefine(); // 链接任务的静态配置表数据（名称、奖励等）
            this.Quests.Add(quest.QuestId, quest); // 更新内存：塞入当前玩家的任务字典
            this.Owner.Data.Quests.Add(dbQuest); // 更新ORM：塞入 EF 数据追踪列表中

            DBService.Instance.save(); // 将 Insert 操作真正提交到 SQL 数据库
            return Result.Success; // 业务闭环，返回成功状态
        }


        /// <summary>
        /// 执行任务交付的核心业务逻辑、发奖与数据库状态更新。
        /// 执行后：经过状态机校验后，直接修改该角色的金币、经验、背包数据，改变任务状态为彻底完成，并全盘保存至 SQL 数据库。
        /// </summary>
        public Result SubmitQuest(int questId, out Quest quest)
        {
            quest = null; // 初始化 out 参数

            // ============================================ 校验拦截 ============================================

            if (!this.Quests.ContainsKey(questId)) // 玩家根本没接这个任务，直接阻断
                return Result.Failed;
            if (!DataManager.Instance.Quests.ContainsKey(questId)) // 任务 ID 非法，直接阻断
                return Result.Failed;

            quest = this.Quests[questId]; // 从字典提取真实任务实体引用

            if (!quest.CanSubmit) // 核心状态机校验：判断其进度是否达标（如杀怪数量是否满足），不满足严禁提交
                return Result.Failed;

            // ======================================= 发放数值与道具奖励 =======================================

            if (quest.Define.RewardGold > 0) // 直接修改服务器角色的核心资产数值 (金币)
            {
                this.Owner.Gold += quest.Define.RewardGold; 
            }

            if(quest.Define.RewardExp > 0) // 增加经验，可能内部会触发角色升级事件
            {
                this.Owner.Exp += quest.Define.RewardExp;
            }

            if(quest.Define.RewardItem1 > 0) // 调用背包管理器，发道具 1
            {
                int rewardItemId1 = quest.Define.RewardItem1;
                int count1 = quest.Define.RewardItem1Count;
                this.Owner.ItemManager.AddItem(rewardItemId1, count1);
            }

            if (quest.Define.RewardItem2 > 0) // 调用背包管理器，发道具 2
            {
                int rewardItemId2 = quest.Define.RewardItem2;
                int count2 = quest.Define.RewardItem2Count;
                this.Owner.ItemManager.AddItem(rewardItemId2, count2);
            }

            if (quest.Define.RewardItem3 > 0) // 调用背包管理器，发道具 3
            {
                int rewardItemId3 = quest.Define.RewardItem3;
                int count3 = quest.Define.RewardItem3Count;
                this.Owner.ItemManager.AddItem(rewardItemId3, count3);
            }

            // ========================================= 内存更新与持久化 =========================================
            
            quest.OnSubmit(); // 修改内部状态字段，将状态正式切换为 Finished
            DBService.Instance.save(); // 将资产修改和任务状态的 UPDATE 语句统一提交给 SQL 数据库，确保事务一致性

            return Result.Success; // 业务闭环，返回成功状态
        }
    }
}
