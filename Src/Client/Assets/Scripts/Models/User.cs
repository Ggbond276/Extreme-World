using Common.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SkillBridge.Message;
using Entities;

namespace Models
{
    class User : Singleton<User>
    {
        // 用户信息
        private NUserInfo userInfo;
        // 角色信息 —— 改成 private set,所有写入必须经过 SetCurrentCharacter,杜绝野指针
        public Character CurrentCharacter { get; private set; }
        // 角色实体信息
        public GameObject CurrentCharacterObject { get; set; }
        // 当前地图配置信息
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
        /// 唯一入口:赋值 / 清空 CurrentCharacter。
        /// 所有模块(UserService / MapService / 后续可能的 GM / 切换角色)都必须走这里,
        /// 严禁直接 `User.Instance.CurrentCharacter = ...`。
        /// </summary>
        /// <param name="character">要写入的角色实体;传 null 表示清空(必须显式 allowNull)</param>
        /// <param name="source">调用方标签,用于 Console 溯源(例如 "UserService.OnGameEnter")</param>
        /// <param name="allowNull">仅在明确要清空(如 OnGameLeave)时置 true</param>
        public void SetCurrentCharacter(Character character, string source, bool allowNull = false)
        {
            if (character == null && !allowNull)
            {
                // 不允许变 null 时如果传 null:保留旧值并大声报错,这是典型的"被某条路径意外清空"事故信号
                Debug.LogErrorFormat(
                    "[User.SetCurrentCharacter] 拒绝把 CurrentCharacter 置空! source={0}, 旧值保留 EntityId={1} Name={2}",
                    source,
                    this.CurrentCharacter != null ? this.CurrentCharacter.entityId.ToString() : "null",
                    this.CurrentCharacter != null ? this.CurrentCharacter.Name : "null");
                return;
            }

            Character old = this.CurrentCharacter;
            this.CurrentCharacter = character;

            // 任何写入都打印来源 + 旧值/新值,出现"突然消失"时可以立刻在 Console 逆推上一条是谁写的
            Debug.LogFormat(
                "[User.SetCurrentCharacter] source={0} oldEntityId={1} -> newEntityId={2} newName={3}",
                source,
                old != null ? old.entityId.ToString() : "null",
                character != null ? character.entityId.ToString() : "null",
                character != null ? character.Name : "null");
        }

        public static event Action<long> OnGoldChanged;
        public static event Action<long> OnExpChanged;

        internal void AddGold(int value)
        {
            if (this.CurrentCharacter == null) return;
            this.CurrentCharacter.Gold += value;
            OnGoldChanged?.Invoke(this.CurrentCharacter.Gold);
        }

        internal void AddExp(int value)
        {
            if (this.CurrentCharacter == null) return;
            this.CurrentCharacter.Exp += value;
            OnExpChanged?.Invoke(this.CurrentCharacter.Exp);
        }
    }
}
