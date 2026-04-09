using Assets.Scripts.Managers;
using Common.Data;
using Models;
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
    class MapService : Singleton<MapService>, IDisposable
    {
        public int CurrentMapId { get; set; }
        //构造的时候执行
        public MapService()
        {
            #region 注册协议 接收服务端发送来的响应信息
            MessageDistributer.Instance.Subscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Subscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            #endregion
        }
        //销毁的时候执行
        public void Dispose()
        {
            #region 销毁协议 不再接收服务端发送来的响应信息
            MessageDistributer.Instance.Unsubscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Unsubscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            #endregion
        }
        public void Init()
        {

        }
        // 处理服务器发来的角色进入地图的消息
        private void OnMapCharacterEnter(object sender, MapCharacterEnterResponse response)
        {
            
            // 将response中的信息打印出来
            Debug.LogFormat("OnMapCharacterEnter : {0} Count : {1}", response.mapId, response.Characters.Count);
           
            //1.如果发现角色准备进入的地图的id和当前所在的地图的id不一样 那就加载新的地图资源
            if(CurrentMapId != response.mapId)
            {
                CharacterManager.Instance.Clear();
                // 调用EnterMap方法进入新的地图
                this.EnterMap(response.mapId);
                // 更新当前的地图id
                this.CurrentMapId = response.mapId;
            }
            //2.把角色丢给角色管理器
            foreach( var cha in response.Characters)
            {
                // 如果发现名单里的某个人 ID 跟我自己一样，说明这是服务器发来的最新的我的数据（可能血量变了，或者装备变了），赶紧更新一下本地的自己。
                if (User.Instance.CurrentCharacter.Id == cha.Id)
                {
                    //重新赋值
                    User.Instance.CurrentCharacter = cha;
                }
                // 把所有的角色全部都丢给角色管理器去处理
                CharacterManager.Instance.AddCharacter(cha);
            } 
        }
        // 处理服务器发来的角色离开地图的消息
        private void OnMapCharacterLeave(object sender, MapCharacterLeaveResponse response)
        {
            // 接收到消息了
            Debug.LogFormat("OnMapCharacterLeave: {0}", response.characterId);
            // 如果是要离开的角色不是玩家角色 就清除当前角色即可
            if(response.characterId != User.Instance.CurrentCharacter.Entity.Id)
            {
                Debug.LogFormat("CharacterManager.Instance.RemoveCharacter()");
                // 这里要传入的是EnityId
                CharacterManager.Instance.RemoveCharacter(response.characterId);
            } else
            {
                Debug.LogFormat("CharacterManager.Instance.Clear()");
                CharacterManager.Instance.Clear();
            }
        }
        private void EnterMap(int mapId)
        {
            //根据MapId 使用DataManager拿到关于地图的信息
            //判断地图是否存在
            if (DataManager.Instance.Maps.ContainsKey(mapId))
            {   
                //使用DataManager获取地图的定义 地图的定义中包含地图的资源
                MapDefine map = DataManager.Instance.Maps[mapId];
               
                User.Instance.CurrentMapData = map;
                //加载地图资源
                //map.Resource可以拿到资源路径
                //然后调用SceneManager.Instance.LoadScene这个方法就可以让 Unity 引擎去自己的 Assets 资源库里寻找名为 Scene_StartCity 的真实场景文件（包含地形、模型、光源等）。
                SceneManager.Instance.LoadScene(map.Resource);
            }
            else
            {
                Debug.LogErrorFormat("EnterMap : Map {0} not existed", mapId);
            }
        }
    }
}
