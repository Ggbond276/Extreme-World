using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Entities;

namespace Assets.Scripts.Managers
{
    class CharacterManager : Singleton<CharacterManager> , IDisposable
    {
        public Dictionary<int, Character> Characters = new Dictionary<int, Character>();
        // 这个委托会在GameObjectManager中
        public UnityAction<Character> OnCharacterEnter;

        public CharacterManager()
        {

        }

        public void  Dispose()
        {

        }

        public void Init()
        {

        }

        public void Clear()
        {
            this.Characters.Clear();
        }

        public void AddCharacter(NCharacterInfo cha)
        {
            // 打印所有的角色信息数据
            Debug.LogFormat("AddCharacter : {0} : {1} Map:{2} Entity:{3}", cha.Id, cha.Name, cha.mapId, cha.Entity.String());
            // 通过NCharacter中的信息 实例化Character变成一个Entity
            Character character = new Character(cha);
            this.Characters[cha.Id] = character;

            if(OnCharacterEnter!=null)
            {
                OnCharacterEnter(character);
            }
        }

    }
}
