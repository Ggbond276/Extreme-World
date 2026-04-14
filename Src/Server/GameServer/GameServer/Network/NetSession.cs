using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using GameServer;
using GameServer.Entities;
using GameServer.Services;
using SkillBridge.Message;

namespace Network
{
    class NetSession
    {
        public TUser User { get; set; }
        public Character Character { get; set; }
        public NEntity Entity { get; set; }

        internal void Disconnected()
        {
             if(Character!= null)
            {
                Log.InfoFormat("CharacterLeave: {0}", Character.Id);
                UserService.Instance.CharacterLeave(Character);
            }
        }
    }
}
