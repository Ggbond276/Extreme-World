using Assets.Scripts.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Managers
{
    class TeamManager : Singleton<TeamManager>
    {
        //int32 id = 1;
        //int32 leader = 2;
        //repeated NCharacterInfo members = 3;
        /// <summary> 
        /// 当前所在队伍的全量网络数据（包含Id, Leader, Members等） 
        /// </summary>
        public NTeamInfo CurrentTeam;
        /// <summary> 
        /// 广播事件：队伍数据发生变动时触发（UI监听此事件刷新面板） 
        /// </summary>
        public UnityAction OnTeamChanged;
        /// <summary> 
        /// 广播事件：收到他人的组队邀请时触发（UI监听此事件弹出确认框） 
        /// </summary>
        public UnityAction<TeamInviteRequest> OnReceiveTeamInvite;
        /// <summary> 
        /// 广播事件：需要向屏幕中央抛出提示飘字时触发 
        /// </summary>
        public UnityAction<string> OnShowFloatMessage;



        /// <summary>
        /// 接收服务端同步：全量覆写本地队伍数据并广播 UI 刷新
        /// </summary>
        public void UpdateTeamInfo(NTeamInfo newTeamInfo)
        {

            Debug.LogFormat("TeamManager.UpdateTeamInfo: 更新本地队伍数据, 最新队伍ID:{0}", newTeamInfo != null ? newTeamInfo.Id.ToString() : "Null(已解散/离队)");
            this.CurrentTeam = newTeamInfo;

            if (this.OnTeamChanged != null)
                this.OnTeamChanged.Invoke();
        }

        /// <summary>
        /// 接收服务端请求：处理别人发给我的组队邀请，呼叫 UI 弹窗
        /// </summary>
        public void ReceiveTeamInvite(TeamInviteRequest request)
        {

            Debug.LogFormat("TeamManager.ReceiveTeamInvite: 收到邀请, 准备通知UI弹窗, 发起人:{0}", request.FromName);
            if (this.OnReceiveTeamInvite != null)
                this.OnReceiveTeamInvite(request);
        }

        /// <summary>
        /// 接收服务端响应：处理我发出的组队邀请的最终反馈结果
        /// </summary>
        public void HandleInviteResult(Result result, string msg)
        {
            Debug.LogFormat("TeamManager.HandleInviteResult: 处理邀请最终结果 Result:{0} Msg:{1}", result, msg);
            string defaultMsg = "组队成功";

            if (result == Result.Failed)
            {
                defaultMsg = msg;
            }

            if (this.OnShowFloatMessage != null)
                this.OnShowFloatMessage(defaultMsg);

        }

        /// <summary>
        /// 接收服务端响应：处理我离开队伍的最终反馈结果
        /// </summary>
        public void HandleLeaveResult(Result result, string msg)
        {

            Debug.LogFormat("TeamManager.HandleLeaveResult: 处理离队最终结果 Result:{0} Msg:{1}", result, msg);
            string defaultMsg = "退出队伍成功";
            if(result == Result.Failed)
            {
                defaultMsg = msg;
            }

            if (this.OnShowFloatMessage != null)
                this.OnShowFloatMessage(defaultMsg);
        }




        /// <summary>
        /// 玩家点击按键：向指定目标好友发起组队邀请
        /// </summary>
        /// <param name="targetId">目标好友的本地 ID</param>
        public void SendInvite(int targetId)
        {
            // 上层传参需要做合法性验证
            Debug.LogFormat("TeamManager.SendInvite: 准备发起组队邀请, 校验目标ID:{0}", targetId);
            NFriendInfo info = FriendManager.Instance.GetFriend(targetId);
            if (info == null)
            {
                Debug.LogWarningFormat("TeamManager.SendInvite 拦截: 目标ID {0} 不在本地好友列表中！", targetId);
                return;
            }
               

            string targetName = info.friendInfo.Name;
            Debug.LogFormat("TeamManager.SendInvite: 校验通过, 目标名字:{0}, 移交Service发包", targetName);
            TeamService.Instance.SendTeamInviteRequest(targetId, targetName);
        }

        /// <summary>
        /// 玩家点击按键：在 UI 弹窗上点击“同意”或“拒绝”别人的组队邀请
        /// </summary>
        public void ResponseInvite(bool isAgree, TeamInviteRequest originRequest)
        {
            Debug.LogFormat("TeamManager.ResponseInvite: 玩家做出选择, 是否同意:{0}, 回复给ID:{1}", isAgree, originRequest.FromId);
            Result result = Result.Success;
            if (!isAgree)
                result = Result.Failed;
            TeamService.Instance.SendTeamInviteResponse(result, originRequest);
            
        }

        /// <summary>
        /// 玩家点击按键：申请离开/解散当前队伍
        /// </summary>
        public void SendLeave()
        {
            Debug.LogFormat("TeamManager.SendLeave: 玩家点击离开队伍按钮, 移交Service发包");
            TeamService.Instance.SendTeamLeaveRequest();
        }
    }
}
