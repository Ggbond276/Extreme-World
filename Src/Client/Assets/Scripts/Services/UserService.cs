using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Network;
using UnityEngine;

using SkillBridge.Message;
using Models;
using Assets.Scripts.Services;
using Managers;

namespace Services
{
    class UserService : Singleton<UserService>, IDisposable
    {

        public UnityEngine.Events.UnityAction<Result, string> OnLogin;
        public UnityEngine.Events.UnityAction<Result, string> OnRegister;
        public UnityEngine.Events.UnityAction<Result, string> OnCharacterCreate;
        //pendingMessage is a Queue
        NetMessage pendingMessage = null;
        //connected is used to determine whether the client and the server are connected
        bool connected = false;

        //构造的时候执行
        public UserService()
        {
            NetClient.Instance.OnConnect += OnGameServerConnect;
            NetClient.Instance.OnDisconnect += OnGameServerDisconnect;
            //注册登录协议 接收服务端发送来的响应
            MessageDistributer.Instance.Subscribe<UserLoginResponse>(this.OnUserLogin);
            //注册注册协议 接收服务端发送来的响应
            MessageDistributer.Instance.Subscribe<UserRegisterResponse>(this.OnUserRegister);
            //注册角色创建协议 接收服务端发送来的响应
            MessageDistributer.Instance.Subscribe<UserCreateCharacterResponse>(this.OnUserCreateCharacter);
            MessageDistributer.Instance.Subscribe<UserGameEnterResponse>(this.OnGameEnter);
            MessageDistributer.Instance.Subscribe<UserGameLeaveResponse>(this.OnGameLeave);

        }
        //销毁的时候执行
        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<UserLoginResponse>(this.OnUserLogin);
            MessageDistributer.Instance.Unsubscribe<UserRegisterResponse>(this.OnUserRegister);
            MessageDistributer.Instance.Unsubscribe<UserCreateCharacterResponse>(this.OnUserCreateCharacter);
            MessageDistributer.Instance.Unsubscribe<UserGameEnterResponse>(this.OnGameEnter);
            MessageDistributer.Instance.Unsubscribe<UserGameLeaveResponse>(this.OnGameLeave);
            NetClient.Instance.OnConnect -= OnGameServerConnect;
            NetClient.Instance.OnDisconnect -= OnGameServerDisconnect;
        }
        public void Init()
        {

        }
        
        // 1. 连接到服务器 ConnectToServer()
        public void ConnectToServer()
        {
            Debug.Log("ConnectToServer() Start ");
            //NetClient.Instance.CryptKey = this.SessionId;
            NetClient.Instance.Init("127.0.0.1", 8000);
            NetClient.Instance.Connect();
        }
        // 2. 检测是否连接到服务器 OnGameServerConnect(int result, string reason)
        void OnGameServerConnect(int result, string reason)
        {
            Log.InfoFormat("LoadingMesager::OnGameServerConnect :{0} reason:{1}", result, reason);
            if (NetClient.Instance.Connected)
            {
                this.connected = true;
                if (this.pendingMessage != null)
                {
                    NetClient.Instance.SendMessage(this.pendingMessage);
                    this.pendingMessage = null;
                }
            }
            else
            {
                if (!this.DisconnectNotify(result, reason))
                {
                    MessageBox.Show(string.Format("网络错误，无法连接到服务器！\n RESULT:{0} ERROR:{1}", result, reason), "错误", MessageBoxType.Error);
                }
            }
        }
        // 3. 检查是否和服务器断开连接 OnGameServerDisconnect(int result, string reason)
        public void OnGameServerDisconnect(int result, string reason)
        {
            this.DisconnectNotify(result, reason);
            return;
        }
        // 4. 服务器断连的提示 DisconnectNotify(int result,string reason)
        bool DisconnectNotify(int result, string reason)
        {
            if (this.pendingMessage != null)
            {
                if (this.pendingMessage.Request.userLogin != null)
                {
                    if (this.OnLogin != null)
                    {
                        this.OnLogin(Result.Failed, string.Format("服务器断开！\n RESULT:{0} ERROR:{1}", result, reason));
                    }
                }
                else if (this.pendingMessage.Request.userRegister != null)
                {
                    if (this.OnRegister != null)
                    {
                        this.OnRegister(Result.Failed, string.Format("服务器断开！\n RESULT:{0} ERROR:{1}", result, reason));
                    }
                }
                else
                {
                    if (this.OnCharacterCreate != null)
                    {
                        this.OnCharacterCreate(Result.Failed, string.Format("服务器断开！\n RESULT:{0} ERROR:{1}", result, reason));
                    }
                }
                return true;
            }
            return false;
        }


        // 用户登录
        public void SendLogin(string user, string passward)
        {
            // 在日志中打印将登录信息发送给服务器
            Debug.LogFormat("UserLoginRequest::user :{0} psw :{1}", user, passward);

            // 封装发送给服务器的消息
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.userLogin = new UserLoginRequest();
            message.Request.userLogin.User = user;
            message.Request.userLogin.Passward = passward;

            // 检查是否连接到服务器并发送消息
            if (this.connected && NetClient.Instance.Connected)
            {
                pendingMessage = null;
                NetClient.Instance.SendMessage(message);
            }
            else
            {
                pendingMessage = message;
                this.ConnectToServer();
            }
        }
        void OnUserLogin(object sender, UserLoginResponse response)
        {
            Debug.LogFormat("OnLogin:{0} [{1}]", response.Result, response.Errormsg);

            if (response.Result == Result.Success)
            {
                //登陆成功逻辑
                User.Instance.SetupUserInfo(response.Userinfo);
            };
            if (this.OnLogin != null)
            {
                this.OnLogin(response.Result, response.Errormsg);

            }
        }


        // 用户注册
        public void SendRegister(string user, string psw)
        {
            // 在日志中打印将注册信息
            Debug.LogFormat("UserRegisterRequest::user :{0} psw:{1}", user, psw);

            // 封装发送给服务器的信息
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.userRegister = new UserRegisterRequest();
            //write the message which you want to send in the message object
            message.Request.userRegister.User = user;
            message.Request.userRegister.Passward = psw;

            // 检查是否连接到服务器并发送信息
            if (this.connected && NetClient.Instance.Connected)
            {
                // pendingMessage是一个缓存容器 当客户端与服务端的链接失败的时候 我们要发送的数据会被存储到
                // pendingMessage这个缓存容器中 然后尝试重新连接服务器 并再次尝试发送
                this.pendingMessage = null;
                NetClient.Instance.SendMessage(message);
            }
            else
            {
                //If the client and the Server are fail to connected , push the message into the cache container
                this.pendingMessage = message;
                //Reconnect to the Server
                this.ConnectToServer();
            }
        }
        void OnUserRegister(object sender, UserRegisterResponse response)
        {
            Debug.LogFormat("OnUserRegister:{0} [{1}]", response.Result, response.Errormsg);

            if (this.OnRegister != null)
            {
                this.OnRegister(response.Result, response.Errormsg);
            }
        }


        // 用户创建角色
        public void SendCharacterCreate(string name, CharacterClass cls)
        {
            #region 在日志中打印角色创建信息
            Debug.LogFormat("UserCreateCharacterRequest::name :{0} class:{1}", name, cls);
            #endregion

            #region 封装发送给服务器的信息
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.createChar = new UserCreateCharacterRequest();
            message.Request.createChar.Name = name;
            message.Request.createChar.Class = cls;
            #endregion

            #region 检查是否成功连接到服务器
            if (this.connected && NetClient.Instance.Connected)
            {
                this.pendingMessage = null;
                NetClient.Instance.SendMessage(message);
            }
            else
            {
                this.pendingMessage = message;
                this.ConnectToServer();
            }
            #endregion
        }
        void OnUserCreateCharacter(object sender, UserCreateCharacterResponse response)
        {
            Debug.LogFormat("OnUserCreateCharacter:{0} [{1}]", response.Result, response.Errormsg);

            if (response.Result == Result.Success)
            {
                Models.User.Instance.Info.Player.Characters.Clear();
                Models.User.Instance.Info.Player.Characters.AddRange(response.Characters);
            }

            if (this.OnCharacterCreate != null)
            {
                this.OnCharacterCreate(response.Result, response.Errormsg);
            }
        }


        // 角色进入游戏
        public void SendGameEnter(int characterIdx)
        {
            //打印日志 告知现在进行的是角色进入游戏
            Debug.LogFormat("UserGameEnterRequest :: characterId : {0}", characterIdx);
            //打包封装角色进入游戏请求
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.gameEnter = new UserGameEnterRequest();
            message.Request.gameEnter.characterIdx = characterIdx;
            //发送请求给服务器
            NetClient.Instance.SendMessage(message);
        }
        void OnGameEnter(object sender, UserGameEnterResponse response)
        {
            Debug.LogFormat("OnGameEnter : {0} [{1}]", response.Result, response.Errormsg);
            // 如果受到了服务器成功进入游戏的请求
            if (response.Result == Result.Success)
            {
                NCharacterInfo Info = response.Character;
                ItemManager.Instance.Init(Info.Items);
                // 初始化背包
                BagManager.Instance.Init(Info.Bag);

                #region 测试物品和背包的数据已经被传输过来了
                //foreach (var item in ItemManager.Instance.Items)
                //{
                //    Debug.LogErrorFormat("Item ID : [ {0} ]  Count : [ {1} ]", item.Value.ItemID, item.Value.Count);
                //}
                //Debug.LogErrorFormat("BagUnlock : [ {0} ]", Info.Bag.Unlocked);
                //Debug.LogErrorFormat("BagItemsCount : [ {0} ]", BagManager.Instance.bagItems.Length);
                #endregion

            }
        }


        // 角色离开游戏
        public void SendGameLeave()
        {
            Debug.LogFormat("UserGameLeaveRequest");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.gameLeave = new UserGameLeaveRequest();
            NetClient.Instance.SendMessage(message);
        }
        void OnGameLeave(object sender, UserGameLeaveResponse response)
        {
            Debug.LogFormat("OnGameLeave : {0} [{1}]", response.Result, response.Errormsg);
            User.Instance.CurrentCharacter = null;
            MapService.Instance.CurrentMapId = 0;
        }



    }
}
