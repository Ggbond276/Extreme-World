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
            // 唤醒底层的 Manager 进行数据初始化
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
            Character character = sender.Session.Character;
            Log.InfoFormat("OnGuildCreateRequest : 收到创建公会请求, CharacterID:{0}, 申请公会名:{1}, 限制等级:{2}", character.Data.ID, request.GuildName, request.ReqLevel);

            sender.Session.Response.guildCreate = new GuildCreateResponse();

            // 拦截 1：玩家当前已经在一个公会中，禁止脚踏两只船
            if (character.GuildId > 0)
            {
                Log.WarningFormat("OnGuildCreateRequest : 创建公会失败, 玩家已存在公会中, CharacterID:{0}, 当前GuildID:{1}", character.Data.ID, character.GuildId);
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "你已经在一个公会中了，无法创建新公会";
                sender.SendResponse();
                return;
            }

            // 拦截 2：公会名称不能为空白字符
            if (string.IsNullOrWhiteSpace(request.GuildName))
            {
                Log.WarningFormat("OnGuildCreateRequest : 创建公会失败, 公会名称为空, CharacterID:{0}", character.Data.ID);
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "公会名称不能为空";
                sender.SendResponse();
                return;
            }

            // 拦截 3：入会等级限制参数越界，防止前端篡改发脏数据
            if (request.ReqLevel < 0 || request.ReqLevel > 100)
            {
                Log.WarningFormat("OnGuildCreateRequest : 创建公会失败, 入会等级条件非法, CharacterID:{0}, 传入等级:{1}", character.Data.ID, request.ReqLevel);
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "入会等级条件设置非法";
                sender.SendResponse();
                return;
            }

            // TODO: 拓展预留 - 以后要把公会人数上限、创建基础消耗(100000)等抽离到静态配置表中 (如 DataManager.Instance.GuildConfig)
            // 拦截 4：检验玩家余额是否足够支付建会手续费
            long createCost = 100000;
            if (character.Gold < createCost)
            {
                Log.WarningFormat("OnGuildCreateRequest : 创建公会失败, 金币不足, CharacterID:{0}, 当前金币:{1}, 需要金币:{2}", character.Data.ID, character.Gold, createCost);
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "金币不足，创建公会需要 100,000 金币";
                sender.SendResponse();
                return;
            }

            // 核心流转：呼叫底层 Manager 执行建会落库事务
            Guild newGuild = GuildManager.Instance.CreateGuild(request.GuildName, request.Notice, request.ReqLevel, character);

            // 拦截 5：底层创建失败 (通常是因为公会重名或数据库断连)
            if (newGuild == null)
            {
                Log.ErrorFormat("OnGuildCreateRequest : 创建公会失败, 底层 Manager 执行失败或重名, CharacterID:{0}, 申请公会名:{1}", character.Data.ID, request.GuildName);
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "公会名称已存在或创建失败";
                sender.SendResponse();
                return;
            }

            // 成功处理：正式扣除玩家金币
            character.Gold -= createCost;

            Log.InfoFormat("OnGuildCreateRequest : 创建公会成功, CharacterID:{0}, 新公会ID:{1}, 公会名:{2}", character.Data.ID, newGuild.Data.Id, newGuild.Data.Name);
            sender.Session.Response.guildCreate.Result = Result.Success;
            sender.Session.Response.guildCreate.Errormsg = "创建公会成功";
            sender.Session.Response.guildCreate.Guild = newGuild.ToNGuildInfo(); // 附带新公会大盘数据供客户端渲染
            sender.SendResponse();
        }

        /// <summary>
        /// 处理：解散公会请求
        /// </summary>
        private void OnGuildDisbandRequest(NetConnection<NetSession> sender, GuildDisbandRequest request)
        {
            Character character = sender.Session.Character;
            int characterId = character.Data.ID;
            Log.InfoFormat("OnGuildDisbandRequest : 收到解散公会请求, 发起人 CharacterID:{0}", characterId);

            sender.Session.Response.guildDisband = new GuildDisbandResponse();

            // 提取公会内存对象
            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            // 拦截 1：玩家当前不在任何有效公会中
            if (guild == null)
            {
                Log.WarningFormat("OnGuildDisbandRequest : 解散公会失败, 玩家当前不在任何公会中, CharacterID:{0}", characterId);
                sender.Session.Response.guildDisband.Result = Result.Failed;
                sender.Session.Response.guildDisband.Errormsg = "你当前不在任何公会中";
                sender.SendResponse();
                return;
            }

            // 拦截 2：核心越权防御 - 只有最高统帅(会长)有权解散公会
            GuildMember guildMember = guild.GetGuildMember(characterId);
            if (guildMember.Data.Position != (int)GuildPosition.GuildPositionLeader)
            {
                Log.WarningFormat("OnGuildDisbandRequest : 解散公会失败, 权限不足, CharacterID:{0}, 当前职位:{1}", characterId, guildMember.Data.Position);
                sender.Session.Response.guildDisband.Result = Result.Failed;
                sender.Session.Response.guildDisband.Errormsg = "权限不足，只有会长可以解散公会";
                sender.SendResponse();
                return;
            }

            // 提取解散前所有的在线成员列表 (用于稍后广播)
            var onlineConnections = guild.GetOnlineSessions();

            // 核心流转：呼叫底层 Manager 执行级联删库清理
            bool success = GuildManager.Instance.DisbandGuild(guildId, characterId);

            // 拦截 3：底层数据库删表事务执行失败
            if (!success)
            {
                Log.ErrorFormat("OnGuildDisbandRequest : 解散公会失败, 底层 Manager 事务执行失败, GuildID:{0}, 发起人:{1}", guildId, characterId);
                sender.Session.Response.guildDisband.Result = Result.Failed;
                sender.Session.Response.guildDisband.Errormsg = "解散公会失败，请联系管理员";
                sender.SendResponse();
                return;
            }

            // 成功处理：优先给发起人下发解散成功的响应
            Log.InfoFormat("OnGuildDisbandRequest : 解散公会成功, GuildID:{0} 现已彻底销毁, 准备广播下线通知, 在线人数:{1}", guildId, onlineConnections.Count);
            sender.Session.Response.guildDisband.Result = Result.Success;
            sender.SendResponse();

            // 组播逻辑：给除了操作者之外的所有在线老公会成员，推送公会毁灭/被踢通知
            foreach (var connection in onlineConnections)
            {
                if (connection != null
                    && connection.Session.Character.Data.ID != characterId)
                {
                    Log.InfoFormat("OnGuildDisbandRequest : 广播公会解散被踢通知, 目标 CharacterID:{0}", connection.Session.Character.Data.ID);
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
            Character character = sender.Session.Character;
            int characterId = character.Data.ID;
            Log.InfoFormat("OnGuildSettingModifyRequest : 收到修改公会设置请求, 发起人 CharacterID:{0}, 新限制等级:{1}", characterId, request.NewReqLevel);

            sender.Session.Response.guildSettingModify = new GuildSettingModifyResponse();

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            // 拦截 1：玩家未归属公会或公会已销毁
            if (guild == null)
            {
                Log.WarningFormat("OnGuildSettingModifyRequest : 修改设置拦截, 玩家不在公会中, CharacterID:{0}", characterId);
                return; // 注意: 此处原始逻辑直接 Return (客户端可能挂起)，未来可考虑补全 SendResponse
            }

            // 拦截 2：核心越权防御 - 仅限会长或副会长操作。普通成员或无职位者拦截。
            GuildMember guildMember = guild.GetGuildMember(characterId);
            if (guildMember.Data.Position == (int)GuildPosition.GuildPositionMember
                 || guildMember.Data.Position == (int)GuildPosition.GuildPositionNone)
            {
                Log.WarningFormat("OnGuildSettingModifyRequest : 修改设置拦截, 权限不足, CharacterID:{0}, 职位:{1}", characterId, guildMember.Data.Position);
                return;
            }

            // 拦截 3：参数合法性校验 (-1说明改成无条件, 否则等级限制必须在 0-500 范围内)
            if (request.NewReqLevel != -1 && (request.NewReqLevel < 0 || request.NewReqLevel > 500))
            {
                Log.WarningFormat("OnGuildSettingModifyRequest : 修改设置拦截, 传入等级参数非法, ReqLevel:{0}", request.NewReqLevel);
                return;
            }

            // 核心流转：执行底层设置更新落库
            bool success = GuildManager.Instance.ModifyGuildSettings(guildId, request.NewNotice, request.NewReqLevel);

            // 拦截 4：落库事务失败
            if (!success)
            {
                Log.ErrorFormat("OnGuildSettingModifyRequest : 修改设置失败, 底层 Manager 事务执行异常, GuildID:{0}", guildId);
                sender.Session.Response.guildSettingModify.Result = Result.Failed;
                sender.Session.Response.guildSettingModify.Errormsg = "修改设置失败";
                sender.SendResponse();
                return;
            }

            // 成功处理：下发修改结果，并附带更新后的最新值供前端刷新 UI
            Log.InfoFormat("OnGuildSettingModifyRequest : 修改公会设置成功, GuildID:{0}, 准备广播信息变更通知", guildId);
            sender.Session.Response.guildSettingModify.Result = Result.Success;
            sender.Session.Response.guildSettingModify.UpdatedNotice = guild.Data.Notice;
            sender.Session.Response.guildSettingModify.UpdatedReqLevel = guild.Data.ReqLevel;
            sender.SendResponse();

            // 组播逻辑：向全公会其他在线成员同步最新公会信息 (用于其他玩家大厅面板或公会详情的实时刷新)
            var onlineConnections = guild.GetOnlineSessions();
            foreach (var connection in onlineConnections)
            {
                if (connection != null && connection.Session.Character.Data.ID != characterId)
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
            Log.InfoFormat("OnGuildJoinApplyRequest : 收到加入公会申请请求, CharacterID:{0}", sender.Session.Character.Data.ID);
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：公会审批请求 (管理员同意/拒绝玩家加入)
        /// </summary>
        private void OnGuildApplyProcessRequest(NetConnection<NetSession> sender, GuildApplyProcessRequest request)
        {
            Log.InfoFormat("OnGuildApplyProcessRequest : 收到公会审批请求, 审批人 CharacterID:{0}", sender.Session.Character.Data.ID);
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：离开公会请求 (玩家主动退出)
        /// </summary>
        private void OnGuildLeaveRequest(NetConnection<NetSession> sender, GuildLeaveRequest request)
        {
            Character character = sender.Session.Character;
            int characterId = character.Data.ID;
            Log.InfoFormat("OnGuildLeaveRequest : 收到离开公会请求, CharacterID:{0}", characterId);

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            // 拦截 1：玩家当前是自由身，无会可退
            if (guild == null)
            {
                Log.WarningFormat("OnGuildLeaveRequest : 离开公会失败, 玩家不在任何公会中, CharacterID:{0}", characterId);
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "你当前不在任何公会中";
                sender.SendResponse();
                return;
            }

            // 拦截 2：内存极光脏数据 - 找不到该玩家的成员实体
            GuildMember guildMember = guild.GetGuildMember(characterId);
            if (guildMember == null)
            {
                Log.WarningFormat("OnGuildLeaveRequest : 离开公会失败, 找不到成员内存数据, GuildID:{0}, CharacterID:{1}", guildId, characterId);
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "你不是该公会成员";
                sender.SendResponse();
                return;
            }

            // 拦截 3：业务规则 - 最高统帅(会长)不可以直接开溜，必须先转让位置或走解散流程
            if (guildMember.Data.Position == (int)GuildPosition.GuildPositionLeader)
            {
                Log.WarningFormat("OnGuildLeaveRequest : 离开公会拦截, 会长禁止直接退会, GuildID:{0}, CharacterID:{1}", guildId, characterId);
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "会长不能直接退出公会，请先转让会长或解散公会";
                sender.SendResponse();
                return;
            }

            // 核心流转：剥离成员身份，删库清缓存
            bool success = GuildManager.Instance.LeaveGuild(guildId, characterId);

            // 拦截 4：退会事务执行失败
            if (!success)
            {
                Log.ErrorFormat("OnGuildLeaveRequest : 离开公会失败, 底层 Manager 事务执行异常, GuildID:{0}, CharacterID:{1}", guildId, characterId);
                sender.Session.Response.guildLeave.Result = Result.Failed;
                sender.Session.Response.guildLeave.Errormsg = "退出公会失败";
                sender.SendResponse();
                return;
            }

            Log.InfoFormat("OnGuildLeaveRequest : 离开公会成功, GuildID:{0}, CharacterID:{1}, 准备广播离会通知", guildId, characterId);

            // 组播逻辑：玩家走后，通知仍在公会里的其他在线老伙计 (更新他们的成员列表显示)
            var onlineConnections = guild.GetOnlineSessions();
            foreach (var connection in onlineConnections)
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
            Log.InfoFormat("OnGuildChatRequest : 收到公会频道聊天请求, CharacterID:{0}", sender.Session.Character.Data.ID);
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理：获取公会成员列表请求
        /// </summary>
        private void OnGuildMemberListRequest(NetConnection<NetSession> sender, GuildMemberListRequest request)
        {
            Character character = sender.Session.Character;
            int characterId = character.Data.ID;
            Log.InfoFormat("OnGuildMemberListRequest : 收到获取成员列表请求, 发起人 CharacterID:{0}", characterId);

            sender.Session.Response.guildMemberList = new GuildMemberListResponse();

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            // 成功处理：提取全员名单下发
            if (guild != null)
            {
                var members = guild.GetNGuildMembers();
                sender.Session.Response.guildMemberList.Members.AddRange(members);
                sender.Session.Response.guildMemberList.Result = Result.Success;
                Log.InfoFormat("OnGuildMemberListRequest : 下发成员列表成功, GuildID:{0}, 下发数量:{1}", guildId, members.Count);
            }
            // 拦截/异常：可能在此期间公会刚刚被会长解散，退回失败状态
            else
            {
                Log.WarningFormat("OnGuildMemberListRequest : 下发成员列表失败, 公会不存在或已解散, CharacterID:{0}", characterId);
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
            Character character = sender.Session.Character;
            int characterId = character.Data.ID;
            Log.InfoFormat("OnGuildApplyListRequest : 收到获取申请列表请求, 发起人 CharacterID:{0}", characterId);

            sender.Session.Response.guildApplyList = new GuildApplyListResponse();

            int guildId = GuildManager.Instance.GetGuildIdByCharacter(characterId);
            Guild guild = GuildManager.Instance.GetGuild(guildId);

            if (guild != null)
            {
                // 拦截防御：极度严格的权限校验！
                // 1. 玩家必须在成员字典中
                // 2. 且其职位必须是管理层 (职位枚举越小权限越大，假设 1=会长, 2=副会长)
                if (guild.Members.TryGetValue(characterId, out GuildMember myMember) &&
                    myMember.Data.Position <= (int)GuildPosition.GuildPositionViceLeader)
                {
                    // 成功处理：下发所有申请人的记录
                    var applies = guild.GetNGuildApplies();
                    sender.Session.Response.guildApplyList.Applies.AddRange(applies);
                    sender.Session.Response.guildApplyList.Result = Result.Success;
                    Log.InfoFormat("OnGuildApplyListRequest : 下发申请列表成功, GuildID:{0}, 下发数量:{1}", guildId, applies.Count);
                }
                else
                {
                    // 越权拦截：普通成员尝试偷看申请名单
                    Log.WarningFormat("OnGuildApplyListRequest : 获取申请列表被拦截, 权限不足, CharacterID:{0}", characterId);
                    sender.Session.Response.guildApplyList.Result = Result.Failed;
                    sender.Session.Response.guildApplyList.Errormsg = "权限不足：只有会长或副会长可查看申请列表";
                }
            }
            else
            {
                // 拦截异常：公会不存在
                Log.WarningFormat("OnGuildApplyListRequest : 下发申请列表失败, 公会不存在, CharacterID:{0}", characterId);
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
            Log.InfoFormat("OnGuildListRequest : 收到获取大厅公会列表请求, 发起人 CharacterID:{0}", sender.Session.Character.Data.ID);

            sender.Session.Response.guildList = new GuildListResponse();

            // 核心逻辑：提取内存中全服的公会摘要信息
            List<NGuildInfo> nGuilds = GuildManager.Instance.GetGuildsInfo();

            // 成功处理：打包塞给客户端
            sender.Session.Response.guildList.Guilds.AddRange(nGuilds);
            sender.Session.Response.guildList.Result = Result.Success;

            Log.InfoFormat("OnGuildListRequest : 下发大厅公会列表成功, 下发公会总数:{0}", nGuilds.Count);
            sender.SendResponse();
        }

        /// <summary>
        /// 处理：公会管理操作请求 (踢出成员、升降职等)
        /// </summary>
        private void OnGuildAdminRequest(NetConnection<NetSession> sender, GuildAdminRequest request)
        {
            Log.InfoFormat("OnGuildAdminRequest : 收到公会管理操作请求, 发起人 CharacterID:{0}", sender.Session.Character.Data.ID);
            throw new NotImplementedException();
        }
    }
}