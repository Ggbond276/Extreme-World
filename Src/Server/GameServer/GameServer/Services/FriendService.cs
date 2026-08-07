using Common;
using GameServer.Entities;
using GameServer.Manager;
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
    class FriendService : Singleton<FriendService>
    {
        // 首先写构造方法 注册应该注册的网络协议
        public FriendService()
        {
            Log.InfoFormat("FriendService : 服务端好友服务初始化并订阅网络协议");
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<FriendAddRequest>(this.OnFriendAddRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<FriendAddResponse>(this.OnFriendAddResponse);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<FriendRemoveRequest>(this.OnFriendRemoveRequest);
        }

        // Init这个空方式 只是为了用来触发单例调用构造方法使用的 具体细节可以看Singleton的源码实现
        public void Init() { }

        /// <summary>
        /// 处理好友添加请求 转发好友请求给对应的好友客户端
        /// </summary>
        public void OnFriendAddRequest(NetConnection<NetSession> sender, FriendAddRequest request)
        {
            
            sender.Session.Response.friendAdd = new FriendAddResponse();
            //request
            // toId : 想添加的人的Id
            // fromId : 我的Id

            // 1. 获取当前发起请求的玩家实体 这样我就可以知道是谁发来的请求
            Character character = sender.Session.Character;

            Log.InfoFormat("OnFriendAddRequest : 收到好友添加请求, 请求者 ID:{0} Name:{1}, 目标 ID:{2} Name:{3}",
                character.Id, character.Info.Name, request.ToId, request.ToName);

            // 2. 查找目标玩家 ID
            if (request.ToId == 0)
            {
                request.ToId = CharacterManager.Instance.GetDBIdByName(request.ToName);
                Log.InfoFormat("OnFriendAddRequest : 目标ID为0, 通过Name查找到的真实目标 ID:{0}", request.ToId);
            }

            // 第一层拦截：好友是否存在数据库中的拦截
            if(request.ToId == 0)
            {
                Log.InfoFormat("OnFriendAddRequest : 添加失败, 数据库中不存在名为 {0} 的玩家", request.ToName);
                sender.Session.Response.friendAdd.Result = Result.Failed;
                sender.Session.Response.friendAdd.Errormsg = "该玩家不存在，请检查名字是否正确";
                sender.SendResponse();
                return;
            }
            // 3. 拦截：判断是不是已经是好友了
            if(character.friendManager.isFriend(request.ToId))
            {
                // 返回错误信息给客户端用户处理
                // 并且直接跳出方法
                // 这里要构建Response
                Log.InfoFormat("OnFriendAddRequest : 添加失败, 玩家 ID:{0} 已经是请求者的好友", request.ToId); 
                sender.Session.Response.friendAdd.Result = Result.Failed;
                sender.Session.Response.friendAdd.Errormsg = "此玩家已经是您的好友";
                sender.SendResponse();
                return;
            }

            // 4. 获取目标玩家的网络连接（检查是否在线）
            NetConnection<NetSession> friendConnection = SessionManager.Instance.GetSession(request.ToId);

            // 第二层拦截：好友是否在线的拦截
            if(friendConnection == null)
            {
                Log.InfoFormat("OnFriendAddRequest : 添加失败, 目标好友 ID:{0} 当前不在线", request.ToId);
                sender.Session.Response.friendAdd.Result = Result.Failed;
                sender.Session.Response.friendAdd.Errormsg = "当前好友不在线";
                sender.SendResponse();
                return;
            }

            // 5.转发消息给目标
            Log.InfoFormat("OnFriendAddRequest : 目标好友 ID:{0} 在线, 正在转发好友请求", request.ToId);
            friendConnection.Session.Response.friendAdd = new FriendAddResponse();
            friendConnection.Session.Response.friendAdd.Request = request;
            friendConnection.SendResponse();
        }

        /// <summary>
        /// 处理好友同意添加请求
        /// </summary>
        public void OnFriendAddResponse(NetConnection<NetSession> sender, FriendAddResponse response)
        {
            Log.InfoFormat("OnFriendAddResponse : 收到处理好友请求的响应, 处理结果 Result:{0}, 请求者 ID:{1}, 响应者(Sender) ID:{2}",
                response.Result, response.Request.FromId, sender.Session.Character.Id);
            // 先找到之前申请添加的那个玩家
            NetConnection <NetSession> requesterConnection = SessionManager.Instance.GetSession(response.Request.FromId);

            // 情况1：申请人不在线直接return
            if (requesterConnection == null)
            {
                Log.InfoFormat("OnFriendAddResponse : 处理失败, 之前的请求者 ID:{0} 已下线", response.Request.FromId);
                sender.Session.Response.friendAdd = new FriendAddResponse();
                sender.Session.Response.friendAdd.Result = Result.Failed;
                sender.Session.Response.friendAdd.Errormsg = "请求者已下线，添加失败";
                sender.SendResponse();
                return;
            }
                
            // 情况2：如果消息是拒绝
            if(response.Result != Result.Success)
            {
                // 拒绝的消息应给返回给请求者
                Log.InfoFormat("OnFriendAddResponse : 玩家 ID:{0} 拒绝了请求者 ID:{1} 的好友请求", sender.Session.Character.Id, response.Request.FromId);
                requesterConnection.Session.Response.friendAdd = new FriendAddResponse();
                requesterConnection.Session.Response.friendAdd.Result = Result.Failed;
                requesterConnection.Session.Response.friendAdd.Errormsg = "对方拒绝了您的好友请求";
                requesterConnection.SendResponse();
                return;
            }

            // 情况3：如果申请人和被申请人已经是好友（我不确定这一点是不是应该在客户端就该被拦截了，但是客户端是查寻不到数据库和Session，所以还是应该由服务器拦截）
            if(sender.Session.Character.friendManager.isFriend(response.Request.ToId) ||
                requesterConnection.Session.Character.friendManager.isFriend(response.Request.FromId))
            {
                Log.InfoFormat("OnFriendAddResponse : 玩家 ID:{0} 已经是 请求者 ID:{1} 的好友，请勿重复添加", response.Request.FromId, sender.Session.Character.Id);
                sender.Session.Response.friendAdd = new FriendAddResponse();
                sender.Session.Response.friendAdd.Result = Result.Failed;
                sender.Session.Response.friendAdd.Errormsg = "对方已是您的好友";
                sender.SendResponse();
                return;
            }




            // 情况4：接下来我们处理同意的情况
            Log.InfoFormat("OnFriendAddResponse : 玩家 ID:{0} 同意了您的好友请求 (请求者 ID:{1})", sender.Session.Character.Id, response.Request.FromId);
            Character requester = CharacterManager.Instance.GetCharacter(response.Request.FromId);
            Character replier = CharacterManager.Instance.GetCharacter(response.Request.ToId);
            // 将申请者加入到 请求者的好友列表
            requester.friendManager.AddFriend(replier);
            // 将请求者加入到 申请者的好友列表
            replier.friendManager.AddFriend(requester);


            // 将同意的消息转发出去
            Log.InfoFormat("OnFriendAddResponse : 双方内存好友数据添加完成, 触发全量同步逻辑");
            //sender.Session.Response.friendAdd = response;
            //sender.SendResponse();
            requesterConnection.Session.Response.friendAdd = new FriendAddResponse();
            requesterConnection.Session.Response.friendAdd.Result = Result.Success;
            requesterConnection.SendResponse();

            sender.Session.Response.friendAdd = new FriendAddResponse();
            sender.Session.Response.friendAdd.Result = Result.Success;
            sender.SendResponse();
        }

        /// <summary>
        /// 处理好友删除请求 friendId 是被删除者 id是请求者
        /// </summary>
        public void OnFriendRemoveRequest(NetConnection<NetSession> sender, FriendRemoveRequest request)
        {
            int requesterId = sender.Session.Character.Id; // 请求删除者
            int targetId = request.friendId; // 被删除者
            Log.InfoFormat("OnFriendRemoveRequest : 收到好友删除请求, 请求者 ID:{0}, 目标被删者 ID:{1}", requesterId, targetId);

            // 主动删除者删除目标好友
            sender.Session.Character.friendManager.RemoveFriend(targetId);

            // 处理被动删除者
            NetConnection<NetSession> targetConnection = SessionManager.Instance.GetSession(targetId);

            if(targetConnection != null)
            {
                // 在线删除：需要处理两份数据 Manager数据和 数据库数据
                Log.InfoFormat("OnFriendRemoveRequest : 目标被删者 ID:{0} 在线, 同步清理其内存中的好友数据", targetId);
                targetConnection.Session.Character.friendManager.RemoveFriend(requesterId);
                targetConnection.SendResponse();
            } else
            {
                // 离线删除：只需要处理数据库数据 并不需要处理内存Manager数据
                Log.InfoFormat("OnFriendRemoveRequest : 目标被删者 ID:{0} 离线, 准备清理数据库中的关联记录", targetId);
                TCharacterFriend targetRecord = null;

                // 1。找到这条好友记录
                foreach(var record in  DBService.Instance.Entities.TCharacterFriendSet)
                {
                    if(record.FriendID == requesterId && record.TCharacterID == targetId)
                    {
                        targetRecord = record;
                        break;
                    }
                }

                // 2。从数据库删除这条好友记录
                if(targetRecord != null)
                {
                    Log.InfoFormat("OnFriendRemoveRequest : 成功删除离线目标 ID:{0} 相关的数据库记录", targetId);
                    DBService.Instance.Entities.TCharacterFriendSet.Remove(targetRecord);
                    DBService.Instance.save();
                }
            }


            Log.InfoFormat("OnFriendRemoveRequest : 删除流程执行完毕, 返回成功响应给请求者 ID:{0}", requesterId);
            sender.Session.Response.friendRemove = new FriendRemoveResponse();
            sender.Session.Response.friendRemove.Result = Result.Success;
            sender.Session.Response.friendRemove.Id = targetId;
            sender.SendResponse();


        }
    }
}
