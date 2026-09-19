using Assets.Scripts.Managers;
using Common.Data;
using Managers;
using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Entities;

namespace Assets.Scripts.Services
{
    /// <summary>
    /// 地图服务类 (MapService)
    /// 职责：负责处理与地图相关的网络协议、场景加载切换、以及实体（角色/怪物）的同步管理。
    /// </summary>
    class MapService : Singleton<MapService>, IDisposable
    {
        /// <summary>
        /// 记录当前玩家所在的地图 ID
        /// </summary>
        public int CurrentMapId { get; set; }

        /// <summary>
        /// 构造函数：初始化时订阅相关的网络消息。
        /// 监听：角色进入、角色离开、实体同步（移动等）。
        /// </summary>
        public MapService()
        {
            MessageDistributer.Instance.Subscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Subscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            MessageDistributer.Instance.Subscribe<MapEntitySyncResponse>(this.OnMapEntitySync);
        }

        /// <summary>
        /// 销毁处理：取消消息订阅，防止对象销毁后的空指针引用或内存泄漏。
        /// </summary>
        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Unsubscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            MessageDistributer.Instance.Unsubscribe<MapEntitySyncResponse>(this.OnMapEntitySync);
        }

        public void Init()
        {
            // 初始化逻辑预留
        }

        /// <summary>
        /// 核心方法：执行进入地图的逻辑。
        /// 主要职责：更新本地数据中心 (User.Instance)，并通知 Unity 引擎加载对应的 3D 场景资源。
        /// </summary>
        /// <param name="mapId">目标地图的配置 ID</param>
        private void EnterMap(int mapId)
        {
            if (DataManager.Instance.Maps.ContainsKey(mapId))
            {
                MapDefine map = DataManager.Instance.Maps[mapId];

                // 同步当前玩家所在的地图静态配置数据
                User.Instance.CurrentMapData = map;

                // 核心：调用场景管理器加载地图对应的场景文件 (Scene_XXX)
                SceneManager.Instance.LoadScene(map.Resource);
            }
            else
            {
                Debug.LogErrorFormat("EnterMap : Map {0} not existed", mapId);
            }
        }

        /// <summary>
        /// 网络响应：处理服务器发来的“角色进入地图”消息。
        /// 逻辑流：1. 更新本地玩家数据 -> 2. 通知角色管理器生成实体 -> 3. 判断是否需要切换 Unity 场景。
        /// 调试重点：如果怪物或玩家进图后没显示，检查这里的 response.Characters 列表。
        /// </summary>
        private void OnMapCharacterEnter(object sender, MapCharacterEnterResponse response)
        {
            Debug.LogFormat("OnMapCharacterEnter : MapID:{0} CharactersCount:{1}", response.mapId, response.Characters.Count);

            foreach (var cha in response.Characters)
            {
                if (cha.Type == CharacterType.Player
                    && User.Instance.CurrentCharacter != null
                    && User.Instance.CurrentCharacter.entityId == cha.EntityId)
                {
                    // 必须先 AddCharacter 把 cha 注册到字典里,再 GetCharacter 回查
                    // 否则字典里查不到,会让 CurrentCharacter 变 null,引发 UIMain.UpdateAvatar() 空引用
                    CharacterManager.Instance.AddCharacter(cha);
                    Character newCurrent = CharacterManager.Instance.GetCharacter(cha.EntityId);
                    if (newCurrent != null)
                    {
                        // 唯一入口赋值,带溯源 + null 保护
                        User.Instance.SetCurrentCharacter(newCurrent, "MapService.OnMapCharacterEnter(player-self)");
                    }
                    else
                    {
                        // 容错:即使没查到也不能把已有 CurrentCharacter 覆盖成 null
                        Debug.LogErrorFormat("OnMapCharacterEnter: AddCharacter 后 GetCharacter 仍返回 null,EntityId={0}", cha.EntityId);
                    }
                    continue;
                }

                CharacterManager.Instance.AddCharacter(cha);
            }

            if (CurrentMapId != response.mapId)
            {
                this.EnterMap(response.mapId);
                this.CurrentMapId = response.mapId;
            }
        }

        private void OnMapCharacterLeave(object sender, MapCharacterLeaveResponse response)
        {
            Debug.LogFormat("OnMapCharacterLeave: {0}", response.characterId);

            Character current = User.Instance.CurrentCharacter;

            // 场景 1:玩家当前没有任何 Character 引用(异常态),无论谁离开都安全清场
            // 场景 2:离开的角色就是玩家自己(回城 / 切场景) -> 清空整张地图的角色
            if (current == null)
            {
                Debug.LogWarningFormat("[MapService.OnMapCharacterLeave] CurrentCharacter 为空,直接 Clear。响应 characterId={0}", response.characterId);
                CharacterManager.Instance.Clear();
                return;
            }

            if (response.characterId == current.entityId)
            {
                Debug.LogFormat("[MapService.OnMapCharacterLeave] 玩家自己离开地图,Clear 全部角色");
                CharacterManager.Instance.Clear();
            }
            else
            {
                // 场景 3:别的角色离开 -> 只从管理器里移除这一条
                CharacterManager.Instance.RemoveCharacter(response.characterId);
            }
        }

        /// <summary>
        /// 客户端请求：向服务器发送本地角色的位置同步请求。
        /// 触发时机：当本地玩家按下移动键或发生位移状态改变时调用。
        /// </summary>
        /// <param name="entityEvent">事件类型 (如：Idle, Move)</param>
        /// <param name="entity">最新的位置、方向、速度等数据</param>
        public void SendMapEntitySync(EntityEvent entityEvent, NEntity entity)
        {
            Debug.LogFormat("MapEntityUpdateRequest :EntityID {0} POS: {1} DIR: {2} SPD: {3} ", entity.Id, entity.Position.String(), entity.Direction.String(), entity.Speed);

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.mapEntitySync = new MapEntitySyncRequest();
            message.Request.mapEntitySync.entitySync = new NEntitySync()
            {
                Id = entity.Id,
                Event = entityEvent,
                Entity = entity
            };
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 网络响应：处理服务器广播回来的“所有实体同步”消息。
        /// 职责：将收到的怪物、其他玩家的位置更新分发给 EntityManager 处理。
        /// 调试重点：如果怪物平移丝滑度有问题，查这里的推送逻辑。
        /// </summary>
        public void OnMapEntitySync(object sender, MapEntitySyncResponse response)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("MapEntityUpdateResponse: Entity:{0}", response.entitySyncs.Count);
            sb.AppendLine();

            foreach (var entity in response.entitySyncs)
            {
                // 通知实体管理器去平滑更新对应的渲染层模型
                EntityManager.Instance.OnEntitySync(entity);

                sb.AppendFormat("[ {0} ] evt : {1}  entity : {2}", entity.Id, entity.Event, entity.Entity.ToString());
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 客户端请求：向服务器发送“使用传送门”的请求。
        /// </summary>
        /// <param name="teleporterID">传送门在配置表中的 ID</param>
        internal void SendMapTeleport(int teleporterID)
        {
            Debug.LogFormat("MapTeleportRequest : teleporterID: {0}", teleporterID);

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.mapTeleport = new MapTeleportRequest();
            message.Request.mapTeleport.teleporterId = teleporterID;

            NetClient.Instance.SendMessage(message);
        }
    }
}