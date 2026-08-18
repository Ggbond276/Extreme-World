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

            // 完美排雷：不再向 CharacterManager 索要活体对象，而是向户籍大管家索要名片！
            // 无论玩家是否在线，都能以 O(1) 的速度瞬间拿到完整数据
            CharacterInfo info = CharacterInfoManager.Instance.GetCharacterInfo(characterId);

            NGuildApply nGuildApply = new NGuildApply();
            nGuildApply.CharacterId = characterId;

            // 增加一层防御性编程，防止数据库出现极其罕见的孤儿脏数据
            if (info != null)
            {
                nGuildApply.Name = info.Name;
                nGuildApply.Level = info.Level;
                nGuildApply.ClassType = info.Class;
            }
            else
            {
                nGuildApply.Name = "未知玩家";
                nGuildApply.Level = 0;
                nGuildApply.ClassType = 0;
            }

            // 在线状态依然通过 Session 判断，这个逻辑是完全正确的
            nGuildApply.IsOnline = SessionManager.Instance.GetSession(characterId) != null ? 1 : 0;

            return nGuildApply;
        }
    }
}
