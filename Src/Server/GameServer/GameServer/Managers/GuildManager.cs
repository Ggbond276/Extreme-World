using Common;
using GameServer.Models;
using GameServer.Services;
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

        /// <summary>
        /// 初始化内存数据方法
        /// </summary>
        public void Init()
        {
            var allTGuilds = DBService.Instance.Entities.TGuildSet.ToList();
            var allTGuildMembers = DBService.Instance.Entities.TGuildMemberSet.ToList();
            var allTGuildApplies = DBService.Instance.Entities.TGuildApplySet.ToList();

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



    }
}
