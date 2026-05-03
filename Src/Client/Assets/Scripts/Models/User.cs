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
        // 谁来注册这个委托就可以实时监听到金币的变化
        public static event Action<long> OnGoldChanged;
        public static event Action<long> OnExpChanged;
        internal void AddGold(int value)
        {
            this.CurrentCharacter.Gold += value;
            OnGoldChanged?.Invoke(CurrentCharacter.Gold);
        }

        internal void AddExp(int value)
        {
            this.CurrentCharacter.Exp += value;
            OnExpChanged?.Invoke(CurrentCharacter.Exp);
        }
    }
}
