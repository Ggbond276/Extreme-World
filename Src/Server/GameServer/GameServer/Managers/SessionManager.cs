using Common;
using Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class SessionManager : Singleton<SessionManager>
    {
        public Dictionary<int, NetConnection<NetSession>> Sessions = new Dictionary<int, NetConnection<NetSession>>();

        /// <summary>
        /// 玩家进入游戏 添加到Session
        /// </summary>
        public void AddSession(int characterId , NetConnection<NetSession> session) 
        {
            this.Sessions[characterId] = session;
        }

        /// <summary>
        /// 玩家离开游戏 删除Session
        /// </summary>
        /// <param name="characterId"></param>
        public void RemoveSession(int characterId)
        {
            this.Sessions.Remove(characterId);
        }

        /// <summary>
        /// 获取某个玩家的Session连接
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        public NetConnection<NetSession> GetSession(int characterId)
        {
            this.Sessions.TryGetValue(characterId, out NetConnection<NetSession> session);
            return session;
        }
    }
}
