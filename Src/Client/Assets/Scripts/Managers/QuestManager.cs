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

    public List<NQuestInfo> quests; // 服务器下发的当前玩家正在进行或已完成的动态任务数据列表
    public Dictionary<int, Quest> allQuests = new Dictionary<int, Quest>(); // 以任务ID为Key的全局任务字典（包含已接取和可接取的任务）
    public Dictionary<int, List<Quest>> npcQuests = new Dictionary<int, List<Quest>>(); // 以NPC ID为Key的字典，记录每个NPC身上挂载的任务集合

    public Action<Quest> OnOpenQuestDialog; // 事件：触发打开任务对话框UI，向表现层广播
    public Action<Quest> OnQuestStatusChanged; // 事件：任务状态发生变化（接取/打怪进度更新/交付），通知UI追踪面板刷新

    /// <summary>
    /// 初始化任务管理器，接收服务器下发的数据并重置本地任务池。
    /// 执行后：本地缓存的 allQuests 和 npcQuests 字典被清空，并触发 InitQuests 重新构建整套任务数据树。
    /// </summary>
    public void Init(List<NQuestInfo> quests)
    {
        this.quests = quests; // 保存服务器下发的动态数据
        allQuests.Clear(); // 清空历史全局任务缓存
        npcQuests.Clear(); // 清空历史NPC任务挂载缓存
        this.InitQuests(); // 核心：重新构建任务池
    }

    /// <summary>
    /// 核心任务池构建逻辑，分两步合并数据：1.封装服务器已接取/已完成的任务；2.遍历静态配置表筛选出当前可接取的新任务。
    /// 执行后：allQuests 字典被完整填充，npcQuests 字典将所有有效任务精确挂载到对应的接取NPC和交付NPC身上，准备好为UI和场景提供数据查询。
    /// </summary>
    public void InitQuests()
    {
        
        foreach(var Info in quests) // [分块1] 构建已存在的任务：遍历服务器下发的动态任务数据
        {
            Quest quest = new Quest(Info);  // 将动态数据封装为客户端任务运行时对象
            this.allQuests[quest.Define.ID] = quest; // 注册到全局任务字典

            this.AddNpcQuest(quest.Define.AcceptNPC, quest); // 以【接取NPC】的ID为键，将该任务归类存入字典，建立正向数据索引
            this.AddNpcQuest(quest.Define.SubmitNPC, quest); // 以【交付NPC】的ID为键，将该任务归类存入字典，建立反向数据索引
        }


        foreach(var kv in DataManager.Instance.Quests) // [分块2] 挖掘可接取的新任务：遍历本地所有静态任务配置表
        {
            QuestDefine questDefine = kv.Value;  // 获取配置表静态数据

           
            if (questDefine.LimitClass != CharacterClass.None 
                && questDefine.LimitClass != User.Instance.CurrentCharacter.Class)  // 拦截 1：排除不符合职业要求的任务
                continue;

            if (questDefine.LimitLevel > User.Instance.CurrentCharacter.Level) // 拦截 2：排除不符合玩家当前等级要求的任务
                continue;

            if (allQuests.ContainsKey(questDefine.ID)) // 拦截 3：排除已经被接取或已经完成的任务（防止重复添加）
                continue;

            if (questDefine.PreQuest > 0) // 拦截 4：存在前置任务要求时的严苛判断校验
            {
                Quest preQuest; // 声明前置任务对象

                if (!this.allQuests.TryGetValue(questDefine.PreQuest, out preQuest)) // 拦截 4.1：前置任务根本不在玩家的任务池中，说明未接取
                    continue;

                if (preQuest.Info == null) // 拦截 4.2：前置任务只有静态配置没有动态数据，说明未接取
                    continue;

                if (preQuest.Info.Status != QuestStatus.Finished) // 拦截 4.3：前置任务存在但状态不是彻底完结(Finished)
                    continue;
            }
            Quest quest = new Quest(questDefine); // 通过所有拦截校验，创建可接取的新任务对象（此时无动态Info数据）
            allQuests.Add(quest.Define.ID, quest); // 注册到全局任务字典

            this.AddNpcQuest(quest.Define.AcceptNPC, quest); // 以【接取NPC】的ID为键，将该新任务归类存入字典，供后续查表
            this.AddNpcQuest(quest.Define.SubmitNPC, quest); // 以【交付NPC】的ID为键，将该新任务归类存入字典，供后续查表
        }
    }


    /// <summary>
    /// 构建 NPC ID 到 任务列表 的映射关系（建立数据索引）。
    /// 执行后：如果该 NPC 存在且任务合法，任务将被追加到 npcQuests[npcId] 的字典列表中，用于交互时快速查表。
    /// </summary>
    public void AddNpcQuest(int npcId, Quest quest)
    {
        if (!DataManager.Instance.NPCs.ContainsKey(npcId)) // 容错校验：静态NPC表中不存在该NPC，直接阻断
            return;

        if (quest == null) // 容错校验：任务对象为空，直接阻断
            return;

        if (!this.npcQuests.ContainsKey(npcId)) // 初始化判定：若该NPC首次被记录索引，则创建List实例
            this.npcQuests[npcId] = new List<Quest>();

        if(!this.npcQuests[npcId].Contains(quest)) // 防重判定：确保同一个任务不会被重复添加到同一个NPC的索引列表中
            this.npcQuests[npcId].Add(quest);
    }


    /// <summary>
    /// 处理玩家点击场景NPC时的交互判定逻辑，按最高优先级筛选该NPC当前能提供的任务服务。
    /// 执行后：如果命中任务，必定会消费掉这次交互：要么弹出未完成的飘字提示，要么触发 OnOpenQuestDialog 广播打开UI面板；并返回 true。
    /// </summary>
    public bool OpenNpcQuest(int npcId)
    {
        if(this.npcQuests.TryGetValue(npcId, out List<Quest> questList)) // 查表：检查当前点击的NPC是否映射了相关任务
        {
            Quest targetQuest = null; // 用于锁定最终要处理的高优先级任务

            if (targetQuest == null) // [优先级 1 - 最高]：寻找可领取奖励的交付任务
            {
                foreach (var quest in questList) // 遍历该NPC关联的所有任务
                {
                    if(npcId == quest.Define.SubmitNPC && // 判定条件：我是交付NPC && 任务已接取 && 任务目标已达成
                        quest.Info != null && 
                        quest.Info.Status == QuestStatus.Complated) 
                    {
                        targetQuest = quest; // 锁定任务
                        break; // 命中最高优先级，立即跳出循环
                    }
                }
            }

            if(targetQuest == null) // [优先级 2 - 中等]：寻找可以接取的新任务
            {
                foreach(var quest in questList) // 遍历该NPC关联的所有任务
                {
                    if(npcId == quest.Define.AcceptNPC && // 判定条件：我是接取NPC && 任务动态数据为空(代表未接取)
                        quest.Info == null) 
                    {
                        targetQuest = quest;  // 锁定任务
                        break; // 命中优先级，立即跳出循环
                    }
                }
            }  

            if(targetQuest == null) // [优先级 3 - 最低]：寻找正在进行中但未完成的任务（用于NPC催更提醒）
            {
                foreach(var quest in questList) // 遍历该NPC关联的所有任务
                {
                    if(npcId == quest.Define.SubmitNPC && 
                        quest.Info != null && 
                        quest.Info.Status == QuestStatus.InProgress) // 判定条件：我是交付NPC && 任务已接取 && 任务进度还在进行中
                    {
                        targetQuest = quest; // 锁定任务
                        break; // 命中优先级，立即跳出循环
                    }
                }
            }

            if(targetQuest != null) // [执行阶段]：已锁定目标任务，开始决定交互表现
            {
                if(targetQuest.Info != null &&  // 分支 A：属于优先级 3 (未完成催更)
                    targetQuest.Info.Status == QuestStatus.InProgress)
                {
                    string defaultMsg = "还有任务没有完成哦"; // 设置默认催更文案
                    string msg = string.IsNullOrEmpty(targetQuest.Define.DialogIncomplete) ? defaultMsg : targetQuest.Define.DialogIncomplete; // 读取配置表专属催更文案，若无则用默认
                    MessageBox.Show(msg); // 弹出屏幕中央飘字提示框
                    return true; // 交互被消费，返回 true
                }
                OnOpenQuestDialog?.Invoke(targetQuest); // 分支 B：属于优先级 1 或 2，广播事件打开正规任务对话面板UI
                return true; // 交互被消费，返回 true
            }
        }
        return false; // 该NPC身上没有任何有效任务需要处理，返回 false，让交互系统继续判定是否打开普通商店或对话等
    }

    /// <summary>
    /// 计算指定NPC头顶应该显示的状态图标标识。
    /// 执行后：返回一个确定的 NpcQuestStatus 枚举，表现层(View)将根据此枚举实例化对应的UI特效并显示在NPC头顶。
    /// </summary>
    public NpcQuestStatus GetQuestStatusByNpc(int npcId)
    {
        if(this.npcQuests.TryGetValue(npcId, out List<Quest> questList)) // 查表：检查该NPC是否映射了相关任务
        {
             foreach(var quest in questList)
            {
                if (quest.Info != null && // 逻辑剥离：已彻底完结的任务属于历史遗留，直接略过不计算状态
                    quest.Info.Status == QuestStatus.Finished) 
                    continue;

                if (quest.Define.SubmitNPC == npcId &&  // 命中：该NPC是交付人且目标已达成，返回金色问号状态
                    quest.Info != null &&
                    quest.Info.Status == QuestStatus.Complated)
                    return NpcQuestStatus.Complete;
            }

             foreach(var quest in questList) // [筛选 2 - 中等优先级]：判定是否有【可接取】状态 (Available)
            {
                if (quest.Define.AcceptNPC == npcId &&  // 命中：该NPC是接取人且未接取过，返回金色感叹号状态
                    quest.Info == null)
                    return NpcQuestStatus.Available;
            }

             foreach(var quest in questList) // [筛选 3 - 最低优先级]：判定是否有【进行中催更】状态 (Incomplete)
            {
                if (quest.Define.SubmitNPC == npcId && // 命中：该NPC是交付人但玩家还没打完怪，返回银色问号状态
                    quest.Info != null && 
                    quest.Info.Status == QuestStatus.InProgress) 
                    return NpcQuestStatus.Incomplete;
            }
        }
        return NpcQuestStatus.None; // 没有任何有效任务，返回无状态
    }



    /// <summary>
    /// 向服务器发送【请求接取任务】的协议指令。
    /// 执行后：客户端状态不会立即改变，逻辑挂起，等待网络层接收到回应后触发 OnAcceptQuest 才会刷新数据。
    /// </summary>
    public void AcceptQuest(Quest quest)
    {
        if (quest != null) // 校验任务不为空后，通过单例网络服务发送网络包
            QuestService.Instance.SendQuestAccept(quest);
    }

    /// <summary>
    /// 向服务器发送【请求交付任务】的协议指令。
    /// 执行后：客户端状态不会立即改变，逻辑挂起，等待网络层发奖并回应后触发 OnSubmitQuest 才会刷新数据。
    /// </summary>
    public void SubmitQuest(Quest quest)
    {
        if (quest != null) // 校验任务不为空后，通过单例网络服务发送网络包
            QuestService.Instance.SendQuestSubmit(quest);
    }


    /// <summary>
    /// 接收服务器下发的【接取任务成功】网络回调响应。
    /// 执行后：该任务的内存动态数据被赋值，并触发 OnQuestStatusChanged 广播，通知主界面UI（任务追踪面板/NPC头顶图标）实时刷新。
    /// </summary>
    public void OnAcceptQuest(NQuestInfo info)
    {

        if(this.allQuests.TryGetValue(info.QuestId, out Quest quest)) // 从全局任务字典中抓取出对应的任务实例
        {
            quest.Info = info; // 服务器数据覆盖本地动态数据（此时状态通常变为 InProgress）
            OnQuestStatusChanged?.Invoke(quest); // 触发状态变化事件总线
        }
    }


    /// <summary>
    /// 接收服务器下发的【交付任务成功】网络回调响应。
    /// 执行后：当前任务被标记为彻底完成(Finished)，随即触发 InitQuests 强行重构整棵任务数据树（为了解锁下一环前置条件），最后广播通知UI刷新。
    /// </summary>s
    public void OnSubmitQuest(NQuestInfo info)
    { 
        if(this.allQuests.TryGetValue(info.QuestId, out Quest quest))  // 从全局任务字典中抓取出对应的任务实例
        {
            quest.Info = info; // 服务器数据覆盖本地动态数据（此时状态通常变为 Finished）
            this.InitQuests(); // 极其关键：必须全盘重新初始化一次数据池，因为当前任务的完成可能正是其他新任务的开启条件 (PreQuest)
            OnQuestStatusChanged?.Invoke(quest); // 触发状态变化事件总线，通知表现层刷新
        }
    }

}
