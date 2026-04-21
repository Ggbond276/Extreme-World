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
        // 对npc的类型进行判断
        if(npc.Type == NpcType.Task)
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
        MessageBox.Show("点击了任务Npc: " + npc.Name);
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
