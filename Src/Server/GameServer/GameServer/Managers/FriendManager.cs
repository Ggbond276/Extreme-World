using GameServer.Entities;
using GameServer.Manager;
using GameServer.Models;
using GameServer.Services;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class FriendManager : IPostResponser
    {
        public Character Owner { get; private set; }

        /// <summary>
        /// 这里使用Character的DBId作为键值
        /// </summary>
        public Dictionary<int, Friend> friends = new Dictionary<int, Friend>();

        public bool friendChanged = false;

        public FriendManager(Character character)
        {
            this.Owner = character;
            // 构造方法中需要利用Onwer的Data属性 从数据库里面拿出数据填充到Manager容器中
            this.InitFriends();
            // 通知好友我在线
            this.NotifyOnlineStatus(true);
        }

        /// <summary>
        /// 玩家进入游戏的时候，拉取数据库中的角色数据加入容器
        /// </summary>
        private void InitFriends()
        {
            // 首先清除容器
            this.friends.Clear();

            // 遍历底层的数据库取出数据
            foreach(var dbFriend in Owner.Data.Friends)
            {

                // 根据数据库信息创建好友
                Friend friend = new Friend(dbFriend);
                Character onlineCharacter = CharacterManager.Instance.GetCharacter(dbFriend.Id);

                // 对好友的在线状态赋值
                if(onlineCharacter != null)
                {
                    friend.isOnline = true;
                } else
                {
                    friend.isOnline = false;
                }

                // 将好友加入到自己的容器中
                this.friends[dbFriend.FriendID] = friend; 

                
            }
        }

        /// <summary>
        /// 通知好友我目前的在线状态
        /// </summary>
        /// <param name="status"></param>
        public void NotifyOnlineStatus(bool status)
        {
            foreach(var friend in this.friends.Values)
            {
                NetConnection<NetSession> friendConnection = SessionManager.Instance.GetSession(friend.FriendId);
                if(friendConnection != null)
                {
                    // 通知这个好友 我在线
                    // 怎么通知呢 我把我的ID给他 还有我上下线的状态给他
                    int targetId = this.Owner.Id;
                    // 他会根据我的ID去他的Manager中寻找到我
                    // 然后修改一下我的上下线状态(但是上线下线的修改涉及到修改内部容器 所以我们需要暴露一个方法出去专门用来修改内部容器的状态)
                    friendConnection.Session.Character.friendManager.UpdateFriendStatus(targetId, status);
                    
                }
            }
        }

        /// <summary>
        /// 修改好友的状态
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Status"></param>
        public void UpdateFriendStatus(int Id, bool Status)
        {
            // 修改完对方的内存状态之后 将对方的FriendChanged改成true 后面自动会更新的
            Friend friend = this.friends[Id];
            friend.isOnline = Status;
            this.friendChanged = true;
        }


        /// <summary>
        /// 将Manager中的数据打包成网络消息返回
        /// </summary>
        /// <param name="list"></param>
        public void GetFriendInfo(List<NFriendInfo> list)
        {
            // 遍历friends中的friend 将自己转成NFriendInfo 加入list返回
            foreach(var v in this.friends.Values)
            {
                list.Add(v.ToNFriendInfo());
            }
        }

       /// <summary>
       /// 判断是否存在这个好友
       /// </summary>
       /// <param name="id"></param>
       /// <returns></returns>
        public bool isFriend(int id)
        {
            return friends.ContainsKey(id);
        }

        /// <summary>
        /// 将好友添加到数据库中
        /// </summary>
        /// <param name="replier"></param>
        internal void AddFriend(Character replier)
        {
            // 将新的好友数据添加到数据库中(这里需要给外键吗 还是不需要)
            TCharacterFriend tf = new TCharacterFriend();
            tf.FriendID = replier.Id;
            tf.FriendName = replier.Data.Name;
            tf.Class = replier.Data.Class;
            tf.Level = replier.Data.Level;
            tf.TCharacterID = Owner.Id;

            this.Owner.Data.Friends.Add(tf);
            DBService.Instance.save();
            // 再将好友数据添加到Manager列表中
            Friend newFriend = new Friend(tf);
            // 优点：绝对安全。如果字典里没有这个键，它会新增；如果已经有了，它会直接覆盖，绝对不会抛出异常。
            //严谨度提升：理论上，在执行添加好友操作前，你应该已经调用过第72行的 isFriend(int id) 拦截过重复添加了。所以执行到第95行时，字典里一定没有这个好友。你可以考虑换成 this.friends.Add(newFriend.FriendId, newFriend);。
            //这样如果在并发极端情况下（比如两人同时狂点添加），如果不小心漏过了判断，Add 方法会抛出异常，帮你暴露出潜在的逻辑漏洞，而不是默默地覆盖掉数据。
            this.friends[newFriend.FriendId] = newFriend;   
        }

        /// <summary>
        /// 从数据库中删除好友
        /// </summary>
        /// <param name="friendId"></param>
        internal void RemoveFriend(int friendId)
        {
            // 先从数据库中将数据删除
            // 高级写法
            // TCharacterFriend dbFriend = this.Owner.Data.Friends.FirstOrDefault(f => f.FriendID == friendId);
            TCharacterFriend dbFriend = null;
            foreach(TCharacterFriend friend in this.Owner.Data.Friends)
            {
                if(friend.FriendID == friendId)
                {
                    dbFriend = friend;
                    break;
                }
            }

            if(dbFriend != null)
            {
                this.Owner.Data.Friends.Remove(dbFriend);
                DBService.Instance.save();
            }

            // 再到Manager容器中删除好友记录
            this.friends.Remove(friendId);
        }

        /// <summary>
        /// 消息后处理器：用于每次网络响应结束之前，统一检查并下发好友列表更新
        /// </summary>
        /// <param name="response"></param>
        public void PostResponse(NetMessageResponse response)
        {
            // 确保我们的好友列表的信息被修改了
            if(friendChanged)
            {
                if (response.friendList == null)
                    response.friendList = new FriendListResponse();


                // 新建网络信息列表 填入网络信息
                List<NFriendInfo> infos = new List<NFriendInfo>();
                this.GetFriendInfo(infos);

                // 添加到即将发送的网络消息中
                response.friendList.Friends.AddRange(infos);

                // 标志未知重置
                friendChanged = false;
            }

        }
        
    }
}
