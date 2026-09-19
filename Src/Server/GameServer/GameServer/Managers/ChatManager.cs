using Common;
using GameServer.Entities;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class ChatManager : Singleton<ChatManager>
    {
        private const int MAX_CHAT_RECORD_NUMS = 100;

        // 全局频道
        public List<NChatMessage> System = new List<NChatMessage>();
        public List<NChatMessage> World = new List<NChatMessage>();
        // 范围频道
        public Dictionary<int, List<NChatMessage>> Local = new Dictionary<int, List<NChatMessage>>();
        public Dictionary<int, List<NChatMessage>> Team = new Dictionary<int, List<NChatMessage>>();
        public Dictionary<int, List<NChatMessage>> Guild = new Dictionary<int, List<NChatMessage>>();

        public void Init()
        {

        }


        /// <summary>
        /// 核心方法：将消息存入对应频道的内存池中
        /// </summary>
        public void AddMessage(Character from, NChatMessage message)
        {
            switch (message.Channel)
            {
                case ChatChannel.Local:
                    AddToList(this.Local, from.MapId, message);
                    break;
                case ChatChannel.World:
                    AddToList(this.World, message);
                    break;
                case ChatChannel.System:
                    AddToList(this.System, message);
                    break;
                case ChatChannel.Team:
                    if (from.team != null)
                    {
                        AddToList(this.Team, from.team.Id, message);
                    }
                    else
                    {
                        // 严格套用 Skill 规范的错误日志
                        Log.ErrorFormat("AddMessage : 收到无队伍玩家的队伍消息(已拦截), CharacterId:{0}", from.Id);
                    }
                    break;
                case ChatChannel.Guild:
                    if (from.GuildId > 0)
                    {
                        AddToList(this.Guild, from.GuildId, message);
                    }
                    else
                    {
                        // 严格套用 Skill 规范的错误日志
                        Log.ErrorFormat("AddMessage : 收到无公会玩家的公会消息(已拦截), CharacterId:{0}", from.Id);
                    }
                    break;
            }
        }

        /// <summary>
        /// 泛型重载：向全局列表添加消息并做溢出裁剪
        /// </summary>
        /// <param name="list"></param>
        /// <param name="message"></param>
        private void AddToList(List<NChatMessage> list, NChatMessage message)
        {
            list.Add(message);
            if(list.Count > MAX_CHAT_RECORD_NUMS)
            {
                list.RemoveAt(0);
            }
        }
        /// <summary>
        /// 泛型重载：向字典列表添加消息并做溢出裁剪
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="message"></param>
        private void AddToList(Dictionary<int, List<NChatMessage>> dict,int key, NChatMessage message)
        {
            if(!dict.TryGetValue(key, out List<NChatMessage> list))
            {
                list = new List<NChatMessage>();
                dict[key] = list;
            }
            AddToList(list, message);
        }
    }
}
