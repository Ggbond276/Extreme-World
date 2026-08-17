using Common;
using GameServer.Managers;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
    class GuildService : Singleton<GuildService>
    {
        public GuildService()
        {
            Log.InfoFormat("GuildService: 服务端公会服务初始化并订阅网络协议");

            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildCreateRequest>(this.OnGuildCreateRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildDisbandRequest>(this.OnGuildDisbandRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildSettingModifyRequest>(this.OnGuildSettingModifyRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildJoinApplyRequest>(this.OnGuildJoinApplyRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildApplyProcessRequest>(this.OnGuildApplyProcessRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildLeaveRequest>(this.OnGuildLeaveRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildChatRequest>(this.OnGuildChatRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildMemberListRequest>(this.OnGuildMemberListRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildApplyListRequest>(this.OnGuildApplyListRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildListRequest>(this.OnGuildListRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildAdminRequest>(this.OnGuildAdminRequest);
        }

        public void Init()
        {
            GuildManager.Instance.Init();
        }


        // ==========================================
        // 网络请求处理方法 (Handlers)
        // ==========================================

        /// <summary>
        /// 处理：创建公会请求
        /// </summary>
        private void OnGuildCreateRequest(NetConnection<NetSession> sender, GuildCreateRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：解散公会请求
        /// </summary>
        private void OnGuildDisbandRequest(NetConnection<NetSession> sender, GuildDisbandRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：修改公会设置请求 (如修改宗旨、加入等级限制)
        /// </summary>
        private void OnGuildSettingModifyRequest(NetConnection<NetSession> sender, GuildSettingModifyRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：申请加入公会请求 (玩家发起)
        /// </summary>
        private void OnGuildJoinApplyRequest(NetConnection<NetSession> sender, GuildJoinApplyRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：公会审批请求 (管理员同意/拒绝玩家加入)
        /// </summary>
        private void OnGuildApplyProcessRequest(NetConnection<NetSession> sender, GuildApplyProcessRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：离开公会请求 (玩家主动退出)
        /// </summary>
        private void OnGuildLeaveRequest(NetConnection<NetSession> sender, GuildLeaveRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：公会聊天请求
        /// </summary>
        private void OnGuildChatRequest(NetConnection<NetSession> sender, GuildChatRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：获取公会成员列表请求
        /// </summary>
        private void OnGuildMemberListRequest(NetConnection<NetSession> sender, GuildMemberListRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：获取公会申请列表请求 (仅管理员可看)
        /// </summary>
        private void OnGuildApplyListRequest(NetConnection<NetSession> sender, GuildApplyListRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：获取全服公会列表请求 (用于大厅面板展示)
        /// </summary>
        private void OnGuildListRequest(NetConnection<NetSession> sender, GuildListRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：公会管理操作请求 (踢出成员、升降职等)
        /// </summary>
        private void OnGuildAdminRequest(NetConnection<NetSession> sender, GuildAdminRequest request)
        {
            throw new NotImplementedException();
        }

    }
}
