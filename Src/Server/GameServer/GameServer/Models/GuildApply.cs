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
    class GuildApply
    {
        /// <summary>
        /// 数据源
        /// </summary>
        public TGuildApply Data { get; private set; }

        /// <summary>
        ///  构造方法
        /// </summary>
        /// <param name="guildApplyData"></param>
        public GuildApply(TGuildApply guildApplyData)
        {
            this.Data = guildApplyData;
        }

        /// <summary>
        ///  服务端内存数据转网络数据
        /// </summary>
        /// <returns></returns>
        public NGuildApply ToNGuildApply()
        {

            int characterId = this.Data.CharacterID;
            // 离线玩家的空指针问题
            Character character = CharacterManager.Instance.GetCharacter(characterId);

            NGuildApply nGuildApply = new NGuildApply();
            nGuildApply.CharacterId = characterId;
            nGuildApply.Name = character.Data.Name;
            nGuildApply.Level = character.Data.Level;
            nGuildApply.ClassType = character.Data.Class;
            nGuildApply.IsOnline = SessionManager.Instance.GetSession(characterId) != null ? 1 : 0;

            return nGuildApply;
        }
    }
}
