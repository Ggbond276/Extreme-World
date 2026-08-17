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
            // 空指针异常问题
            int characterId = this.Data.CharacterID;
            Character character = CharacterManager.Instance.GetCharacter(characterId);

            NGuildMember nGuildMember = new NGuildMember();

            nGuildMember.CharacterId = characterId;
            nGuildMember.Name = character.Data.Name;
            nGuildMember.Level = character.Data.Level;
            nGuildMember.ClassType = character.Data.Class;
            nGuildMember.Position = (GuildPosition)this.Data.Position;
            nGuildMember.IsOnline = SessionManager.Instance.GetSession(characterId) != null ? 1 : 0;

            return nGuildMember;
        }
    }
}
