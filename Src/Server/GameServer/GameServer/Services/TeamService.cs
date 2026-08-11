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

            Log.InfoFormat("");
            // 1.让Manager将请求对象清理出队伍
            Character requester = sender.Session.Character;
            if (requester == null || requester.team == null) return;

            int teamId = requester.team.Id;
            int requesterId = requester.Id;

            sender.Session.Response.teamLeave = new TeamLeaveResponse();

            if (TeamManager.Instance.LeaveTeam(teamId, requesterId, out string errorMsg))
            {
                sender.Session.Response.teamLeave.Result = Result.Success;
            } else
            {
                sender.Session.Response.teamLeave.Result = Result.Failed;
                sender.Session.Response.teamLeave.Errormsg = errorMsg;
            }
            
            // 2.通知请求者
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
