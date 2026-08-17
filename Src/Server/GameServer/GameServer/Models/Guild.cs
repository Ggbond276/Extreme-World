using GameServer.Entities;
using GameServer.Manager;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    class Guild
    {
        public TGuild Data { get; private set; }
        public Dictionary<int, GuildMember> Members { get; private set; } = new Dictionary<int, GuildMember>();
        public Dictionary<int, GuildApply> Applies { get; private set; } = new Dictionary<int, GuildApply>();
        /// <summary>
        /// 脏标记
        /// </summary>
        public bool IsDirty { get; set; } = false;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="guildData"></param>
        public Guild(TGuild guildData)
        {
            this.Data = guildData;
        }

        /// <summary>
        /// 初始化公会
        /// </summary>
        /// <param name="tguildMembers"></param>
        /// <param name="tguildApplies"></param>
        public void InitGuild(List<TGuildMember> tguildMembers, List<TGuildApply> tguildApplies)
        {
            foreach(TGuildMember tGuildMember in tguildMembers)
            {
                GuildMember guildMember = new GuildMember(tGuildMember);
                int memberId = guildMember.Data.CharacterID;
                this.Members[memberId] = guildMember;
            }


            foreach(TGuildApply tguildApply in tguildApplies)
            {
                GuildApply guildApply = new GuildApply(tguildApply);
                int applicantId = guildApply.Data.CharacterID;
                this.Applies[applicantId] = guildApply;
            }
        }

        /// <summary>
        /// 服务端内存数据转网络数据
        /// </summary>
        /// <returns></returns>
        public NGuildInfo ToNGuildInfo()
        {
            // 不在线空指针异常问题
            Character character = CharacterManager.Instance.GetCharacter(this.Data.LeaderID);
            string leaderName = character.Data.Name;

            NGuildInfo nGuildInfo = new NGuildInfo();
            nGuildInfo.Id = this.Data.Id;
            nGuildInfo.Name = this.Data.Name;
            nGuildInfo.LeaderName = leaderName;
            nGuildInfo.Level = this.Data.Level;
            nGuildInfo.MemberCount = this.Data.MemberCount;
            nGuildInfo.ActivityLevel = this.Data.ActivityLevel;
            nGuildInfo.ReqLevel = this.Data.ReqLevel;
            nGuildInfo.Notice = this.Data.Notice;

            return nGuildInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<NGuildMember> GetNGuildMembers()
        {
            List<NGuildMember> nGuildMembers = new List<NGuildMember>();
            foreach(GuildMember guildMember in this.Members.Values)
            {
                nGuildMembers.Add(guildMember.ToNGuildMember());
            }

            return nGuildMembers;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<NGuildApply> GetNGuildApplies()
        {
            List<NGuildApply> nGuildApplies = new List<NGuildApply>();
            foreach (GuildApply guildApply in this.Applies.Values)
            {
                nGuildApplies.Add(guildApply.ToNGuildApply());
            }

            return nGuildApplies;
        }
 
    }
}
