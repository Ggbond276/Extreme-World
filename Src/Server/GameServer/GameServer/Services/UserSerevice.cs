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
    class UserService : Singleton<UserService>
    {

        public UserService()
        {
            //消息分发器 消息分发器会将客户端传送来的信息分发给对应的消息处理方法
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserLoginRequest>(this.OnLogin);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserRegisterRequest>(this.OnRegister);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserCreateCharacterRequest>(this.OnCreateCharacter);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameEnterRequest>(this.OnGameEnter);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameLeaveRequest>(this.OnGameLeave);
        }

        public void Init()
        {

        }

        //Response the request from the Client
        void OnLogin(NetConnection<NetSession> sender, UserLoginRequest request)
        {
            Log.InfoFormat("UserLoginRequest: User:{0}  Pass:{1}", request.User, request.Passward);

            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.userLogin = new UserLoginResponse();


            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user == null)
            {
                message.Response.userLogin.Result = Result.Failed;
                message.Response.userLogin.Errormsg = "用户不存在";
            }
            else if (user.Password != request.Passward)
            {
                message.Response.userLogin.Result = Result.Failed;
                message.Response.userLogin.Errormsg = "密码错误";
            }
            else
            {
                sender.Session.User = user;

                message.Response.userLogin.Result = Result.Success;
                message.Response.userLogin.Errormsg = "None";
                message.Response.userLogin.Userinfo = new NUserInfo();
                message.Response.userLogin.Userinfo.Id = (int)user.ID;
                message.Response.userLogin.Userinfo.Player = new NPlayerInfo();
                message.Response.userLogin.Userinfo.Player.Id = user.Player.ID;
                foreach (var c in user.Player.Characters)
                {
                    NCharacterInfo info = new NCharacterInfo();
                    info.Id = c.ID;
                    info.Name = c.Name;
                    info.Type = CharacterType.Player;
                    info.Class = (CharacterClass)c.Class;
                    // 这里希望Tid访问的是数据库的id
                    info.Tid = c.ID;
                    message.Response.userLogin.Userinfo.Player.Characters.Add(info);
                }

            }
            byte[] data = PackageHandler.PackMessage(message);
            sender.SendData(data, 0, data.Length);
        }
        //Response the request from the Client
        void OnRegister(NetConnection<NetSession> sender, UserRegisterRequest request)
        {
            //Printing What we are doing now in the Log
            Log.InfoFormat("UserRegisterRequest: User:{0}  Pass:{1}", request.User, request.Passward);

            //Prepare the message sending to Client
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.userRegister = new UserRegisterResponse();

            
            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user != null)
            {
                message.Response.userRegister.Result = Result.Failed;
                message.Response.userRegister.Errormsg = "用户已存在.";
            }
            else
            {
                TPlayer player = DBService.Instance.Entities.Players.Add(new TPlayer());
                DBService.Instance.Entities.Users.Add(new TUser() { Username = request.User, Password = request.Passward, Player = player });
                DBService.Instance.Entities.SaveChanges();
                message.Response.userRegister.Result = Result.Success;
                message.Response.userRegister.Errormsg = "None";
            }

            byte[] data = PackageHandler.PackMessage(message);
            sender.SendData(data, 0, data.Length);
        }
        //Response the request from the Client
        private void OnCreateCharacter(NetConnection<NetSession> sender, UserCreateCharacterRequest request)
        {
            // 打印日志
            Log.InfoFormat("UserCreateCharacterRequest: Name:{0}  Class:{1}", request.Name, request.Class);

            // 将客户端传输过来的数据创建成表
            TCharacter character = new TCharacter()
            {
                Name = request.Name,
                Class = (int)request.Class,
                TID = (int)request.Class,
                MapID = 1,
                MapPosX = 5000,
                MapPosY = 4000,
                MapPosZ = 820,
            };
            // 设置自己是背包的所有者
            var bag = new TCharacterBag();
            bag.Owner = character;
            bag.Items = new byte[0];
            bag.Unlocked = 20;
            DBService.Instance.Entities.CharacterBag.Add(bag);

            // 将创建好的表添加到数据库中
            DBService.Instance.Entities.Characters.Add(character);
            //角色表要关联到一个Player上
            sender.Session.User.Player.Characters.Add(character);
            DBService.Instance.Entities.SaveChanges();

            // 封装要返回给客户端的数据 
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.createChar = new UserCreateCharacterResponse();
            message.Response.createChar.Result = Result.Success;
            message.Response.createChar.Errormsg = "None";

            // 为什么加了这一段代码之后角色选择列表在创建完角色之后就可以实时显示了
            foreach(var c in sender.Session.User.Player.Characters)
            {
                NCharacterInfo info = new NCharacterInfo();
                info.Id = c.ID;
                info.Name = c.Name;
                info.Type = CharacterType.Player;
                info.Class = (CharacterClass)c.Class;
                // 这里希望Tid访问的是数据库的id
                info.Tid = c.ID;
                message.Response.createChar.Characters.Add(info);
            }
            // 将数据打包成字节流发送
            byte[] data = PackageHandler.PackMessage(message);
            sender.SendData(data, 0, data.Length);
           
        }
        //角色进入游戏的方法
        private void OnGameEnter(NetConnection<NetSession> sender, UserGameEnterRequest request)
        {
            //查找出相应的角色信息
            TCharacter dbchar = sender.Session.User.Player.Characters.ElementAt(request.characterIdx);
            //打印日志
            Log.InfoFormat("UserGameEnterRequest : character : {0} : {1}  Map : {2} ", dbchar.ID, dbchar.Name, dbchar.MapID);
            //使用查找出的角色信息 创建出相应的角色
            Character character = CharacterManager.Instance.AddCharascter(dbchar);

            //打包信息并发送
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.gameEnter = new UserGameEnterResponse();
            message.Response.gameEnter.Result = Result.Success;
            message.Response.gameEnter.Errormsg = "None";
            // 将角色信息打包返回
            message.Response.gameEnter.Character = character.Info;

            #region  测试当玩家进入游戏的时候有道具生成
            if(character.ItemManager.Items.Count == 0)
            {
                character.ItemManager.AddItem(1, 100);
                character.ItemManager.AddItem(2, 50);
                character.ItemManager.AddItem(3, 200);
                character.ItemManager.AddItem(4, 300);
                Log.Info("Item添加成功");
            }

            #endregion
            //使用PackageHandler将响应客户端的信息打包成字节流
            byte[] data = PackageHandler.PackMessage(message);
            //将响应信息发送出去
            sender.SendData(data, 0, data.Length);
            //更新临时会话中的角色信息
            sender.Session.Character = character;   

            MapManager.Instance[dbchar.MapID].CharacterEnter(sender, character);
        }
        //角色离开游戏的方法
        private void OnGameLeave(NetConnection<NetSession> sender, UserGameLeaveRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("OnGameLeave: characterID: {0} : {1} Map: {2}", character.Id, character.Info.Name, character.Info.mapId);
           
            CharacterLeave(character);

            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.gameLeave = new UserGameLeaveResponse();
            message.Response.gameLeave.Result = Result.Success;
            message.Response.gameLeave.Errormsg = "None";

            byte[] data = PackageHandler.PackMessage(message);
            sender.SendData(data, 0, data.Length);
        }
        //角色离开游戏引用角色离开游戏的方法
        public void CharacterLeave(Character character)
        {
            CharacterManager.Instance.RemoveCharacter(character.Id);
            MapManager.Instance[character.Info.mapId].CharacterLeave(character);
        }
    }
}
