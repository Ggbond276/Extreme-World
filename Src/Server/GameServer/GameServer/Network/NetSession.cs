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
    class NetSession : INetSession
    {
        public TUser User { get; set; }
        public Character Character { get; set; }
        public NEntity Entity { get; set; }

        internal void Disconnected()
        {
            if (Character != null)
            {
                Log.InfoFormat("CharacterLeave: {0}", Character.entityId);
                UserService.Instance.CharacterLeave(Character);
            }
        }


        NetMessage response;
        public  NetMessageResponse Response
        {
            get
            {
                if (response == null)
                    response = new NetMessage();
                if (response.Response == null)
                    response.Response = new NetMessageResponse();
                return response.Response;
            }
        }

        public byte[] GetResponse()
        {
            if (Response != null)
            {
                if (this.Character != null && this.Character.statusManager.Status != null)
                {
                    this.Character.statusManager.ApplyReponse(Response);
                }
                byte[] data = PackageHandler.PackMessage(response);
                response = null;
                return data;
            }
            return null;
        }
    }
}
