using Common;
using GameServer.Entities;
using GameServer.Managers;
using GameServer.Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;

namespace GameServer.Services
{
    class GuildService : Singleton<GuildService>
    {
        public GuildService()
        {
            Log.Info("GuildService : 服务端公会服务初始化并订阅网络协议");

            // 注册所有的公会网络协议路由
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
        // 原则一：指令响应与数据同步分离 (Response & Notify)
        // ==========================================

        /// <summary>
        /// 处理：创建公会请求
        /// </summary>
        private void OnGuildCreateRequest(NetConnection<NetSession> sender, GuildCreateRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("OnGuildCreateRequest : 收到创建公会请求, 申请人 ID:{0} Name:{1}, 拟建公会名:{2}, 限制等级:{3}", character.Data.ID, character.Data.Name, request.GuildName, request.ReqLevel);

            sender.Session.Response.guildCreate = new GuildCreateResponse();

            if (character.GuildId > 0)
            {
                Log.WarningFormat("OnGuildCreateRequest : 创建失败, 玩家已在公会中, 申请人 ID:{0}, 当前公会 ID:{1}", character.Data.ID, character.GuildId);
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "你已经在一个公会中了，无法创建新公会";
                sender.SendResponse();
                return;
            }

            // 金币校验逻辑略...

            Guild newGuild = GuildManager.Instance.CreateGuild(request.GuildName, request.Notice, request.ReqLevel, character);
            if (newGuild == null)
            {
                Log.ErrorFormat("OnGuildCreateRequest : 创建底层事务失败或公会重名, 拟建公会名:{0}, 申请人 ID:{1}", request.GuildName, character.Data.ID);
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "公会名称已存在或创建失败";
                sender.SendResponse();
                return;
            }

            Log.InfoFormat("OnGuildCreateRequest : 创建成功, 新公会 ID:{0}, 会长 ID:{1}", newGuild.Data.Id, character.Data.ID);

            // [特例响应]: 只有创建公会时，Response 会带大盘数据，因为操作者需要拿它初始化自己空荡荡的 Manager
            sender.Session.Response.guildCreate.Result = Result.Success;
            sender.Session.Response.guildCreate.Guild = newGuild.ToNGuildInfo();
            sender.SendResponse();
        }

        /// <summary>
        /// 处理：解散公会请求
        /// </summary>
        private void OnGuildDisbandRequest(NetConnection<NetSession> sender, GuildDisbandRequest request)
        {
            int characterId = sender.Session.Character.Data.ID;
            Log.InfoFormat("OnGuildDisbandRequest : 收到解散公会请求, 发起人 ID:{0}", characterId);

            sender.Session.Response.guildDisband = new GuildDisbandResponse();

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if (guild == null || guild.GetGuildMember(characterId)?.Data.Position != (int)GuildPosition.GuildPositionLeader)
            {
                Log.WarningFormat("OnGuildDisbandRequest : 解散失败, 权限不足或不在公会中, 发起人 ID:{0}", characterId);
                sender.Session.Response.guildDisband.Result = Result.Failed;
                sender.Session.Response.guildDisband.Errormsg = "权限不足或不在公会中";
                sender.SendResponse();
                return;
            }

            var onlineConnections = guild.GetOnlineSessions();
            bool success = GuildManager.Instance.DisbandGuild(guildId, characterId);
            if (!success)
            {
                Log.ErrorFormat("OnGuildDisbandRequest : 解散底层事务失败, 公会 ID:{0}", guildId);
                sender.Session.Response.guildDisband.Result = Result.Failed;
                sender.Session.Response.guildDisband.Errormsg = "解散公会失败";
                sender.SendResponse();
                return;
            }

            Log.InfoFormat("OnGuildDisbandRequest : 解散成功, 公会 ID:{0} 已注销, 准备广播解散通知", guildId);
            // 1. 【状态响应】给会长：解散成功，前端关闭 UI
            sender.Session.Response.guildDisband.Result = Result.Success;
            sender.SendResponse();

            // 2. 【组播同步】给所有其他在线成员：推送移除指令 (按 ID 删除)
            foreach (var connection in onlineConnections)
            {
                if (connection != null && connection.Session.Character.Data.ID != characterId)
                {
                    connection.Session.Response.guildMemberRemoveNotify = new GuildMemberRemoveNotify();
                    connection.Session.Response.guildMemberRemoveNotify.CharacterId = connection.Session.Character.Data.ID;
                    connection.SendResponse();
                }
            }
        }

        /// <summary>
        /// 处理：修改公会设置请求
        /// </summary>
        private void OnGuildSettingModifyRequest(NetConnection<NetSession> sender, GuildSettingModifyRequest request)
        {
            int characterId = sender.Session.Character.Data.ID;
            Log.InfoFormat("OnGuildSettingModifyRequest : 收到修改公会设置请求, 发起人 ID:{0}", characterId);

            sender.Session.Response.guildSettingModify = new GuildSettingModifyResponse();

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            // 权限拦截略...

            bool success = GuildManager.Instance.ModifyGuildSettings(guildId, request.NewNotice, request.NewReqLevel);
            if (!success)
            {
                Log.ErrorFormat("OnGuildSettingModifyRequest : 修改设置底层事务失败, 公会 ID:{0}", guildId);
                sender.Session.Response.guildSettingModify.Result = Result.Failed;
                sender.Session.Response.guildSettingModify.Errormsg = "修改设置失败";
                sender.SendResponse();
                return;
            }

            Log.InfoFormat("OnGuildSettingModifyRequest : 修改设置成功, 公会 ID:{0}, 准备广播大盘信息更新", guildId);
            // 1. 【状态响应】给操作者：只回 Success，没有任何冗余的 Updated 字段了
            sender.Session.Response.guildSettingModify.Result = Result.Success;
            sender.SendResponse();

            // 2. 【组播同步】给全公会所有人 (含操作者)：推送公会信息变更指令
            var onlineConnections = guild.GetOnlineSessions();
            foreach (var connection in onlineConnections)
            {
                if (connection != null)
                {
                    connection.Session.Response.guildInfoChangeNotify = new GuildInfoChangeNotify();
                    connection.Session.Response.guildInfoChangeNotify.GuildInfo = guild.ToNGuildInfo();
                    connection.SendResponse();
                }
            }
        }

        /// <summary>
        /// 处理：申请加入公会请求
        /// </summary>
        private void OnGuildJoinApplyRequest(NetConnection<NetSession> sender, GuildJoinApplyRequest request)
        {
            int characterId = sender.Session.Character.Data.ID;
            Log.InfoFormat("OnGuildJoinApplyRequest : 收到加入公会申请, 申请人 ID:{0}, 目标公会 ID:{1}", characterId, request.TargetGuildId);

            Guild guild = GuildManager.Instance.GetGuild(request.TargetGuildId);
            sender.Session.Response.guildJoinApply = new GuildJoinApplyResponse();

            if (sender.Session.Character.GuildId != 0 || guild == null)
            {
                Log.WarningFormat("OnGuildJoinApplyRequest : 申请入会失败, 玩家已在公会中或目标公会不存在, 申请人 ID:{0}", characterId);
                sender.Session.Response.guildJoinApply.Result = Result.Failed;
                sender.Session.Response.guildJoinApply.Errormsg = "无法申请";
                sender.SendResponse();
                return;
            }

            bool success = GuildManager.Instance.ApplyGuild(request.TargetGuildId, characterId);
            if (!success)
            {
                Log.WarningFormat("OnGuildJoinApplyRequest : 申请入会失败, 防刷拦截(已申请过)或事务异常, 申请人 ID:{0}", characterId);
                sender.Session.Response.guildJoinApply.Result = Result.Failed;
                sender.Session.Response.guildJoinApply.Errormsg = "申请失败";
                sender.SendResponse();
                return;
            }

            Log.InfoFormat("OnGuildJoinApplyRequest : 投递申请成功, 申请人 ID:{0}, 目标公会 ID:{1}, 准备广播新增申请", characterId, request.TargetGuildId);
            // 1. 【状态响应】给申请人：成功投递，前端解除按钮封印
            sender.Session.Response.guildJoinApply.Result = Result.Success;
            sender.SendResponse();

            // 2. 【组播同步】给全服在线管理员：新增一条申请记录实体
            var onlineAdmins = guild.GetOnlineAdminSessions();
            foreach (var connection in onlineAdmins)
            {
                if (connection != null)
                {
                    connection.Session.Response.guildApplyAddNotify = new GuildApplyAddNotify();
                    connection.Session.Response.guildApplyAddNotify.NewApply = guild.GetGuildApply(characterId).ToNGuildApply();
                    connection.SendResponse();
                }
            }
        }

        /// <summary>
        /// 处理：公会审批请求 (管理员操作)
        /// </summary>
        private void OnGuildApplyProcessRequest(NetConnection<NetSession> sender, GuildApplyProcessRequest request)
        {
            Log.InfoFormat("OnGuildApplyProcessRequest : 收到审批操作请求, 审批人 ID:{0}, 被审批人 ID:{1}, 指令:{2}", sender.Session.Character.Data.ID, request.ApplicantCharacterId, request.Command);

            Guild guild = GuildManager.Instance.GetGuild(sender.Session.Character.GuildId);
            if (guild == null) return;

            int applicantId = request.ApplicantCharacterId;
            bool isAccept = request.Command == GuildApplyProcessCommand.Accept;
            sender.Session.Response.guildApplyProcess = new GuildApplyProcessResponse();

            bool success = GuildManager.Instance.ProcessApply(guild.Data.Id, applicantId, isAccept);
            if (!success)
            {
                Log.ErrorFormat("OnGuildApplyProcessRequest : 审批底层事务执行失败, 公会 ID:{0}, 被审批人 ID:{1}", guild.Data.Id, applicantId);
                sender.Session.Response.guildApplyProcess.Result = Result.Failed;
                sender.Session.Response.guildApplyProcess.Errormsg = "审批失败";
                sender.SendResponse();
                return;
            }

            Log.InfoFormat("OnGuildApplyProcessRequest : 审批操作成功落实, 公会 ID:{0}, 是否同意:{1}, 准备执行广播同步", guild.Data.Id, isAccept);
            // ==================== 极其优雅的 1 响应 + 3 同步模型 ====================

            // 1. 【状态响应】给操作者：纯净的 Success，彻底解耦
            sender.Session.Response.guildApplyProcess.Result = Result.Success;
            sender.SendResponse();

            // 2. 【同步数据 - 删】向所有管理员广播：把这条申请按 ID 删掉 (无论同意拒绝都得删)
            var onlineAdmins = guild.GetOnlineAdminSessions();
            foreach (var adminConn in onlineAdmins)
            {
                if (adminConn != null)
                {
                    adminConn.Session.Response.guildApplyRemoveNotify = new GuildApplyRemoveNotify();
                    adminConn.Session.Response.guildApplyRemoveNotify.CharacterId = applicantId;
                    adminConn.SendResponse();
                }
            }

            // 3. 【同步数据 - 结果】向那个等待的申请人推送命运宣告 (极简推送)
            var applicantConnection = SessionManager.Instance.GetSession(applicantId);
            if (applicantConnection != null)
            {
                applicantConnection.Session.Response.guildApplyResultNotify = new GuildApplyResultNotify();
                applicantConnection.Session.Response.guildApplyResultNotify.GuildId = guild.Data.Id;
                applicantConnection.Session.Response.guildApplyResultNotify.IsAccept = isAccept;
                if (isAccept) applicantConnection.Session.Response.guildApplyResultNotify.GuildName = guild.Data.Name;
                applicantConnection.SendResponse();
            }

            // 4. 【同步数据 - 增】如果同意，向所有成员广播新成员名片
            if (isAccept)
            {
                GuildMember newMember = guild.GetGuildMember(applicantId);
                if (newMember != null)
                {
                    var onlineConnections = guild.GetOnlineSessions();
                    foreach (var connection in onlineConnections)
                    {
                        if (connection != null && connection.Session.Character.Data.ID != applicantId)
                        {
                            connection.Session.Response.guildMemberAddNotify = new GuildMemberAddNotify();
                            connection.Session.Response.guildMemberAddNotify.NewMember = newMember.ToNGuildMember();
                            connection.SendResponse();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理：离开公会请求
        /// </summary>
        private void OnGuildLeaveRequest(NetConnection<NetSession> sender, GuildLeaveRequest request)
        {
            int characterId = sender.Session.Character.Data.ID;
            Log.InfoFormat("OnGuildLeaveRequest : 收到退出公会请求, 申请人 ID:{0}", characterId);

            sender.Session.Response.guildLeave = new GuildLeaveResponse();

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if (guild == null) return;
            // 会长离开拦截略...

            bool success = GuildManager.Instance.LeaveGuild(guildId, characterId);
            if (!success)
            {
                Log.ErrorFormat("OnGuildLeaveRequest : 退出公会底层事务失败, 公会 ID:{0}, 申请人 ID:{1}", guildId, characterId);
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "退出失败";
                sender.SendResponse();
                return;
            }

            Log.InfoFormat("OnGuildLeaveRequest : 退出公会成功, 申请人 ID:{0}, 准备广播移除成员通知", characterId);
            // 1. 【状态响应】给操作者：纯净的 Success
            sender.Session.Response.guildLeave.Result = Result.Success;
            sender.SendResponse();

            // 2. 【组播同步】给所有仍然在公会的老成员：推送移除指令 (按 ID 删除)
            var onlineConnections = guild.GetOnlineSessions();
            foreach (var connection in onlineConnections)
            {
                if (connection != null)
                {
                    connection.Session.Response.guildMemberRemoveNotify = new GuildMemberRemoveNotify();
                    connection.Session.Response.guildMemberRemoveNotify.CharacterId = characterId;
                    connection.SendResponse();
                }
            }
        }

        // ==========================================
        // 原则二：全量按需拉取接口 (Pull Once)
        // ==========================================

        private void OnGuildMemberListRequest(NetConnection<NetSession> sender, GuildMemberListRequest request)
        {
            Log.InfoFormat("OnGuildMemberListRequest : 收到全量拉取成员列表请求, 发起人 ID:{0}", sender.Session.Character.Data.ID);

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(sender.Session.Character.Data.ID);
            Guild guild = GuildManager.Instance.GetGuild(guildId);
            sender.Session.Response.guildMemberList = new GuildMemberListResponse();

            if (guild != null)
            {
                sender.Session.Response.guildMemberList.Result = Result.Success;
                sender.Session.Response.guildMemberList.Members.AddRange(guild.GetNGuildMembers());
                Log.InfoFormat("OnGuildMemberListRequest : 成功下发全量成员列表, 共计 {0} 人", sender.Session.Response.guildMemberList.Members.Count);
            }
            else
            {
                Log.WarningFormat("OnGuildMemberListRequest : 全量拉取成员列表失败, 发起人不在公会中");
                sender.Session.Response.guildMemberList.Result = Result.Failed;
            }
            sender.SendResponse();
        }

        private void OnGuildApplyListRequest(NetConnection<NetSession> sender, GuildApplyListRequest request)
        {
            Log.InfoFormat("OnGuildApplyListRequest : 收到全量拉取申请列表请求, 发起人 ID:{0}", sender.Session.Character.Data.ID);

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(sender.Session.Character.Data.ID);
            Guild guild = GuildManager.Instance.GetGuild(guildId);
            sender.Session.Response.guildApplyList = new GuildApplyListResponse();

            if (guild != null)
            {
                sender.Session.Response.guildApplyList.Result = Result.Success;
                sender.Session.Response.guildApplyList.Applies.AddRange(guild.GetNGuildApplies());
                Log.InfoFormat("OnGuildApplyListRequest : 成功下发全量申请列表, 共计 {0} 条", sender.Session.Response.guildApplyList.Applies.Count);
            }
            sender.SendResponse();
        }

        private void OnGuildListRequest(NetConnection<NetSession> sender, GuildListRequest request)
        {
            Log.InfoFormat("OnGuildListRequest : 收到全量拉取大厅公会列表请求, 发起人 ID:{0}", sender.Session.Character.Data.ID);

            sender.Session.Response.guildList = new GuildListResponse();
            sender.Session.Response.guildList.Result = Result.Success;
            sender.Session.Response.guildList.Guilds.AddRange(GuildManager.Instance.GetGuildsInfo());

            Log.InfoFormat("OnGuildListRequest : 成功下发公会大厅列表, 共计 {0} 个公会", sender.Session.Response.guildList.Guilds.Count);
            sender.SendResponse();
        }

        // ==========================================
        // 公会管理操作 (踢人、升降职、转让)
        // ==========================================
        private void OnGuildAdminRequest(NetConnection<NetSession> sender, GuildAdminRequest request)
        {
            int characterId = sender.Session.Character.Data.ID;
            Log.InfoFormat("OnGuildAdminRequest : 收到公会管理操作请求, 操作者 ID:{0}, 目标 ID:{1}, 指令:{2}", characterId, request.TargetCharacterId, request.Command);

            sender.Session.Response.guildAdmin = new GuildAdminResponse();

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if (guild == null)
            {
                Log.WarningFormat("OnGuildAdminRequest : 管理操作失败, 操作者不在公会中, 操作者 ID:{0}", characterId);
                sender.Session.Response.guildAdmin.Result = Result.Failed;
                sender.Session.Response.guildAdmin.Errormsg = "你当前不在公会中";
                sender.SendResponse();
                return;
            }

            bool success = GuildManager.Instance.ExcuteAdminCommand(guildId, characterId, request.TargetCharacterId, request.Command);
            if (!success)
            {
                Log.WarningFormat("OnGuildAdminRequest : 管理操作失败, 越权防刷或底层事务失败, 操作者 ID:{0}, 目标 ID:{1}", characterId, request.TargetCharacterId);
                sender.Session.Response.guildAdmin.Result = Result.Failed;
                sender.Session.Response.guildAdmin.Errormsg = "操作失败：权限不足或目标无效";
                sender.SendResponse();
                return;
            }

            Log.InfoFormat("OnGuildAdminRequest : 管理操作底层事务成功落实, 准备广播管理操作通知");
            sender.Session.Response.guildAdmin.Result = Result.Success;
            sender.SendResponse();

            var onlineConnections = guild.GetOnlineSessions();

            // 分支 1：如果是【踢出成员】 (执行 Remove 语义)
            if (request.Command == GuildAdminCommand.CommandKickMember)
            {
                foreach (var conn in onlineConnections)
                {
                    if (conn != null)
                    {
                        conn.Session.Response.guildMemberRemoveNotify = new GuildMemberRemoveNotify();
                        conn.Session.Response.guildMemberRemoveNotify.CharacterId = request.TargetCharacterId;
                        conn.SendResponse();
                    }
                }

                // 单独推送给那个被踢出的“倒霉蛋”
                var kickSession = SessionManager.Instance.GetSession(request.TargetCharacterId);
                if (kickSession != null)
                {
                    kickSession.Session.Response.guildMemberRemoveNotify = new GuildMemberRemoveNotify();
                    kickSession.Session.Response.guildMemberRemoveNotify.CharacterId = request.TargetCharacterId;
                    kickSession.SendResponse();
                }
            }
            // 分支 2：如果是【职位变动】 (执行 Upsert 覆盖语义)
            else
            {
                GuildMember targetMember = guild.GetGuildMember(request.TargetCharacterId);
                GuildMember operatorMember = guild.GetGuildMember(characterId);

                foreach (var conn in onlineConnections)
                {
                    if (conn != null)
                    {
                        // 1. 同步目标玩家的新名片 (如：被升为了副会长)
                        if (targetMember != null)
                        {
                            conn.Session.Response.guildMemberAddNotify = new GuildMemberAddNotify();
                            conn.Session.Response.guildMemberAddNotify.NewMember = targetMember.ToNGuildMember();
                            conn.SendResponse();
                        }

                        // 2. 特殊情况：如果是【转让会长】
                        if (request.Command == GuildAdminCommand.CommandTransferLeader)
                        {
                            // 老会长降职了，也需要同步老会长的新名片
                            if (operatorMember != null)
                            {
                                conn.Session.Response.guildMemberAddNotify = new GuildMemberAddNotify();
                                conn.Session.Response.guildMemberAddNotify.NewMember = operatorMember.ToNGuildMember();
                                conn.SendResponse();
                            }

                            // 公会大盘基础信息里的 LeaderName 发生了变化，触发大盘覆盖
                            conn.Session.Response.guildInfoChangeNotify = new GuildInfoChangeNotify();
                            conn.Session.Response.guildInfoChangeNotify.GuildInfo = guild.ToNGuildInfo();
                            conn.SendResponse();
                        }
                    }
                }
            }
        }

        private void OnGuildChatRequest(NetConnection<NetSession> sender, GuildChatRequest request)
        {
            throw new NotImplementedException();
        }
    }
}