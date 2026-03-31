using Common.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Models
{
    //临时数据存储器用于存储用户信息 如果用户信息不改变就不用再向服务器拉取信息
    //用于将服务器返回的用户信息记录到本地
    class User : Singleton<User>
    {
        SkillBridge.Message.NUserInfo userInfo;
        public SkillBridge.Message.NUserInfo Info
        {
            get { return userInfo; }
        }
        public void SetupUserInfo(SkillBridge.Message.NUserInfo info)
        {
            this.userInfo = info;
        }

        //  当前角色的内在信息
        public SkillBridge.Message.NCharacterInfo CurrentCharacter { get; set; }
        // 当前角色的模型
        public GameObject CurrentCharacterObject { get; set; }
        // 当前角色所在的地图
        public MapDefine CurrentMapData { get; set; }
     
    }
}
