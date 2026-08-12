using Common;
using Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkillBridge.Message;
using GameServer.Managers;
using GameServer.Entities;
using GameServer.Manager;

namespace GameServer.Services
{
    class TeamService : Singleton<TeamService>
    {

        /// <summary>
        /// 构造方法 订阅网络协议
        /// </summary>
        public TeamService()
        {
            Log.InfoFormat("TeamService: 服务端组队服务初始化并订阅网络协议");
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<TeamInviteRequest>(this.OnTeamInviteRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<TeamInviteResponse>(this.OnTeamInviteResponse);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<TeamInfoRequest>(this.OnTeamInfoRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<TeamLeaveRequest>(this.OnTeamLeaveRequest);
        }

        /// <summary>
        /// 初始化方法
        /// </summary>
        public void Init() { }

        /// <summary>
        /// 处理A发送来的组队邀请请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnTeamInviteRequest(NetConnection<NetSession> sender, TeamInviteRequest request)
        {
            
            int requesterId = request.FromId;
            int targetId = request.ToId;
            NetConnection<NetSession> requesterConnection = sender;
            NetConnection<NetSession> targetConnection = SessionManager.Instance.GetSession(targetId);
            Log.InfoFormat("OnTeamInviteRequest: 收到组队请求， 请求者：ID: {0} Name: {1}  目标：ID: {2} Name: {3}", requesterId, request.FromName, targetId, request.ToName);

            // 1.如果B不在线直接拦截 
            if (targetConnection == null)
            {
                Log.InfoFormat("OnTeamIviteRequest: 无法发送组队邀请，目标: ID: {0} Name: {1} 不在线}", targetId, request.ToName);
                requesterConnection.Session.Response.teamInviteRes = new TeamInviteResponse();
                requesterConnection.Session.Response.teamInviteRes.Result = Result.Failed;
                requesterConnection.Session.Response.teamInviteRes.Errormsg = "对方不在线无法发送组队邀请";
                requesterConnection.SendResponse();
                return;
            }
            // 2.判断B如果有队伍直接拦截
            Character target = CharacterManager.Instance.GetCharacter(targetId);
            if(target.team != null)
            {
                Log.InfoFormat("OnTeamInviteRequest: 无法发送组队邀请，目标: ID: {0} Name: {1} 已经组队", targetId, request.ToName);
                requesterConnection.Session.Response.teamInviteRes = new TeamInviteResponse();
                requesterConnection.Session.Response.teamInviteRes.Result = Result.Failed;
                requesterConnection.Session.Response.teamInviteRes.Errormsg = "对方已经组队，无法接受邀请";
                requesterConnection.SendResponse();
                return;
            }

            // 3.如果B在线，且没有队伍，就可以向B发送邀请信息
            Log.InfoFormat("OnTeamInviteRequest: 正在转发组队邀请，目标: ID: {0} Name: {1}", targetId, request.ToName);
            targetConnection.Session.Response.teamInviteReq = request;
            targetConnection.SendResponse();
        }

        /// <summary>
        /// 处理B响应给A是否同意
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnTeamInviteResponse(NetConnection<NetSession> sender, TeamInviteResponse response)
        {
            int requesterId = response.Request.FromId;
            int targetId = response.Request.ToId;
            NetConnection<NetSession> requesterConnection = SessionManager.Instance.GetSession(requesterId);
            NetConnection<NetSession> targetConection = sender;
            Character requester = requesterConnection.Session.Character;
            Character target = targetConection.Session.Character;
            Log.InfoFormat("OnTeamInviteResponse: 收到回应, 玩家(B) ID:{0} 回应了 玩家(A) ID:{1} 的组队请求", targetId, requesterId);


            // 如果请求组队方目前掉线了
            if(requesterConnection == null)
            {
                Log.InfoFormat("");
                targetConection.Session.Response.teamInviteRes = new TeamInviteResponse();
                targetConection.Session.Response.teamInviteRes.Result = Result.Failed;
                targetConection.Session.Response.teamInviteRes.Errormsg = "对方以下线，组队失败";
                targetConection.SendResponse();
                return;
            }

            // 1.如果对面发来的消息是不同意的话
            if (response.Result == Result.Failed)
            {
                Log.InfoFormat("OnTeamInviteResponse: 拒绝组队, 玩家(B) ID:{0} 拒绝了 玩家(A) ID:{1} 的请求", targetId, requesterId);
                requesterConnection.Session.Response.teamInviteRes = new TeamInviteResponse();
                requesterConnection.Session.Response.teamInviteRes.Result = Result.Failed;
                requesterConnection.Session.Response.teamInviteRes.Errormsg = response.Errormsg;
                requesterConnection.SendResponse();
                return;
            }


            // 2.如果对面发来的消息是同意的话

            // 3.如果A目前没有队伍
           if(requester.team == null)
            {
                Log.InfoFormat("OnTeamInviteResponse: 同意组队, 玩家(A) ID:{0} 目前无队伍, 正在为其自动建队", requesterId);
                TeamManager.Instance.CreateTeam(requester);
            }

            // 4.如果A目前有队伍，将B加入到队伍中
            Log.InfoFormat("OnTeamInviteResponse: 同意组队, 准备将 玩家(B) ID:{0} 加入 玩家(A) ID:{1} 的队伍", targetId, requesterId);
            requester.team.AddMember(target);
            target.team = requester.team;

            // 5.通知A组队成功
            Log.InfoFormat("OnTeamInviteResponse: 组队成功, 准备通知 邀请人(A) ID:{0}", requesterId);
            requesterConnection.Session.Response.teamInviteRes = new TeamInviteResponse();
            requesterConnection.Session.Response.teamInviteRes.Result = Result.Success;
            requesterConnection.SendResponse();
            // 6.通知B组队成功
            Log.InfoFormat("OnTeamInviteResponse: 组队成功, 准备通知 响应人(B) ID:{0}", targetId);
            targetConection.Session.Response.teamInviteRes = new TeamInviteResponse();
            targetConection.Session.Response.teamInviteRes.Result = Result.Success;
            targetConection.SendResponse();
        }


        /// <summary>
        /// 处理离开队伍请求（玩家掉线的时候也需要调用这个请求）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnTeamLeaveRequest(NetConnection<NetSession> sender, TeamLeaveRequest request)
        {

            // 安全获取会话角色信息（防止空指针崩溃）
            Character requester = sender.Session != null ? sender.Session.Character : null;

            int reqId = requester != null ? requester.Id : -1;
            string reqName = requester != null ? requester.Data.Name : "未知角色";

            // 1. 打印详尽的接收日志，方便直接对比客户端发过来的参数是否正确
            Log.InfoFormat("OnTeamLeaveRequest: 收到离队请求 -> 协议包携带 TeamId:{0}, CharacterId:{1} | 当前会话角色 ID:{2}, Name:{3}",
                request.TeamId, request.characterId, reqId, reqName);

            // 2. 基础拦截判定
            if (requester == null)
            {
                Log.WarningFormat("OnTeamLeaveRequest 拦截: 该 Connection 的 Session 中找不到对应的 Character 对象！");
                return;
            }

            if (requester.team == null)
            {
                Log.WarningFormat("OnTeamLeaveRequest 拦截: 玩家 [ID:{0}, Name:{1}] 尝试离队，但他当前根本不属于任何队伍！", reqId, reqName);
                return;
            }

            int teamId = requester.team.Id;
            int requesterId = requester.Id;



            // =========================================================================
            // 🌟 【新增位置 1：解散前先备份名单】 🌟
            // 必须在调用 LeaveTeam 之前把名单存下来！不然队长一退，队伍解散，后面的 B、C 就找不到了！
            // =========================================================================
            List<Character> affectedMembers = new List<Character>();
            affectedMembers.AddRange(requester.team.Members);



            Log.InfoFormat("OnTeamLeaveRequest: 校验通过 -> 准备执行离队操作, 目标队伍ID:{0}, 离队队员ID:{1}", teamId, requesterId);

            sender.Session.Response.teamLeave = new TeamLeaveResponse();

            // 3. 交给 Manager 处理具体的队伍剔除逻辑
            if (TeamManager.Instance.LeaveTeam(teamId, requesterId, out string errorMsg))
            {
                sender.Session.Response.teamLeave.Result = Result.Success;
                Log.InfoFormat("OnTeamLeaveRequest: 离队成功, 已从队伍 {0} 中移除队员 {1}", teamId, requesterId);


                // =========================================================================
                // 🌟 【新增位置 2：主动通知其他受影响的队友】 🌟
                // =========================================================================
                foreach (var member in affectedMembers)
                {
                    // 自己 (A) 的响应会在方法最后面单独发送，这里直接跳过自己
                    if (member.Id == requesterId) continue;

                    // 找到队友 (比如 B) 的网络连接线
                    NetConnection<NetSession> memberConnection = SessionManager.Instance.GetSession(member.Id);
                    if (memberConnection != null && memberConnection.Session != null)
                    {
                        // 完美利用你写好的后处理管线！强行把 B 的脏数据组装成 TeamInfoResponse
                        member.PostResponse(memberConnection.Session.Response);

                        // 顺着网线直接糊到 B 的脸上！B 的 UI 瞬间同步！
                        memberConnection.SendResponse();
                    }
                }



            }
            else
            {
                sender.Session.Response.teamLeave.Result = Result.Failed;
                sender.Session.Response.teamLeave.Errormsg = errorMsg;
                Log.WarningFormat("OnTeamLeaveRequest: 离队失败, 原因: {0}", errorMsg);
            }

            // 4. 将响应包发送回客户端
            sender.SendResponse();
        }

        /// <summary>
        /// 处理队伍信息请求（全量更新一般不会主动发送队伍请求）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnTeamInfoRequest(NetConnection<NetSession> sender, TeamInfoRequest message)
        {
            throw new NotImplementedException();
        }

    }
}
