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
    class GuildService : Singleton<GuildService>, IDisposable
    {
        public GuildService()
        {
            MessageDistributer.Instance.Subscribe<GuildMemberListResponse>(this.OnGuildMemberList);
            MessageDistributer.Instance.Subscribe<GuildApplyListResponse>(this.OnGuildApplyList);
            MessageDistributer.Instance.Subscribe<GuildListResponse>(this.OnGuildList);

            // 2. 订阅主动操作 Response (ACK 状态确认)
            MessageDistributer.Instance.Subscribe<GuildCreateResponse>(this.OnGuildCreate);
            MessageDistributer.Instance.Subscribe<GuildDisbandResponse>(this.OnGuildDisband);
            MessageDistributer.Instance.Subscribe<GuildSettingModifyResponse>(this.OnGuildSettingModify);
            MessageDistributer.Instance.Subscribe<GuildJoinApplyResponse>(this.OnGuildJoinApply);
            MessageDistributer.Instance.Subscribe<GuildApplyProcessResponse>(this.OnGuildApplyProcess);
            MessageDistributer.Instance.Subscribe<GuildLeaveResponse>(this.OnGuildLeave);
            MessageDistributer.Instance.Subscribe<GuildAdminResponse>(this.OnGuildAdmin);

            // 3. 订阅全局数据广播 Notify (Push Patching)
            MessageDistributer.Instance.Subscribe<GuildMemberAddNotify>(this.OnGuildMemberAddNotify);
            MessageDistributer.Instance.Subscribe<GuildMemberRemoveNotify>(this.OnGuildMemberRemoveNotify);
            MessageDistributer.Instance.Subscribe<GuildApplyAddNotify>(this.OnGuildApplyAddNotify);
            MessageDistributer.Instance.Subscribe<GuildApplyRemoveNotify>(this.OnGuildApplyRemoveNotify);
            MessageDistributer.Instance.Subscribe<GuildApplyResultNotify>(this.OnGuildApplyResultNotify);
            MessageDistributer.Instance.Subscribe<GuildInfoChangeNotify>(this.OnGuildInfoChangeNotify);
        }

        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<GuildMemberListResponse>(this.OnGuildMemberList);
            MessageDistributer.Instance.Unsubscribe < GuildApplyListResponse>(this.OnGuildApplyList);
            MessageDistributer.Instance.Unsubscribe<GuildListResponse>(this.OnGuildList);


            // 2. 注销主动操作 Response
            MessageDistributer.Instance.Unsubscribe<GuildCreateResponse>(this.OnGuildCreate);
            MessageDistributer.Instance.Unsubscribe<GuildDisbandResponse>(this.OnGuildDisband);
            MessageDistributer.Instance.Unsubscribe<GuildSettingModifyResponse>(this.OnGuildSettingModify);
            MessageDistributer.Instance.Unsubscribe<GuildJoinApplyResponse>(this.OnGuildJoinApply);
            MessageDistributer.Instance.Unsubscribe<GuildApplyProcessResponse>(this.OnGuildApplyProcess);
            MessageDistributer.Instance.Unsubscribe<GuildLeaveResponse>(this.OnGuildLeave);
            MessageDistributer.Instance.Unsubscribe<GuildAdminResponse>(this.OnGuildAdmin);

            // 3. 注销全局数据广播 Notify
            MessageDistributer.Instance.Unsubscribe<GuildMemberAddNotify>(this.OnGuildMemberAddNotify);
            MessageDistributer.Instance.Unsubscribe<GuildMemberRemoveNotify>(this.OnGuildMemberRemoveNotify);
            MessageDistributer.Instance.Unsubscribe<GuildApplyAddNotify>(this.OnGuildApplyAddNotify);
            MessageDistributer.Instance.Unsubscribe<GuildApplyRemoveNotify>(this.OnGuildApplyRemoveNotify);
            MessageDistributer.Instance.Unsubscribe<GuildApplyResultNotify>(this.OnGuildApplyResultNotify);
            MessageDistributer.Instance.Unsubscribe<GuildInfoChangeNotify>(this.OnGuildInfoChangeNotify);
        }

        /// <summary>
        /// 发送公会成员列表请求
        /// </summary>
        public void SendGuildMemberListRequest()
        {
            Debug.Log("SendGuildMemberListRequest : 发送拉取公会成员列表请求");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildMemberList = new GuildMemberListRequest();
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 发送公会申请列表请求
        /// </summary>
        public void SendGuildApplyListRequest()
        {
            Debug.Log("");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildApplyList = new GuildApplyListRequest();
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 发送公会列表请求
        /// </summary>
        public void SendGuildListRequest()
        {
            Debug.Log("");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildList = new GuildListRequest();
            NetClient.Instance.SendMessage(message);
        }


        /// <summary>
        /// 发送请求：创建公会
        /// </summary>
        /// <param name="name">拟建公会名称</param>
        /// <param name="notice">公会宗旨</param>
        /// <param name="reqLevel">入会最低等级要求</param>
        public void SendGuildCreate(string name, string notice, int reqLevel)
        {
            Debug.LogFormat("SendGuildCreate : 发送创建公会请求, Name:{0}", name);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildCreate = new GuildCreateRequest();
            message.Request.guildCreate.GuildName = name;
            message.Request.guildCreate.Notice = notice;
            message.Request.guildCreate.ReqLevel = reqLevel;
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// 发送请求：解散公会 (仅会长有效)
        /// </summary>
        public void SendGuildDisband()
        {
            Debug.Log("SendGuildDisband : 发送解散公会请求");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildDisband = new GuildDisbandRequest();
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// 发送请求：申请加入目标公会
        /// </summary>
        /// <param name="guildId">目标公会ID</param>
        public void SendGuildLeave()
        {
            Debug.Log("SendGuildLeave : 发送离开公会请求");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildLeave = new GuildLeaveRequest();
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// 发送请求：申请加入目标公会
        /// </summary>
        /// <param name="guildId">目标公会ID</param>
        public void SendGuildJoinApply(int guildId)
        {
            Debug.LogFormat("SendGuildJoinApply : 发送入会申请, 目标公会:{0}", guildId);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildJoinApply = new GuildJoinApplyRequest();
            message.Request.guildJoinApply.TargetGuildId = guildId;
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// 发送请求：管理层处理入会申请 (同意/拒绝)
        /// </summary>
        /// <param name="applicantId">申请人角色ID</param>
        /// <param name="cmd">审批指令 (Accept/Reject)</param>
        public void SendGuildApplyProcess(int applicantId, GuildApplyProcessCommand cmd)
        {
            Debug.LogFormat("SendGuildApplyProcess : 发送审批请求, 目标玩家:{0}, 指令:{1}", applicantId, cmd);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildApplyProcess = new GuildApplyProcessRequest();
            message.Request.guildApplyProcess.ApplicantCharacterId = applicantId;
            message.Request.guildApplyProcess.Command = cmd;
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// 发送请求：公会管理层操作 (踢人/升降职/转让)
        /// </summary>
        /// <param name="targetId">操作目标角色ID</param>
        /// <param name="cmd">具体管理指令</param>
        public void SendGuildAdmin(int targetId, GuildAdminCommand cmd)
        {
            Debug.LogFormat("SendGuildAdmin : 发送管理操作, 目标玩家:{0}, 指令:{1}", targetId, cmd);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildAdmin = new GuildAdminRequest();
            message.Request.guildAdmin.TargetCharacterId = targetId;
            message.Request.guildAdmin.Command = cmd;
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// 发送请求：修改公会设置 (仅会长有效)
        /// </summary>
        /// <param name="notice">新公会宗旨</param>
        /// <param name="reqLevel">新入会等级要求</param>
        public void SendGuildSettingModify(string notice, int reqLevel)
        {
            Debug.Log("SendGuildSettingModify : 发送修改公会设置请求");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildSettingModify = new GuildSettingModifyRequest();
            message.Request.guildSettingModify.NewNotice = notice;
            message.Request.guildSettingModify.NewReqLevel = reqLevel;
            NetClient.Instance.SendMessage(message);
        }





        /// <summary>
        /// 接收公会成员响应数据，将数据传递给Manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnGuildMemberList(object sender, GuildMemberListResponse response)
        {
            if (response.Result == Result.Success)
            {
                Debug.LogFormat("OnGuildMemberList : 拉取成员列表成功, 共 {0} 人", response.Members.Count);
                // 优雅调用，将数据装载和事件触发的职责还给 Manager
                GuildManager.Instance.RefreshMembers(response.Members);
            }
        }

        /// <summary>
        /// 接收公会申请响应数据，将数据传递给Manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnGuildApplyList(object sender, GuildApplyListResponse response)
        {
            if (response.Result == Result.Success)
            {
                Debug.LogFormat("OnGuildApplyList : 拉取申请列表成功, 共 {0} 条", response.Applies.Count);
                GuildManager.Instance.RefreshApplies(response.Applies);
            }
        }

        /// <summary>
        /// 接收公会列表申请响应数据，将数据传递给Manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnGuildList(object sender, GuildListResponse response)
        {
            if (response.Result == Result.Success)
            {
                Debug.LogFormat("OnGuildList : 拉取公会大厅成功, 共 {0} 个", response.Guilds.Count);
                GuildManager.Instance.RefreshGuildHall(response.Guilds);
            }
        }

        /// <summary>
        /// 接收响应：创建公会结果反馈
        /// </summary>
        private void OnGuildCreate(object sender, GuildCreateResponse response)
        {
            Debug.LogFormat("OnGuildCreate : 创建公会响应, Result:{0}, Msg:{1}", response.Result, response.Errormsg);
            if (response.Result == Result.Success)
            {
                // 创建是唯一没有全局 Notify 的操作，只有这个操作需要在 Response 里装载大盘
                GuildManager.Instance.UpdateGuildInfo(response.Guild);
                GuildManager.Instance.Init();
                UIMessageBox msgBox =  MessageBox.Show("公会创建成功！", "系统提示", MessageBoxType.Information);
                msgBox.OnYes = () =>
                {
                    UIManager.Instance.Close(typeof(UIGuildCreate));
                    UIManager.Instance.Close(typeof(UIGuildEntry));
                    UIManager.Instance.Show<UIGuildMain>();
                };
            }
            else
            {
                MessageBox.Show(response.Errormsg, "创建失败", MessageBoxType.Error);
            }
        }

        /// <summary>
        /// 接收响应：解散公会结果反馈
        /// </summary>
        private void OnGuildDisband(object sender, GuildDisbandResponse response)
        {
            Debug.LogFormat("OnGuildDisband : 解散公会响应, Result:{0}", response.Result);
            if (response.Result == Result.Success)
            {
                MessageBox.Show("公会已成功解散！", "系统提示", MessageBoxType.Information);
            }
            else
            {
                MessageBox.Show(response.Errormsg, "解散失败", MessageBoxType.Error);
            }
        }

        /// <summary>
        /// 接收响应：离开公会结果反馈
        /// </summary>
        private void OnGuildLeave(object sender, GuildLeaveResponse response)
        {
            Debug.LogFormat("OnGuildLeave : 离开公会响应, Result:{0}", response.Result);
            if (response.Result == Result.Success)
            {
                MessageBox.Show("您已成功退出公会！", "系统提示", MessageBoxType.Information);
            }
            else
            {
                MessageBox.Show(response.Errormsg, "退出失败", MessageBoxType.Error);
            }
        }


        /// <summary>
        /// 接收响应：加入公会申请反馈 (仅代表投递成功与否)
        /// </summary>
        private void OnGuildJoinApply(object sender, GuildJoinApplyResponse response)
        {
            Debug.LogFormat("OnGuildJoinApply : 申请入会响应, Result:{0}", response.Result);
            if (response.Result == Result.Success)
            {
                MessageBox.Show("入会申请已发送，请等待管理层审批。", "系统提示", MessageBoxType.Information);
            }
            else
            {
                MessageBox.Show(response.Errormsg, "申请失败", MessageBoxType.Error);
            }
        }

        /// <summary>
        /// 接收响应：审批入会申请反馈
        /// </summary>
        private void OnGuildApplyProcess(object sender, GuildApplyProcessResponse response)
        {
            Debug.LogFormat("OnGuildApplyProcess : 审批操作响应, Result:{0}", response.Result);
            if (response.Result == Result.Success)
            {
                MessageBox.Show("审批操作执行成功。", "系统提示", MessageBoxType.Information);
            }
            else
            {
                MessageBox.Show(response.Errormsg, "审批失败", MessageBoxType.Error);
            }
        }

        /// <summary>
        /// 接收响应：公会管理层操作反馈
        /// </summary>
        private void OnGuildAdmin(object sender, GuildAdminResponse response)
        {
            Debug.LogFormat("OnGuildAdmin : 管理操作响应, Result:{0}", response.Result);
            if (response.Result == Result.Success)
            {
                MessageBox.Show("管理操作执行成功。", "系统提示", MessageBoxType.Information);
            }
            else
            {
                MessageBox.Show(response.Errormsg, "操作失败", MessageBoxType.Error);
            }
        }

        /// <summary>
        /// 接收响应：公会设置修改反馈
        /// </summary>
        private void OnGuildSettingModify(object sender, GuildSettingModifyResponse response)
        {
            Debug.LogFormat("OnGuildSettingModify : 设置修改响应, Result:{0}", response.Result);
            if (response.Result == Result.Success)
            {
                MessageBox.Show("公会设置修改成功。", "系统提示", MessageBoxType.Information);
            }
            else
            {
                MessageBox.Show(response.Errormsg, "修改失败", MessageBoxType.Error);
            }
        }

        /// <summary>
        /// 接收通知：成员列表新增成员或职位覆盖变更
        /// </summary>
        private void OnGuildMemberAddNotify(object sender, GuildMemberAddNotify notify)
        {
            Debug.LogFormat("OnGuildMemberAddNotify : 收到成员新增/职位覆盖广播, ID:{0}", notify.NewMember.CharacterId);
            GuildManager.Instance.AddOrUpdateMember(notify.NewMember);
        }

        /// <summary>
        /// 接收通知：成员列表移除成员 (自己被踢也会收到此指令)
        /// </summary>
        private void OnGuildMemberRemoveNotify(object sender, GuildMemberRemoveNotify notify)
        {
            Debug.LogFormat("OnGuildMemberRemoveNotify : 收到成员移除广播, ID:{0}", notify.CharacterId);

            // 特判：如果被移除的这个 ID 是自己，说明被踢了或公会解散了
            if (notify.CharacterId == User.Instance.CurrentCharacter.Id)
            {
                Debug.Log("OnGuildMemberRemoveNotify : 发现自己被踢出公会，正在清空本地公会数据...");
                GuildManager.Instance.ClearMyGuildData();
            }
            else
            {
                GuildManager.Instance.RemoveMember(notify.CharacterId);
            }
        }

        /// <summary>
        /// 接收通知：公会收到新的入会申请
        /// </summary>
        private void OnGuildApplyAddNotify(object sender, GuildApplyAddNotify notify)
        {
            Debug.LogFormat("OnGuildApplyAddNotify : 收到新入会申请广播, 申请人:{0}", notify.NewApply.Name);
            GuildManager.Instance.AddApply(notify.NewApply);
        }

        /// <summary>
        /// 接收通知：入会申请已被处理或撤销 (UI从列表中移除该条目)
        /// </summary>
        private void OnGuildApplyRemoveNotify(object sender, GuildApplyRemoveNotify notify)
        {
            Debug.LogFormat("OnGuildApplyRemoveNotify : 收到移除申请广播, 目标ID:{0}", notify.CharacterId);
            GuildManager.Instance.RemoveApply(notify.CharacterId);
        }

        /// <summary>
        /// 接收通知：公会大盘基础信息发生变更 (如：宗旨修改、会长转让)
        /// </summary>
        private void OnGuildInfoChangeNotify(object sender, GuildInfoChangeNotify notify)
        {
            Debug.Log("OnGuildInfoChangeNotify : 收到公会大盘数据更新广播");
            GuildManager.Instance.UpdateGuildInfo(notify.GuildInfo);
        }

        /// <summary>
        /// 接收通知：本人的入会申请命运裁决结果 (同意/拒绝)
        /// </summary>
        private void OnGuildApplyResultNotify(object sender, GuildApplyResultNotify notify)
        {
            Debug.LogFormat("OnGuildApplyResultNotify : 收到本人的入会结果, 公会:{0}, 同意:{1}", notify.GuildName, notify.IsAccept);

            // 核心闭环：如果被同意加入了公会，立刻重新向服务器拉取全量数据
            if (notify.IsAccept)
            {
                GuildManager.Instance.Init();

                UIMessageBox msgbox = MessageBox.Show($"恭喜，您已成功加入公会【{notify.GuildName}】！", "系统提示", MessageBoxType.Information);
                msgbox.OnYes = () =>
                {
                    UIManager.Instance.Close(typeof(UIGuildJoin));
                    UIManager.Instance.Close(typeof(UIGuildEntry));

                    UIManager.Instance.Show<UIGuildMain>();
                };
            } else
            {
                MessageBox.Show($"很遗憾，您加入公会【{notify.GuildName}】的申请被拒绝了。", "系统提示", MessageBoxType.Information);
            }
        }



    }
}
