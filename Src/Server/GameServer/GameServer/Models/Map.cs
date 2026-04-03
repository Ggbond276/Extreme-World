using System.Collections.Generic;
using SkillBridge.Message;

using Common;
using Common.Data;

using Network;
using GameServer.Entities;

//Map类提供了角色进入地图的方法
namespace GameServer.Models
{
    class Map
    {
        //MapCharacter 内部类
        internal class MapCharacter
        {
            //内部类成员变量
            //处理网络连接的对象
            public NetConnection<NetSession> connection;
            //角色对象 存储了角色的相关信息
            public Character character;
            //内部类构造方法   
            public MapCharacter(NetConnection<NetSession> conn, Character cha)
            {
                this.connection = conn;
                this.character = cha;
            }
        }

        //地图ID
        public int ID
        {
            get { return this.Define.ID; }
        }

        //MapDefine 是配置表
        internal MapDefine Define;

        //用于存储地图上角色的字典
        Dictionary<int, MapCharacter> MapCharacters = new Dictionary<int, MapCharacter>();

        //构造方法初始化Map对象
        internal Map(MapDefine define)
        {
            this.Define = define;
        }

        internal void Update()
        {

        }
        internal void CharacterEnter(NetConnection<NetSession> conn, Character character)
        {
            //打印日志
            Log.InfoFormat("CharacterEnter: Map:{0} characterId:{1}", this.Define.ID, character.Id);
            //给网络数据MapID赋值
            character.Info.mapId = this.ID;

            #region 打包响应式信息
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.mapCharacterEnter = new MapCharacterEnterResponse();
            message.Response.mapCharacterEnter.mapId = this.Define.ID;
            //将角色信息添加到集合中 以便通知其他客户端该角色的信息
            message.Response.mapCharacterEnter.Characters.Add(character.Info);
            #endregion


            foreach (var kv in this.MapCharacters)
            {
                //将角色信息添加到Characters集合中
                message.Response.mapCharacterEnter.Characters.Add(kv.Value.character.Info);
                //将进入的角色的信息发送给其他角色 实现角色进入的广播功能
                this.SendCharacterEnterMap(kv.Value.connection, character.Info);
            }

            //将新进入的角色和连接信息存储在MapCharacaters字典中
            this.MapCharacters[character.Id] = new MapCharacter(conn, character);

            //将信息打包成字节流
            byte[] data = PackageHandler.PackMessage(message);
            //发送信息
            conn.SendData(data, 0, data.Length);
        }
        void SendCharacterEnterMap(NetConnection<NetSession> conn, NCharacterInfo character)
        {
            #region 打包响应式信息
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.mapCharacterEnter = new MapCharacterEnterResponse();
            message.Response.mapCharacterEnter.mapId = this.Define.ID;
            message.Response.mapCharacterEnter.Characters.Add(character);
            #endregion

            //将信息打包成字节流
            byte[] data = PackageHandler.PackMessage(message);
            //发送信息
            conn.SendData(data, 0, data.Length);
        }

        internal void CharacterLeave(NCharacterInfo cha)
        {
            // 执行到这条语句出现了问题
            Log.InfoFormat("CharacterLeaveMap: Map : {0} characterId: {1}", this.Define.ID, cha.Id); 
            this.MapCharacters.Remove(cha.Id);
            foreach( var kv in this.MapCharacters)
            {
                // 发送到服务器告诉他们哪个角色离开了
                this.SendCharacterLeaveMap(kv.Value.connection, cha);
            }
        }


        void SendCharacterLeaveMap(NetConnection<NetSession> conn, NCharacterInfo character)
        {
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.mapCharacterLeave = new MapCharacterLeaveResponse();
            message.Response.mapCharacterLeave.characterId = character.Id;

            byte[] data = PackageHandler.PackMessage(message);
            conn.SendData(data, 0, data.Length);
        }


        internal void UpdateEntity(NEntitySync entity)
        {
            foreach(var kv in this.MapCharacters)
            {
                if(kv.Value.character.entityId == entity.Id)
                {
                    kv.Value.character.Position = entity.Entity.Position;
                    kv.Value.character.Direction = entity.Entity.Direction;
                    kv.Value.character.Speed = entity.Entity.Speed;
                }
                else
                {
                    this.SendEntityUpdate(kv.Value.connection, entity);
                }
            }
        }
        void SendEntityUpdate(NetConnection<NetSession> conn , NEntitySync entity)
        {
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.mapEntitySync = new MapEntitySyncResponse();
            message.Response.mapEntitySync.entitySyncs.Add(entity);

            byte[] data = PackageHandler.PackMessage(message);
            conn.SendData(data, 0, data.Length);

        }


    }
}
