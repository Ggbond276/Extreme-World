using Common;
using GameServer.Entities;
using GameServer.Managers;
using GameServer.Models;
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
            sender.Session.Response.guildCreate = new GuildCreateResponse();
            Character character = sender.Session.Character;
            
            if(character.GuildId > 0)
            {
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "你已经在一个公会中了，无法创建新公会";
                sender.SendResponse();
                return;
            }

            if (string.IsNullOrWhiteSpace(request.GuildName))
            {
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "公会名称不能为空";
                sender.SendResponse();
                return;
            }


            if (request.ReqLevel < 0 || request.ReqLevel > 100)
            {
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "入会等级条件设置非法";
                sender.SendResponse();
                return;
            }

            // TODO: 拓展预留 - 以后要把公会人数上限、创建基础消耗(100000)等抽离到静态配置表中 (如 DataManager.Instance.GuildConfig)
            // 这样未来全服更新提价或修改上限时，直接替换配表即可，无需修改代码逻辑。
            long createCost = 100000;
            if (character.Gold < createCost)
            {
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "金币不足，创建公会需要 100,000 金币";
                sender.SendResponse();
                return;
            }

            Guild newGuild = GuildManager.Instance.CreateGuild(request.GuildName, request.Notice, request.ReqLevel, character);

            if (newGuild == null)
            {
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "公会名称已存在或创建失败";
                sender.SendResponse();
                return;
            }

            character.Gold -= createCost;

            sender.Session.Response.guildCreate.Result = Result.Success;
            sender.Session.Response.guildCreate.Errormsg = "创建公会成功";
            sender.Session.Response.guildCreate.Guild = newGuild.ToNGuildInfo(); // 直接下发最新的大盘数据供 UI 渲染[cite: 1]
            sender.SendResponse();
        }

        /// <summary>
        /// 处理：解散公会请求
        /// </summary>
        private void OnGuildDisbandRequest(NetConnection<NetSession> sender, GuildDisbandRequest request)
        {
            sender.Session.Response.guildDisband = new GuildDisbandResponse();
            Character character = sender.Session.Character;
            int characterId = character.Data.ID;
            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if(guild == null)
            {
                sender.Session.Response.guildDisband.Result = Result.Failed;
                sender.Session.Response.guildDisband.Errormsg = "你当前不在任何公会中";
                sender.SendResponse();
                return;
            }

            GuildMember guildMember = guild.GetGuildMember(characterId);
            if(guildMember.Data.Position != (int)GuildPosition.GuildPositionLeader)
            {
                sender.Session.Response.guildDisband.Result = Result.Failed;
                sender.Session.Response.guildDisband.Errormsg = "权限不足，只有会长可以解散公会";
                sender.SendResponse();
                return;
            }


            var onlineConnections = guild.GetOnlineSessions();
            bool success =  GuildManager.Instance.DisbandGuild(guildId, characterId);
            if(!success)
            {
                sender.Session.Response.guildDisband.Result = Result.Failed;
                sender.Session.Response.guildDisband.Errormsg = "解散公会失败，请联系管理员";
                sender.SendResponse();
                return;
            }

            sender.Session.Response.guildDisband.Result = Result.Success;
            sender.SendResponse();

            foreach(var connection in onlineConnections)
            {
                if(connection != null 
                    && connection.Session.Character.Data.ID != characterId)
                {
                    connection.Session.Response.guildMemberLeaveNotify = new GuildMemberLeaveNotify();
                    connection.Session.Response.guildMemberLeaveNotify.CharacterId = connection.Session.Character.Data.ID;
                    connection.SendResponse();
                }
            }

        }

        /// <summary>
        /// 处理：修改公会设置请求 (如修改宗旨、加入等级限制)
        /// </summary>
        private void OnGuildSettingModifyRequest(NetConnection<NetSession> sender, GuildSettingModifyRequest request)
        {
            sender.Session.Response.guildSettingModify = new GuildSettingModifyResponse();
            Character character = sender.Session.Character;
            int characterId = character.Data.ID;

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if(guild == null)
            {
                return;
            }


            GuildMember guildMember = guild.GetGuildMember(characterId);
            if(guildMember.Data.Position == (int)GuildPosition.GuildPositionMember 
                 || guildMember.Data.Position == (int)GuildPosition.GuildPositionNone)
            {
                return;
            }

            // -1说明改成无条件, 条件范围是0-500
            if(request.NewReqLevel != -1 && (request.NewReqLevel < 0 || request.NewReqLevel > 500))
            {
                return;
            }

            bool success = GuildManager.Instance.ModifyGuildSeetings(guildId, request.NewNotice, request.NewReqLevel);
            if (!success)
            {
                sender.Session.Response.guildSettingModify.Result = Result.Failed;
                sender.Session.Response.guildSettingModify.Errormsg = "修改设置失败";
                sender.SendResponse();
                return;
            }

            sender.Session.Response.guildSettingModify.Result = Result.Success;
            sender.Session.Response.guildSettingModify.UpdatedNotice = guild.Data.Notice;
            sender.Session.Response.guildSettingModify.UpdatedReqLevel = guild.Data.ReqLevel;
            sender.SendResponse();

            var onlineConnections = guild.GetOnlineSessions();
            foreach(var connection in onlineConnections)
            {
                if(connection != null && connection.Session.Character.Data.ID != characterId)
                {
                    connection.Session.Response.guildInfoChangeNotify = new GuildInfoChangeNotify();
                    connection.Session.Response.guildInfoChangeNotify.GuildInfo = guild.ToNGuildInfo();
                    connection.SendResponse();
                }
            }
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
            Character character = sender.Session.Character;
            int characterId = character.Data.ID;
            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if(guild == null)
            {
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "你当前不在任何公会中";
                sender.SendResponse();
                return;
            }

            GuildMember guildMember = guild.GetGuildMember(characterId);
            if (guildMember == null)
            {
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "你不是该公会成员";
                sender.SendResponse();
                return;
            }

            if(guildMember.Data.Position == (int)GuildPosition.GuildPositionLeader)
            {
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "会长不能直接退出公会，请先转让会长或解散公会";
                sender.SendResponse();
                return;
            }


            bool success = GuildManager.Instance.LeaveGuild(guildId, characterId);
            if(!success)
            {
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "退出公会失败";
                sender.SendResponse();
                return;
            }


            var onlineConnections = guild.GetOnlineSessions();

            foreach(var connection in onlineConnections)
            {
                connection.Session.Response.guildMemberLeaveNotify = new GuildMemberLeaveNotify();
                connection.Session.Response.guildMemberLeaveNotify.CharacterId = characterId;
                connection.SendResponse();
            }

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
            sender.Session.Response.guildMemberList = new GuildMemberListResponse();

            Character character = sender.Session.Character;
            int characterId = character.Data.ID;

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if (guild != null)
            {
                sender.Session.Response.guildMemberList.Members.AddRange(guild.GetNGuildMembers());
                sender.Session.Response.guildMemberList.Result = Result.Success;
            }
            else
            {
                // 公会不存在了，优雅地告知前端
                sender.Session.Response.guildMemberList.Result = Result.Failed;
                sender.Session.Response.guildMemberList.Errormsg = "公会不存在或已解散";
            }

            sender.SendResponse();

        }

        /// <summary>
        /// 处理：获取公会申请列表请求 (仅管理员可看)
        /// </summary>
        private void OnGuildApplyListRequest(NetConnection<NetSession> sender, GuildApplyListRequest request)
        {
            sender.Session.Response.guildApplyList = new GuildApplyListResponse();

            Character character = sender.Session.Character;
            int characterId = character.Data.ID;

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if (guild != null)
            {
                //  核心防御：权限校验！必须通过成员字典查到自己，且职位必须大于等于副会长 (假设业务逻辑里 1是会长，2是副会长)
                if (guild.Members.TryGetValue(characterId, out GuildMember myMember) &&
                    myMember.Data.Position <= (int)GuildPosition.GuildPositionViceLeader)
                {
                    sender.Session.Response.guildApplyList.Applies.AddRange(guild.GetNGuildApplies());
                    sender.Session.Response.guildApplyList.Result = Result.Success;
                }
                else
                {
                    sender.Session.Response.guildApplyList.Result = Result.Failed;
                    sender.Session.Response.guildApplyList.Errormsg = "权限不足：只有会长或副会长可查看申请列表";
                }
            }
            else
            {
                // 公会不存在了，优雅地告知前端
                sender.Session.Response.guildApplyList.Result = Result.Failed;
                sender.Session.Response.guildApplyList.Errormsg = "公会不存在或已解散";
            }

            sender.SendResponse();
        }

        /// <summary>
        /// 处理：获取全服公会列表请求 (用于大厅面板展示)
        /// </summary>
        private void OnGuildListRequest(NetConnection<NetSession> sender, GuildListRequest request)
        {
            sender.Session.Response.guildList = new GuildListResponse();

            List<NGuildInfo> nGuilds = GuildManager.Instance.GetGuildsInfo();

            sender.Session.Response.guildList.Guilds.AddRange(nGuilds);
            sender.Session.Response.guildList.Result = Result.Success;

            sender.SendResponse();

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
