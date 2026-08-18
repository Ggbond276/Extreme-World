using Common;
using GameServer.Entities;
using GameServer.Manager;
using GameServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{



    /// <summary>
    /// 全局轻量级角色摘要数据 (数据瘦身)
    /// </summary>
    public class CharacterInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Class { get; set; }
        public int Level { get; set; }
    }

    class CharacterInfoManager : Singleton<CharacterInfoManager>
    {

        // 核心户籍字典：全服所有建过角的玩家都在这里 (O(1) 极速查询)
        private Dictionary<int, CharacterInfo> _characters = new Dictionary<int, CharacterInfo>();


        /// <summary>
        /// 1. 系统初始化 (必须在 DBService 之后，社交系统之前调用)
        /// </summary>
        public void Init()
        {
            _characters.Clear();

            // 性能优化核心：绝不拉取全表，只 Select 需要的 4 个字段
            // 注意：如果你的 Entity Framework 集合不叫 Characters，请替换为实际名称 (例如 TCharacterSet)
            var allDbChars = DBService.Instance.Entities.Characters
                .Select(c => new { c.ID, c.Name, c.Class, c.Level })
                .ToList();

            foreach (var dbChar in allDbChars)
            {
                _characters[dbChar.ID] = new CharacterInfo()
                {
                    Id = dbChar.ID,
                    Name = dbChar.Name,
                    Class = dbChar.Class,
                    Level = dbChar.Level
                };
            }

            Log.InfoFormat("CharacterInfoManager: 户籍大管家初始化完成，共载入 {0} 条玩家档案。", _characters.Count);
        }


        /// <summary>
        /// 核心提货接口：在线拿活数据，离线拿快照数据
        /// </summary>
        public CharacterInfo GetCharacterInfo(int characterId)
        {
            // 1. 拦截！先去问在线大管家要人。
            // 只要他在线，在线对象里的数据【绝对是最新的】，直接当场拼一个名片给他！
            Character onlineChar = CharacterManager.Instance.GetCharacter(characterId);
            if (onlineChar != null)
            {
                return new CharacterInfo()
                {
                    Id = onlineChar.Id,
                    Name = onlineChar.Data.Name,
                    Class = onlineChar.Data.Class,
                    Level = onlineChar.Data.Level
                };
            }

            //  2. 降级！如果不在线，再去户籍字典里查快照。
            // 反正他离线了，快照数据肯定就是最终数据。
            if (_characters.TryGetValue(characterId, out CharacterInfo info))
            {
                return info;
            }

            return null; // 查无此人
        }



        /// <summary>
        /// 离线快照：仅在玩家下线/移除时调用一次，覆盖更新
        /// </summary>
        public void SyncOfflineInfo(Character character)
        {
            if (character == null) return;

            // 玩家下线了，把他的最终等级、名字更新到离线字典里
            _characters[character.Id] = new CharacterInfo()
            {
                Id = character.Id,
                Name = character.Data.Name,
                Class = character.Data.Class,
                Level = character.Data.Level
            };
        }


    }
}
