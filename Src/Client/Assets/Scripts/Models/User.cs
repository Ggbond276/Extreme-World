using Common.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SkillBridge.Message;

namespace Models
{
    //临时数据存储器用于存储用户信息 如果用户信息不改变就不用再向服务器拉取信息
    //用于将服务器返回的用户信息记录到本地
    class User : Singleton<User>
    {
        NUserInfo userInfo;
        public NCharacterInfo CurrentCharacter { get; set; }
        public GameObject CurrentCharacterObject { get; set; }
        public MapDefine CurrentMapData { get; set; }
        public NUserInfo Info
        {
            get { return userInfo; }
        }
        public void SetupUserInfo(NUserInfo info)
        {
            this.userInfo = info;
        }
       
        /// <summary>
        /// 注册这两个委托可以分别监听金币和经验的变化
        /// </summary>
        public static event Action<long> OnGoldChanged;
        public static event Action<long> OnExpChanged;

        /// <summary>
        /// 调用这个方法可以增加金币
        /// </summary>
        /// <param name="value"></param>
        internal void AddGold(int value)
        {
            this.CurrentCharacter.Gold += value;
            OnGoldChanged?.Invoke(CurrentCharacter.Gold);
        }

        /// <summary>
        /// 调用这个方法可以增加经验
        /// </summary>
        /// <param name="value"></param>
        internal void AddExp(int value)
        {
            this.CurrentCharacter.Exp += value;
            OnExpChanged?.Invoke(CurrentCharacter.Exp);
        }
    }
}
