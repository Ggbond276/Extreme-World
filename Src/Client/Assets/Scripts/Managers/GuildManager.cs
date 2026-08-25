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
        /// 1. 无参的 Init：用于游戏中动态刷新（比如刚建好公会，或者申请刚被同意）
        /// 它的作用纯粹是清空旧列表并向服务器拉取最新名单
        /// </summary>
        public void Init()
        {
            myMembers.Clear();
            myApplies.Clear();
            guildHallList.Clear();

            GuildService.Instance.SendGuildMemberListRequest();
            GuildService.Instance.SendGuildApplyListRequest();
            GuildService.Instance.SendGuildListRequest();
        }

        /// <summary>
        /// 2. 带参的 Init：专门用于 OnGameEnter 登录时的初始化
        /// </summary>
        /// <param name="nGuildInfo"></param>
        public void Init(NGuildInfo nGuildInfo)
        {
            this.myGuildInfo = nGuildInfo; // 赋值大盘数据
            this.Init(); // 💡 神仙操作：直接调用上面的无参方法，避免代码重复！
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
            this.myApplies.Clear();
            foreach(var a in applies)
            {
                this.myApplies[a.CharacterId] = a;
            }
            this.OnGuildApplyChanged?.Invoke();
        }
        /// <summary>
        /// 收到后端传来的数据 对容器数据进行覆盖
        /// </summary>
        /// <param name="guilds"></param>
        public void RefreshGuildHall(List<NGuildInfo> guilds)
        {
            this.guildHallList.Clear();
            foreach(var g in guilds)
            {
                 this.guildHallList.Add(g);
            }
            this.OnGuildHallListChanged?.Invoke();
        }


        /// <summary>
        /// 新增成员或更新已有成员职位 (覆盖语义)
        /// </summary>
        /// <param name="member">发生变动的成员数据</param>
        public void AddOrUpdateMember(NGuildMember member)
        {
            myMembers[member.CharacterId] = member;
            OnGuildMemberChanged?.Invoke();
        }
        /// <summary>
        /// 从列表中移除指定成员
        /// </summary>
        /// <param name="characterId">要移除的成员ID</param>
        public void RemoveMember(int characterId)
        {
            if (myMembers.ContainsKey(characterId))
            {
                myMembers.Remove(characterId);
                OnGuildMemberChanged?.Invoke();
            }
        }


        /// <summary>
        /// 新增一条入会申请
        /// </summary>
        /// <param name="apply">新的申请数据</param>
        public void AddApply(NGuildApply apply)
        {
            myApplies[apply.CharacterId] = apply;
            OnGuildApplyChanged?.Invoke();
        }

        /// <summary>
        /// 移除一条入会申请
        /// </summary>
        /// <param name="characterId">要移除申请的玩家ID</param>
        public void RemoveApply(int characterId)
        {
            if (myApplies.ContainsKey(characterId))
            {
                myApplies.Remove(characterId);
                OnGuildApplyChanged?.Invoke();
            }
        }

        /// <summary>
        /// 更新我的公会大盘基础信息
        /// </summary>
        /// <param name="info">最新的公会信息</param>
        public void UpdateGuildInfo(NGuildInfo info)
        {
            myGuildInfo = info;
            OnGuildInfoChanged?.Invoke();
        }

        /// <summary>
        /// 清理我的本地公会数据 (用于自己退出公会、公会解散或被踢出时)
        /// </summary>
        public void ClearMyGuildData()
        {
            myGuildInfo = null;
            myMembers.Clear();
            myApplies.Clear();

            OnGuildInfoChanged?.Invoke();
            OnGuildMemberChanged?.Invoke();
        }


        // ==========================================
        // 对 UI 暴露的操作指令接口 (彻底隔离 Service)
        // 核心原则：只暴露业务动作，屏蔽网络发送(Send)语义
        // ==========================================

        /// <summary>
        /// 创建公会
        /// </summary>
        /// <param name="name">公会名称</param>
        /// <param name="notice">公会宗旨/公告</param>
        /// <param name="level">最低加入等级</param>
        public void CreateGuild(string name, string notice, int level)
        {
            // 在这里调用底层的 Service 发送网络包
            GuildService.Instance.SendGuildCreate(name, notice, level);
        }

        /// <summary>
        /// 申请加入公会
        /// </summary>
        /// <param name="guildId">目标公会ID</param>
        public void ApplyGuild(int guildId)
        {
            GuildService.Instance.SendGuildJoinApply(guildId);
        }

        /// <summary>
        /// 审批玩家申请 (同意/拒绝)
        /// </summary>
        /// <param name="characterId">申请人ID</param>
        /// <param name="isAccept">是否同意</param>
        public void ProcessApply(int characterId, bool isAccept)
        {
            GuildApplyProcessCommand command = GuildApplyProcessCommand.Reject;
            if (isAccept)
                command = GuildApplyProcessCommand.Accept;
            GuildService.Instance.SendGuildApplyProcess(characterId, command);
        }

        /// <summary>
        /// 离开/退出公会
        /// </summary>
        public void LeaveGuild()
        {
            GuildService.Instance.SendGuildLeave();
        }

        /// <summary>
        /// 对公会成员进行管理操作 (如踢出、升降职、转让会长)
        /// </summary>
        /// <param name="targetId">目标成员的角色ID</param>
        /// <param name="command">操作指令枚举 (比如 GuildAdminCommand)</param>
        public void AdminMember(int targetId, GuildAdminCommand command)
        {
            GuildService.Instance.SendGuildAdmin(targetId, command);
        }

        /// <summary>
        /// 修改公会设置 (比如会长修改宗旨或最低等级)
        /// </summary>
        /// <param name="notice">新宗旨</param>
        /// <param name="joinLevel">新加入等级限制</param>
        public void ModifyGuildSettings(string notice, int joinLevel)
        {
            // 假设你的 Service 叫 SendGuildSettingModify
            GuildService.Instance.SendGuildSettingModify(notice, joinLevel);
        }

        /// <summary>
        /// 刷新大厅公会列表
        /// </summary>
        public void RefreshGuildHallList()
        {
            GuildService.Instance.SendGuildListRequest();
        }
        /// <summary>
        /// 解散公会
        /// </summary>
        public void DisbanGuild()
        {
            GuildService.Instance.SendGuildDisband();
        }
    }
}
