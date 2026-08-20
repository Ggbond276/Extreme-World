using Assets.Scripts.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace Assets.Scripts.Managers
{
    class GuildManager : Singleton<GuildManager>
    {

        // ==========================================
        // 核心原则：私有容器，绝对封装
        // ==========================================
        private NGuildInfo myGuildInfo = null;
        private Dictionary<int, NGuildMember> myMembers = new Dictionary<int, NGuildMember>();
        private Dictionary<int, NGuildApply> myApplies = new Dictionary<int, NGuildApply>();
        private List<NGuildInfo> guildHallList = new List<NGuildInfo>();

        // ==========================================
        // 对 UI 暴露的安全访问属性 (只读引用)
        // ==========================================
        public bool HasGuild { get { return myGuildInfo != null; } }
        public NGuildInfo MyGuildInfo { get { return myGuildInfo; } }
        public Dictionary<int, NGuildMember> MyMembers { get { return myMembers; } }
        public Dictionary<int, NGuildApply> MyApplies { get { return myApplies; } }
        public List<NGuildInfo> GuildHallList { get { return guildHallList; } }

        // ==========================================
        // 数据变更事件 (Manager 在数据变动后自行抛出)
        // ==========================================
        public Action OnGuildInfoChanged;
        public Action OnGuildMemberChanged;
        public Action OnGuildApplyChanged;
        public Action OnGuildHallListChanged;


        /// <summary>
        /// 随 OnGameEnter 触发的唯一一次初始化拉取
        /// </summary>
        public void Init()
        {
            myGuildInfo = null;
            myMembers.Clear();
            myApplies.Clear();
            guildHallList.Clear();

            GuildService.Instance.SendGuildMemberListRequest();
            GuildService.Instance.SendGuildApplyListRequest();
            GuildService.Instance.SendGuildListRequest();
        }

        public void RefreshMembers(List<NGuildMember> members)
        {
            this.myMembers.Clear();
            foreach(var m in members)
            {
                this.myMembers[m.CharacterId] = m;
            }
            this.OnGuildMemberChanged?.Invoke();
        }
        public void RefreshApplies(List<NGuildApply> applies)
        {
            this.myMembers.Clear();
            foreach(var a in applies)
            {
                this.myApplies[a.CharacterId] = a;
            }
            this.OnGuildApplyChanged?.Invoke();
        }
        public void RefreshGuildHall(List<NGuildInfo> guilds)
        {
            this.guildHallList.Clear();
            foreach(var g in guilds)
            {
                this.guildHallList[g.Id] = g;
            }
            this.OnGuildHallListChanged?.Invoke();
        }




     }
}
