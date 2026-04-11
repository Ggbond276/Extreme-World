using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkillBridge.Message;
using GameServer.Entities;
using Common;
using GameServer.Models;

namespace GameServer.Manager
{
   
    class MapManager : Singleton<MapManager>
    {
        //用于存放地图对象的字典容
        //Maps是在Init的过程中从表中读出来的
        Dictionary<int, Map> Maps = new Dictionary<int, Map>();

        public void Init()
        {
            //遍历地图值
             foreach (var mapdefine in DataManager.Instance.Maps.Values)
            {
                //根据数据库中的地图值 创建对应的地图对象
                Map map = new Map(mapdefine);
                //打印日志
                Log.InfoFormat("MapManager.Init > Map: {0} : {1}", map.Define.ID, map.Define.Name);
                //根据键值将创建好的地图对象放置在容器中
                this.Maps[mapdefine.ID] = map;
            }
        }

        //这是C#中的索引器 可以使用索引器来访问字典中的对象
        public Map this[int key]
        {
            get
            {
                return this.Maps[key];
            }
        }

        //大部分管理器是响应式的不存在自主服务 所以没有Update函数 但是地图管理器是有自主服务的
        public void Update()
        {
            foreach (var map in this.Maps.Values)
            {
                map.Update();
            }
        }

    }
}
