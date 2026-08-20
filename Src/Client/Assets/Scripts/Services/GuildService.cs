using Assets.Scripts.Managers;
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
        }

        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<GuildMemberListResponse>(this.OnGuildMemberList);
            MessageDistributer.Instance.Unsubscribe < GuildApplyListResponse>(this.OnGuildApplyList);
            MessageDistributer.Instance.Unsubscribe<GuildListResponse>(this.OnGuildList);
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


    }
}
