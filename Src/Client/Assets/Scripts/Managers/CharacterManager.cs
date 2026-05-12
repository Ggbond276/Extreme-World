using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Entities;
using Managers;

namespace Assets.Scripts.Managers
{
    class CharacterManager : Singleton<CharacterManager>, IDisposable
    {
        public Dictionary<int, Character> Characters = new Dictionary<int, Character>();
        // 这个委托会在GameObjectManager中
        public UnityAction<Character> OnCharacterEnter;
        public UnityAction<Character> OnCharacterLeave;
        public CharacterManager()
        {

        }

        public void Dispose()
        {

        }

        public void Init()
        {

        }

        //OnMapCharacterLeave调用
        public void Clear()
        {
            // 注释掉的逻辑是我自己思考写出来的
            //if (this.OnCharacterLeave != null)
            //{
            //    foreach (var cha in this.Characters.Values)
            //    {
            //        this.OnCharacterLeave(cha);
            //    }
            //}
            //Debug.LogFormat("this.Characters.Clear()");

            int[] keys = this.Characters.Keys.ToArray();
            foreach(var key in  keys)
            {
                this.RemoveCharacter(key);
            }
            this.Characters.Clear();
        }

        public void AddCharacter(NCharacterInfo cha)
        {
            // 打印角色的所有信息数据
            Debug.LogFormat("AddCharacter : CharacterID:{0} CharacterName:{1} MapID:{2} Entity:{3}", cha.Id, cha.Name, cha.mapId, cha.Entity.String());
            // 通过NCharacter中的信息 实例化Character变成一个Entity
            Character character = new Character(cha);
            // 统一用 entityId 作为字典的 key（与服务端和 EntityManager 保持一致）
            this.Characters[character.entityId] = character;
            EntityManager.Instance.AddEntity(character);
            if (OnCharacterEnter != null)
            {
                OnCharacterEnter(character);
            }
        }

        //OnMapCharacterLeave调用（参数 entityId 由服务端发来）
        public void RemoveCharacter(int entityId)
        {
            if (Characters.ContainsKey(entityId))
            {
                EntityManager.Instance.RemoveEntity(this.Characters[entityId].Info.Entity);
                if (OnCharacterLeave != null)
                {
                    this.OnCharacterLeave(Characters[entityId]);
                }
                this.Characters.Remove(entityId);
            }
        }
    }
}
