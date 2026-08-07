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



            NetConnection<NetSession> targetConnection = SessionManager.Instance.GetSession(targetId);
            if(targetConnection != null)
            {
                // 在线删除：需要处理两份数据 Manager数据和 数据库数据
                Log.InfoFormat("OnFriendRemoveRequest : 目标被删者 ID:{0} 在线, 同步清理其内存中的好友数据", targetId);
                targetConnection.Session.Character.friendManager.RemoveFriend(requesterId);
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
        //public void OnFriendRemoveRequest(NetConnection<NetSession> sender, FriendRemoveRequest request)
        //{
        //    // 这个方法用来处理 离线删除的情况
        //    // 所以正常来说 不管离线还是在线 我们都可以这样处理

        //    // 我们要删除申请者的好友 将删除好友交给他的Manager去处理
        //    sender.Session.Character.friendManager.RemoveFriend(request.friendId);
        //    // 删除被删除者的好友

        //    // 查找到这条记录 好友是申请者 主人是被删除者
        //    TCharacterFriend targetRecord = null;
        //    foreach(var record in DBService.Instance.Entities.TCharacterFriendSet)
        //    {
        //        if(record.FriendID == sender.Session.Character.Id && record.TCharacterID == request.friendId)
        //        {
        //            targetRecord = record;
        //            break;
        //        }
        //    }
        //    // 找到记录之后删除记录（记录其实就是一个类而已可以使用Remove删除 但是使用Remove要将那个对象遍历出来）
        //    DBService.Instance.Entities.TCharacterFriendSet.Remove(targetRecord);




        //    // 如果被删除者在线 我们需要对被删除者的内存也进行清理
        //    NetConnection<NetSession> onlineConnection = SessionManager.Instance.GetSession(request.friendId);
        //    if(onlineConnection != null)
        //    {
        //        Character character = CharacterManager.Instance.GetCharacter(request.friendId);
        //        // request.Id真的是请求者的Id 吗
        //        character.friendManager.RemoveFriend(request.Id);
        //        // 如果被删除者在线 我们需要发送消息通知他 由他的postProcess来通知他 为什么要使用PostProcess呢这里
        //        character.friendManager.friendChanged = true;
        //        character.friendManager.PostResponse(onlineConnection.Session.Response);
        //    }


        //    // 返回消息给主动删除的玩家
        //    sender.Session.Response.friendRemove = new FriendRemoveResponse();
        //    sender.Session.Response.friendRemove.Result = Result.Success;
        //    sender.Session.Response.friendRemove.Id = request.friendId;
        //    sender.SendResponse();
        //}
        //public void OnFriendRemoveRequest(NetConnection<NetSession> sender, FriendRemoveRequest request)
        //{
        //    // 想要删除一个好友 暂且我们假设要在线才能删除 虽然这个逻辑不是很合理
        //    // 因为正常来说 不管在不在线都是要可以删除的
        //    // 为什么要在线删除 原因是我们如果不在线删除 我拿不到character 也就没法调用他Manager中的删除方法
        //    // 就没有办法对数据库中的数据进行增删改查的操作 关于对方玩家不在线的删除逻辑 这个是一个新的好友系统设计
        //    // 我们以后再做考虑
        //    NetConnection<NetSession> targetConnection = SessionManager.Instance.GetSession(request.friendId);

        //    // 如果当前玩家不在线
        //    if (targetConnection == null)
        //    {
        //        sender.Session.Response.friendRemove = new FriendRemoveResponse();
        //        sender.Session.Response.friendRemove.Result = Result.Failed;
        //        sender.Session.Response.friendRemove.Errormsg = "当前好友不在线";
        //        sender.SendResponse();
        //        return;
        //    }

        //    // 接下来处理玩家在线的情况
        //    // 我们需要分别发送信息给双方玩家
        //    Character requesterCharacter = sender.Session.Character;
        //    // Character targetCharacter = CharacterManager.Instance.GetCharacter(request.friendId);
        //    // 去Manager中查找字典是会消耗性能的 我们直接从targetConnection中获取 有助于提高性能
        //    Character targetCharacter = targetConnection.Session.Character;
        //    requesterCharacter.friendManager.RemoveFriend(request.friendId);
        //    targetCharacter.friendManager.RemoveFriend(request.Id);

        //    NetConnection<NetSession> requesterConnection = sender;

        //    requesterConnection.Session.Response.friendRemove = new FriendRemoveResponse();
        //    requesterConnection.Session.Response.friendRemove.Result = Result.Success;
        //    requesterConnection.Session.Response.friendRemove.Id = request.friendId;
        //    requesterConnection.SendResponse();

        //    targetConnection.Session.Response.friendRemove = new FriendRemoveResponse();
        //    targetConnection.Session.Response.friendRemove.Result = Result.Success;
        //    targetConnection.Session.Response.friendRemove.Id = request.Id;
        //    targetConnection.SendResponse();

        //}
    }
}
