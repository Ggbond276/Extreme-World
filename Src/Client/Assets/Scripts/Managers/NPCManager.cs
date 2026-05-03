using Common.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : Singleton<NPCManager>
{
    public delegate bool NpcActionHandler(NpcDefine npc);
    private Dictionary<NpcFunction, NpcActionHandler> eventMap = new Dictionary<NpcFunction, NpcActionHandler>();
    public void RegisterNpcEvent(NpcFunction npcFunction, NpcActionHandler npcActionHandler)
    {
        if(!eventMap.ContainsKey(npcFunction))
        {
            eventMap[npcFunction] = npcActionHandler;
        } else
        {
            eventMap[npcFunction] += npcActionHandler;
        }
    }


    public NpcDefine GetNPCDefine(int id)
    {
        return DataManager.Instance.NPCs[id];
    }
    public bool Interactive(int Id)
    {
        if(DataManager.Instance.NPCs.ContainsKey(Id))
        {
            NpcDefine npc = DataManager.Instance.NPCs[Id];
            return Interactive(npc);
        }
        return false;
    }
    public bool Interactive(NpcDefine npc)
    {
        // 做一个全局拦截器 只要NPC身上有任务 全都以任务为最高优先级 判断没有任务之后 再执行其他的日常逻辑
        if (QuestManager.Instance.OpenNpcQuest(npc.ID))
        {
            return true; // 拦截成功！QuestManager 已经弹出了任务面板或气泡，后面的逻辑（比如打开商店）直接掐断！
        }
        // 对npc的类型进行判断
        if (npc.Type == NpcType.Task)
        {
            // 执行Task的逻辑
            return DoTaskInteractive(npc);
        } else if( npc.Type == NpcType.Functional)
        {
            // 执行Function逻辑
            return DoFunctionInteractive(npc);
        } else
        {
            return false;
        }
    }
    public bool DoTaskInteractive(NpcDefine npc)
    {
        // 走到这里，说明他是个村长，且当前没有任何任务给你
        MessageBox.Show("你好，" + npc.Name + "。目前没有什么事情需要你帮忙，你去别处转转吧。");
        return true;
    }
    public bool DoFunctionInteractive(NpcDefine npc)
    {
        // 为什么做过一次判断还要再做一次判断
        if (npc.Type != NpcType.Functional)
            return false;
        if (!eventMap.ContainsKey(npc.Function))
            return false;
        return eventMap[npc.Function](npc);
    }
}
