using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Network;
using SkillBridge.Message;
using GameServer.Entities;
using GameServer.Manager;
using GameServer.Models;
using GameServer.Managers;

namespace GameServer.Services
{
    /// <summary>
    /// 用户服务类 (单例)
    /// 负责处理与用户账户、角色相关的核心网络请求（登录、注册、创建角色、进出游戏等）
    /// </summary>
    class UserService : Singleton<UserService>
    {
        /// <summary>
        /// 构造函数
        /// 消息分发器会将客户端传送来的信息分发给这里订阅的对应消息处理方法
        /// </summary>
        public UserService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserLoginRequest>(this.OnLogin);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserRegisterRequest>(this.OnRegister);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserCreateCharacterRequest>(this.OnCreateCharacter);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameEnterRequest>(this.OnGameEnter);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameLeaveRequest>(this.OnGameLeave);
        }

        /// <summary>
        /// 服务初始化方法
        /// </summary>
        public void Init()
        {

        }

        /// <summary>
        /// 处理客户端的用户登录请求
        /// 验证数据库中的账号密码，并返回用户的角色列表信息
        /// </summary>
        /// <param name="sender">网络连接会话 (包含客户端 Session 信息)</param>
        /// <param name="request">客户端发送的登录请求数据</param>
        void OnLogin(NetConnection<NetSession> sender, UserLoginRequest request)
        {
            Log.InfoFormat("UserLoginRequest: User:{0}  Pass:{1}", request.User, request.Passward);

            sender.Session.Response.userLogin = new UserLoginResponse();

            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user == null)
            {
                sender.Session.Response.userLogin.Result = Result.Failed;
                sender.Session.Response.userLogin.Errormsg = "用户不存在";
            }
            else if (user.Password != request.Passward)
            {
                sender.Session.Response.userLogin.Result = Result.Failed;
                sender.Session.Response.userLogin.Errormsg = "密码错误";
            }
            else
            {
                sender.Session.User = user;
                sender.Session.Response.userLogin.Result = Result.Success;
                sender.Session.Response.userLogin.Errormsg = "None";
                sender.Session.Response.userLogin.Userinfo = new NUserInfo();
                sender.Session.Response.userLogin.Userinfo.Id = (int)user.ID;
                sender.Session.Response.userLogin.Userinfo.Player = new NPlayerInfo();
                sender.Session.Response.userLogin.Userinfo.Player.Id = user.Player.ID;

                foreach (var cha in user.Player.Characters)
                {
                    NCharacterInfo info = new NCharacterInfo();
                    info.Id = cha.ID;
                    // 标记一下这里的修改 因为Tid之前标注的是职业类型Tid 现在更改掉
                    info.ConfigId = cha.ConfigId;
                    info.Name = cha.Name;
                    info.Type = CharacterType.Player;
                    info.Class = (CharacterClass)cha.Class;
                    sender.Session.Response.userLogin.Userinfo.Player.Characters.Add(info);
                }

            }
            sender.SendResponse();
        }

        /// <summary>
        /// 处理客户端的用户注册请求
        /// 检查用户名是否重复，如果不重复则在数据库中创建新用户和对应的 Player 数据
        /// </summary>
        /// <param name="sender">网络连接会话</param>
        /// <param name="request">客户端发送的注册请求数据</param>
        void OnRegister(NetConnection<NetSession> sender, UserRegisterRequest request)
        {

            Log.InfoFormat("UserRegisterRequest: User:{0}  Pass:{1}", request.User, request.Passward);

            sender.Session.Response.userRegister = new UserRegisterResponse();

            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user != null)
            {
                sender.Session.Response.userRegister.Result = Result.Failed;
                sender.Session.Response.userRegister.Errormsg = "用户已存在.";
            }
            else
            {
                TPlayer player = DBService.Instance.Entities.Players.Add(new TPlayer());
                DBService.Instance.Entities.Users.Add(new TUser() { Username = request.User, Password = request.Passward, Player = player });
                DBService.Instance.Entities.SaveChanges();
                sender.Session.Response.userRegister.Result = Result.Success;
                sender.Session.Response.userRegister.Errormsg = "None";
            }
            sender.SendResponse();
        }

        /// <summary>
        /// 处理客户端的创建角色请求
        /// 初始化角色的名称、职业、出生点坐标、初始金币及背包数据，并保存到数据库
        /// </summary>
        /// <param name="sender">网络连接会话</param>
        /// <param name="request">客户端发送的创建角色请求数据</param>
        private void OnCreateCharacter(NetConnection<NetSession> sender, UserCreateCharacterRequest request)
        {
            // 打印日志
            Log.InfoFormat("UserCreateCharacterRequest: Name:{0}  Class:{1}", request.Name, request.Class);

            // 第一条数据库信息建立字段
            TCharacter character = new TCharacter()
            {
                ConfigId = (int)request.Class,
                Name = request.Name,
                Class = (int)request.Class,
                MapID = 1,
                MapPosX = 5000,
                MapPosY = 4000,
                MapPosZ = 820,
                // 新手大礼包
                Gold = 100000,
                Equips = new byte[28],
            };

           // 第二条数据库信息建立字段
            var bag = new TCharacterBag();
            bag.Owner = character;
            bag.Items = new byte[0];
            bag.Unlocked = 20;


            DBService.Instance.Entities.CharacterBag.Add(bag);
            DBService.Instance.Entities.Characters.Add(character);

            sender.Session.User.Player.Characters.Add(character);
            DBService.Instance.Entities.SaveChanges();

            sender.Session.Response.createChar = new UserCreateCharacterResponse();
            sender.Session.Response.createChar.Result = Result.Success;
            sender.Session.Response.createChar.Errormsg = "None";

            // 为什么加了这一段代码之后角色选择列表在创建完角色之后就可以实时显示了
            foreach (var cha in sender.Session.User.Player.Characters)
            {
                NCharacterInfo info = new NCharacterInfo();
                info.Id = cha.ID;
                // 这里希望Tid访问的是数据库的id 修改为这里需要访问的是配置表的Id
                info.ConfigId = cha.ConfigId;
                info.Name = cha.Name;
                info.Type = CharacterType.Player;
                info.Class = (CharacterClass)cha.Class;
                sender.Session.Response.createChar.Characters.Add(info);
            }
            sender.SendResponse();

        }

        /// <summary>
        /// 处理角色的进入游戏请求
        /// 根据选择的角色索引实例化角色，绑定到当前会话，并将其加入到对应的地图管理器中
        /// </summary>
        /// <param name="sender">网络连接会话</param>
        /// <param name="request">客户端发送的进入游戏请求数据</param>
        private void OnGameEnter(NetConnection<NetSession> sender, UserGameEnterRequest request)
        {
            //查找出相应的角色信息
            TCharacter dbchar = sender.Session.User.Player.Characters.ElementAt(request.characterIdx);
            //打印日志
            Log.InfoFormat("UserGameEnterRequest : CharacterID: {0} CharacterName: {1}  MapID: {2} ", dbchar.ID, dbchar.Name, dbchar.MapID);
            // 将角色添加到角色管理器(实体ID的分配时机就是在进入游戏的时候分配的)
            Character character = CharacterManager.Instance.AddCharascter(dbchar);

            // 返回信息
            sender.Session.Response.gameEnter = new UserGameEnterResponse();
            sender.Session.Response.gameEnter.Result = Result.Success;
            sender.Session.Response.gameEnter.Errormsg = "None";
            sender.Session.Response.gameEnter.Character = character.Info;
            sender.SendResponse();

            #region  测试当玩家进入游戏的时候有道具生成
            //if(character.ItemManager.Items.Count == 0)
            //{
            //    character.ItemManager.AddItem(1, 100);
            //    character.ItemManager.AddItem(2, 50);
            //    character.ItemManager.AddItem(3, 200);
            //    character.ItemManager.AddItem(4, 300);
            //    Log.Info("Item添加成功");
            //}
            #endregion

            sender.Session.Character = character;
            MapManager.Instance[dbchar.MapID].CharacterEnter(sender, character);
        }

        /// <summary>
        /// 处理角色的离开游戏请求
        /// 获取当前会话中的角色，并触发角色离开游戏的清理逻辑
        /// </summary>
        /// <param name="sender">网络连接会话</param>
        /// <param name="request">客户端发送的离开游戏请求数据</param>
        private void OnGameLeave(NetConnection<NetSession> sender, UserGameLeaveRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("OnGameLeave: characterID: {0} : {1} Map: {2}", character.entityId, character.Info.Name, character.Info.mapId);

            CharacterLeave(character);

            sender.Session.Response.gameLeave = new UserGameLeaveResponse();
            sender.Session.Response.gameLeave.Result = Result.Success;
            sender.Session.Response.gameLeave.Errormsg = "None";
            sender.SendResponse();
        }

        /// <summary>
        /// 执行角色离开游戏的具体清理逻辑
        /// 从角色管理器和对应的地图管理器中彻底移除该角色
        /// </summary>
        /// <param name="character">需要离开游戏的角色对象</param>
        public void CharacterLeave(Character character)
        {
            CharacterManager.Instance.RemoveCharacter(character.entityId);
            MapManager.Instance[character.Info.mapId].CharacterLeave(character);
        }
    }
}
