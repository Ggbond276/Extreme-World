using Common;
using GameServer.Models;
using GameServer.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class GuildManager : Singleton<GuildManager>
    {

        public Dictionary<int, Guild> Guilds = new Dictionary<int, Guild>();
        public Dictionary<int, int> CharacterGuildIdMap = new Dictionary<int, int>();

        /// <summary>
        /// 初始化内存数据方法
        /// </summary>
        public void Init()
        {
            var allTGuilds = DBService.Instance.Entities.TGuildSet.ToList();
            var allTGuildMembers = DBService.Instance.Entities.TGuildMemberSet.ToList();
            var allTGuildApplies = DBService.Instance.Entities.TGuildApplySet.ToList();

            foreach(var member in allTGuildMembers)
            {
                this.CharacterGuildIdMap[member.CharacterID] = member.TGuildId;
            }

            var memberByGuild = allTGuildMembers.GroupBy(m => m.TGuildId).ToDictionary(g => g.Key, g => g.ToList());
            var applyByGuild = allTGuildApplies.GroupBy(m => m.TGuildId).ToDictionary(g => g.Key, g => g.ToList());


            foreach (TGuild tGuild in allTGuilds)
            {
                Guild guild = new Guild(tGuild);

                int guildId = guild.Data.Id;

                memberByGuild.TryGetValue(guildId, out List<TGuildMember> tGuildMembers);
                applyByGuild.TryGetValue(guildId, out List<TGuildApply> tGuildApply);

                guild.InitGuild(tGuildMembers, tGuildApply);
            }
        }

        /// <summary>
        /// 获取全服公会列表的网络数据
        /// </summary>
        /// <returns></returns>
        public List<NGuildInfo> GetGuildsInfo()
        {
            List<NGuildInfo> result = new List<NGuildInfo>();
            
            foreach(Guild guild in this.Guilds.Values) 
            {
                result.Add(guild.ToNGuildInfo());
            }

            return result;
        }

        /// <summary>
        /// 用guildId获取公会实体
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public Guild GetGuild(int guildId)
        {
            if(this.Guilds.TryGetValue(guildId, out Guild guild))
            {
                return guild;
            }
            return null;
        }

        /// <summary>
        /// 根据用户的ID查询所在的公会
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        public int GetGuildIdByCharacter(int characterId) {
            if (this.CharacterGuildIdMap.TryGetValue(characterId, out int guildId))
            {
                return guildId;
            }
            return 0; // 没查到就是没公会
        }

    }
}
