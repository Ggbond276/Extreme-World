using Assets.Scripts.Managers;
using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Services
{
    class TeamService : Singleton<TeamService>, IDisposable
    {

        /// <summary>
        /// 初始化组队服务，并向网络分发器注册（订阅）所有组队相关的服务端响应事件
        /// </summary>
        public TeamService()
        {
            Debug.LogFormat("TeamService.Init: 初始化并订阅组队网络事件");
            MessageDistributer.Instance.Subscribe<TeamInviteRequest>(this.OnTeamInviteRequest);
            MessageDistributer.Instance.Subscribe<TeamInviteResponse>(this.OnTeamInviteResponse);
            MessageDistributer.Instance.Subscribe<TeamLeaveResponse>(this.OnTeamLeaveResponse);
            MessageDistributer.Instance.Subscribe<TeamInfoResponse>(this.OnTeamInfoResponse);
        }

        /// <summary>
        /// 释放资源，在玩家登出或程序关闭时取消所有已注册的网络事件监听，防止内存泄漏或幽灵订阅
        /// </summary>
        public void Dispose()
        {
            Debug.LogFormat("TeamService.Dispose: 取消订阅组队网络事件");
            MessageDistributer.Instance.Unsubscribe<TeamInviteRequest>(this.OnTeamInviteRequest);
            MessageDistributer.Instance.Unsubscribe<TeamInviteResponse>(this.OnTeamInviteResponse);
            MessageDistributer.Instance.Unsubscribe<TeamLeaveResponse>(this.OnTeamLeaveResponse);
            MessageDistributer.Instance.Unsubscribe<TeamInfoResponse>(this.OnTeamInfoResponse);
        }
        /// <summary>
        /// 服务的业务层初始化（预留接口，通常在进入游戏主场景时调用）
        /// </summary>
        public void Init() { }



        // ======================== 【邀请组队链路】 ========================

        /// <summary>
        /// [客户端 -> 服务端] 
        /// 发送“发起组队邀请”请求（我们主动邀请其他玩家加入我们的队伍）
        /// </summary>
        public void SendTeamInviteRequest(int toId, string toName)
        {
            Debug.LogFormat("TeamService.SendTeamInviteRequest: 正在发送组队邀请, 目标ID:{0} 目标Name:{1}", toId, toName);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.teamInviteReq = new TeamInviteRequest();
            message.Request.teamInviteReq.FromId = User.Instance.CurrentCharacter.Id;
            message.Request.teamInviteReq.FromName = User.Instance.CurrentCharacter.Name;
            message.Request.teamInviteReq.ToId = toId;
            message.Request.teamInviteReq.ToName = toName;
            if(TeamManager.Instance.CurrentTeam != null)
            {
                message.Request.teamInviteReq.TeamId = TeamManager.Instance.CurrentTeam.Id;
            }
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// [服务端 -> 客户端] 
        /// 处理服务端下发的“收到组队邀请”通知（别人邀请我们加入他的队伍，触发UI弹窗提示）
        /// </summary>
        private void OnTeamInviteRequest(object sender, TeamInviteRequest request)
        {
            Debug.LogFormat("TeamService.OnTeamInviteRequest: 收到组队邀请通知, 来自ID:{0} Name:{1}", request.FromId, request.FromName);
            TeamManager.Instance.ReceiveTeamInvite(request);
        }


        /// <summary>
        /// [客户端 -> 服务端] 
        /// 发送“组队邀请处理结果”（我们在弹窗中点击了“同意”或“拒绝”别人的邀请后，将结果发给服务器）
        /// </summary>
        public void SendTeamInviteResponse(Result result, TeamInviteRequest originRequest)
        {
            Debug.LogFormat("TeamService.SendTeamInviteResponse: 正在发送邀请响应, 回复结果:{0}, 原邀请人ID:{1}", result, originRequest.FromId);
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.teamInviteRes = new TeamInviteResponse();
            message.Response.teamInviteRes.Result = result;
            message.Response.teamInviteRes.Request = originRequest;
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// [服务端 -> 客户端] 
        /// 处理服务端下发的“组队邀请最终结果”响应（我们作为邀请方，收到对方最终是同意还是拒绝的反馈）
        /// </summary>
        private void OnTeamInviteResponse(object sender, TeamInviteResponse response)
        {
            Debug.LogFormat("TeamService.OnTeamInviteResponse: 收到组队邀请的最终结果, Result:{0} Msg:{1}", response.Result, response.Errormsg);
            Result result = response.Result;
            string errorMsg = response.Errormsg;
            TeamManager.Instance.HandleInviteResult(result, errorMsg);
        }




        // ======================== 【离开/解散队伍链路】 ========================

        /// <summary>
        /// [客户端 -> 服务端] 
        /// 发送“离开队伍”请求（包括普通队员主动退队，或队长主动解散队伍）
        /// </summary>
        public void SendTeamLeaveRequest()
        {
            if (TeamManager.Instance.CurrentTeam == null)
            {
                Debug.LogWarning("TeamService.SendTeamLeaveRequest: 尝试发送离队请求，但当前无队伍数据！");
                return;
            }

            Debug.LogFormat("TeamService.SendTeamLeaveRequest: 正在发送离队请求, 当前队伍ID:{0}", TeamManager.Instance.CurrentTeam.Id);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest(); // 补上这一行！
            message.Request.teamLeave = new TeamLeaveRequest();
            message.Request.teamLeave.characterId = User.Instance.CurrentCharacter.Id;
            message.Request.teamLeave.TeamId = TeamManager.Instance.CurrentTeam.Id;
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// [服务端 -> 客户端] 
        /// 处理服务端下发的“离队结果”响应（确认服务端已经成功将自己移出队伍，然后清空本地数据）
        /// </summary>
        private void OnTeamLeaveResponse(object sender, TeamLeaveResponse response)
        {
            Debug.LogFormat("TeamService.OnTeamLeaveResponse: 收到离队结果, Result:{0} Msg:{1}", response.Result, response.Errormsg);
            Result result = response.Result;
            string errorMsg = response.Errormsg;
            TeamManager.Instance.HandleLeaveResult(result, errorMsg);
        }




        // ======================== 【状态同步链路】 ========================

        /// <summary>
        /// [服务端 -> 客户端] 
        /// 处理服务端下发的“队伍最新同步信息”响应
        /// （当队伍发生进人、退人、换队长等变化时，服务端会下发此包，客户端利用此包全量覆写并刷新本地队伍数据）
        /// </summary>
        /// <param name="sender">网络事件发送者</param>
        /// <param name="response">包含最新队伍数据(NTeamInfo)的网络响应包</param>
        private void OnTeamInfoResponse(object sender, TeamInfoResponse response)
        {
            if (response.Result == Result.Success)
            {
                Debug.LogFormat("TeamService.OnTeamInfoResponse: 队伍同步成功, 准备交由Manager全量覆写");
                TeamManager.Instance.UpdateTeamInfo(response.Team);
            } else
            {
                string errorMsg = response.Errormsg;
                Debug.LogWarningFormat("TeamService.OnTeamInfoResponse 失败: {0}", errorMsg);
            }
        }

    }
}
