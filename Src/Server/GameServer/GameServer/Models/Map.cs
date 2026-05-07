using System.Collections.Generic;
using SkillBridge.Message;

using Common;
using Common.Data;

using Network;
using GameServer.Entities;
using GameServer.Services;
using GameServer.Managers;

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

        //用于存储地图上角色的字典(这里使用的是Entity.Id作为键值存放)
        Dictionary<int, MapCharacter> MapCharacters = new Dictionary<int, MapCharacter>();

        // 刷怪管理器
        SpawnManager SpawnManager = new SpawnManager();
        // 怪物管理器
        public MonsterManager MonsterManager = new MonsterManager();

        //构造方法初始化Map对象
        internal Map(MapDefine define)
        {
            this.Define = define;
            this.SpawnManager.Init(this);
            this.MonsterManager.Init(this);
        }

        internal void Update()
        {
            SpawnManager.Update();
        }
        // 玩家进入地图
        internal void CharacterEnter(NetConnection<NetSession> conn, Character character)
        {
            //打印日志
            Log.InfoFormat("CharacterEnter: Map:{0} characterId:{1}", this.Define.ID, character.Id);
            //给网络数据MapID赋值
            character.Info.mapId = this.ID;
            //将新进入的角色和连接信息存储在MapCharacaters字典中(这里使用的是Entity.Id作为键值存放)
            this.MapCharacters[character.Id] = new MapCharacter(conn, character);

            conn.Session.Response.mapCharacterEnter = new MapCharacterEnterResponse();
            conn.Session.Response.mapCharacterEnter.mapId = this.Define.ID;

            // 玩家进入地图 将地图中的角色全部打包扔给客户端
            foreach (var kv in this.MapCharacters)
            {
                conn.Session.Response.mapCharacterEnter.Characters.Add(kv.Value.character.Info);
                if (kv.Value.character != character)
                    this.SendCharacterEnterMap(kv.Value.connection, character.Info);
            }
            // 玩家进入地图 将地图中的怪物全部打包扔给客户端
            foreach(var kv in  this.MonsterManager.Monsters)
            {
                conn.Session.Response.mapCharacterEnter.Characters.Add(kv.Value.Info);
            }
            conn.SendResponse();

        }
        void SendCharacterEnterMap(NetConnection<NetSession> conn, NCharacterInfo character)
        {
            
            if(conn.Session.Response.mapCharacterEnter == null)
            {
                conn.Session.Response.mapCharacterEnter = new MapCharacterEnterResponse();
                conn.Session.Response.mapCharacterEnter.mapId = this.Define.ID;
            }
            conn.Session.Response.mapCharacterEnter.Characters.Add(character);
            
            // 为了新能这里的SendResponse以后是可以删除掉的 好几个系统的协议 可能在一次消息的同步发送出去
            // 减少发包的次数 提高性能
            conn.SendResponse();
        }

        // 玩家离开地图
        internal void CharacterLeave(Character character)
        {
            Log.InfoFormat("CharacterLeaveMap: Map : {0} characterId: {1}", this.Define.ID, character.Id);
            foreach( var kv in this.MapCharacters)
            {
                // 发送到服务器告诉他们哪个角色离开了
                this.SendCharacterLeaveMap(kv.Value.connection, character);
            }
            // 删除地图管理器中的角色(这里的bug是因为Id弄错了） 所以修改Remove(cha.Id) --> Remove(cha.Entity.Id)
            this.MapCharacters.Remove(character.Id);
        }
        void SendCharacterLeaveMap(NetConnection<NetSession> conn, Character character)
        {
            conn.Session.Response.mapCharacterLeave = new MapCharacterLeaveResponse();
            conn.Session.Response.mapCharacterLeave.characterId = character.Id;
            conn.SendResponse();

        }

        // 玩家移动同步
        internal void UpdateEntity(NEntitySync entity)
        {
           foreach(var kv in MapCharacters)
            {
                if(kv.Value.character.entityId == entity.Id)
                {
                    kv.Value.character.Position = entity.Entity.Position;
                    kv.Value.character.Direction = entity.Entity.Direction;
                    kv.Value.character.Speed = entity.Entity.Speed;
                } else
                {
                    MapService.Instance.SendEntityUpdate(kv.Value.connection, entity);
                }

            }
        }

        // 怪物刷新的时候 要通知给客户端 怪物刷新啦
        internal void MonsterEnter(Monster monster)
        {
            Log.InfoFormat("MonsterEnter: Map:{0} monsterId:{1}", this.Define.ID, monster.Id);
            foreach(var kv in this.MapCharacters)
            {
                this.SendCharacterEnterMap(kv.Value.connection, monster.Info);
            }
        }
     
    }
}
