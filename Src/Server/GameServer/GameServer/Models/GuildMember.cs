using GameServer.Entities;
using GameServer.Manager;
using GameServer.Managers;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    class GuildMember
    {
        /// <summary>
        /// 数据源
        /// </summary>
        public TGuildMember Data { get; private set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="guildMemberData"></param>
        public GuildMember(TGuildMember guildMemberData)
        {
            this.Data = guildMemberData;
        }

        /// <summary>
        /// 服务端内存数据转网络数据
        /// </summary>
        /// <returns></returns>
        public NGuildMember ToNGuildMember()
        {
            int characterId = this.Data.CharacterID;

            // 接入户籍大管家，彻底解决离线玩家的空指针异常
            CharacterInfo info = CharacterInfoManager.Instance.GetCharacterInfo(characterId);

            NGuildMember nGuildMember = new NGuildMember();
            nGuildMember.CharacterId = characterId;

            // 增加一层防御性编程
            if (info != null)
            {
                nGuildMember.Name = info.Name;
                nGuildMember.Level = info.Level;
                nGuildMember.ClassType = info.Class;
            }
            else
            {
                nGuildMember.Name = "未知玩家";
                nGuildMember.Level = 0;
                nGuildMember.ClassType = 0;
            }

            nGuildMember.Position = (GuildPosition)this.Data.Position;
            nGuildMember.IsOnline = SessionManager.Instance.GetSession(characterId) != null ? 1 : 0;

            return nGuildMember;
        }
    }
}
