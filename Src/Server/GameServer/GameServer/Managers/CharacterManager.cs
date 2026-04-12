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
        // 角色管理器使用字典来存储角色对象 查询效率较高 不需要做遍历即可查找
        // 这里利用的是EntityId作为键值
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
            //根据DB角色创建实体角色
            Character character = new Character(CharacterType.Player, cha);
            EntityManager.Instance.AddEntity(cha.MapID, character);
            character.Info.Id = character.Id;
            //这段代码在干什么
            this.Characters[character.Id] = character;
            //返回实体角色
            return character;
        }

        //删除角色
        public void RemoveCharacter(int characterId)
        {
            var cha = this.Characters[characterId];
            EntityManager.Instance.RemoveEntity(cha.Data.MapID, cha);
            // 删除角色管理器中的角色
            this.Characters.Remove(characterId);
        }
    }
}
