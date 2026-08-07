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
        /// <summary>
        /// 这里使用玩家的DBId作为键值
        /// </summary>
        public Dictionary<int, NetConnection<NetSession>> Sessions = new Dictionary<int, NetConnection<NetSession>>();

        /// <summary>
        /// 玩家进入游戏 添加到Session
        /// </summary>
        public void AddSession(int characterId , NetConnection<NetSession> session) 
        {
            Log.InfoFormat("AddSession : 正在将玩家添加到Session字典, CharacterId:{0}", characterId);
            if (characterId == 0)
            {
                Log.ErrorFormat("AddSession : 异常警告 - 试图将 CharacterId 为 0 的会话写入字典，请检查调用栈", characterId);
            }
            this.Sessions[characterId] = session;

        }

        /// <summary>
        /// 玩家离开游戏 删除Session
        /// </summary>
        /// <param name="characterId"></param>
        public void RemoveSession(int characterId)
        {
            Log.InfoFormat("AddSession : 正在将玩家移除Session字典, CharacterId:{0}", characterId);
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
