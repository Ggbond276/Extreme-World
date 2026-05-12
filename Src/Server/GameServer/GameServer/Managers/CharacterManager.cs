using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkillBridge.Message;
using GameServer.Entities;
using Common;
using GameServer.Managers;

namespace GameServer.Manager
{
    //为什么这里要继承这个接口
    class CharacterManager : Singleton<CharacterManager>
    {
        /// <summary>
        /// 角色管理器使用EntityId
        /// </summary>
        public Dictionary<int, Character> Characters = new Dictionary<int, Character>();

        public CharacterManager()
        {

        }

        public void Dispose()
        {

        }

        public void Init()
        {

        }

        public void Clear()
        {
            this.Characters.Clear();
        }

        //添加角色
        public Character AddCharascter(TCharacter cha)
        {
            // 创建内存角色数据(这个时候还没有EntityID)
            Character character = new Character(CharacterType.Player, cha);
            // 将角色添加到Entity管理器中(添加到管理器之后才有EntityID)
            EntityManager.Instance.AddEntity(cha.MapID, character);
            // 网络数据也需要更新EntityID
            character.Info.EntityId = character.entityId;

            // 将角色添加到Character管理器中
            this.Characters[character.entityId] = character;
            return character;
        }

        //删除角色
        public void RemoveCharacter(int characterId)
        {
            // 获得内存角色数据
            var cha = this.Characters[characterId];
            // 将角色数据从Entity管理器中移除
            EntityManager.Instance.RemoveEntity(cha.Data.MapID, cha);
            // 将角色数据从Character管理器中移除
            this.Characters.Remove(characterId);
        }
    }
}
