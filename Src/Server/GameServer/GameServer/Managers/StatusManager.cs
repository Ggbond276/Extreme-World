using GameServer.Entities;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class StatusManager
    {
        public Character Owner;
        
        public List<NStatus> Status;
        public StatusManager(Character character)
        {
            this.Owner = character;
            Status = new List<NStatus>();
        }
        public void AddStatus(StatusType type, StatusAction action, int id, int value) 
        {
            Status.Add(new NStatus()
            {
                Type = type,
                Action = action,
                Id = id,
                Value = value
            });
        }
        // 这个方法将变化的消息塞进了大卡车
        public void ApplyReponse(NetMessageResponse Response)
        {
            if (Response.statusNotify == null)
                Response.statusNotify = new StatusNotify();
            foreach(var status in this.Status)
            {
                Response.statusNotify.Status.Add(status);
            }
            this.Status.Clear();
        }



        // 金币状态发生变化
        public void AddGoldChange(int goldDelta)
        {
            if(goldDelta > 0)
            {
                AddStatus(StatusType.Money, StatusAction.Add, 0, goldDelta);
            }
            if(goldDelta < 0)
            {
                AddStatus(StatusType.Money, StatusAction.Delete, 0, goldDelta);           
            }
        }
        // 道具发生了变化
        public void AddItemChange(StatusAction action, int id, int count)
        {
            AddStatus(StatusType.Item, action, id, count);
        }
        // 经验值发生了变化
        public void AddExpChange(int expDelta)
        {
            if(expDelta > 0)
            {
                AddStatus(StatusType.Exp, StatusAction.Add, 0, expDelta);
            }
            if(expDelta < 0)
            {
                AddStatus(StatusType.Exp, StatusAction.Delete, 0, expDelta);
            }
        }
    }
}
 