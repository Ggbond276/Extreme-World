using Common.Data;
using GameServer.Core;
using GameServer.Manager;
using SkillBridge.Message;

namespace GameServer.Entities
{
    /// <summary>
    /// 业务实体基类 — 在物理属性之上叠加"身份属性 + 图纸引用"。所有父类字段必须由本类构造函数一次性收口初始化。
    /// </summary>
    public class CharacterBase : Entity
    {
        public int Id { get; set; }                  // 数据库主键 ID（来源：构造参数）
        public string Name { get; set; }             // 显示名称（来源：构造参数；null 时由图纸默认名兜底）
        public int ConfigId { get; set; }            // 配置表 ID（指向 CharacterDefine 的键）
        public int Level { get; set; }               // 等级（来源：构造参数）
        public CharacterType Type { get; set; }      // 实体类型（玩家/怪物/NPC，来源：构造参数）
        public int MapId { get; set; }               // 当前所在地图 ID（来源：构造参数）
        public CharacterDefine Define;               // 图纸引用 — 静态配置快照（来源：DataManager.Characters[ConfigId]，构造期一次性查表）

        // ======================== 构造与初始化 ========================
        public CharacterBase(int id, string name, CharacterType type, int configId, int level, int mapId, Vector3Int pos, Vector3Int dir) : base(pos, dir)
        {
            this.Id = id;
            this.Type = type;
            this.ConfigId = configId;
            this.Level = level;
            this.MapId = mapId;
            this.Define = DataManager.Instance.Characters[this.ConfigId];
            this.Name = string.IsNullOrEmpty(name) ? this.Define.Name : name;
        }


        // ======================== 网络数据映射 ========================
        public virtual NCharacterInfo ToCharacterBaseInfo()         // 纯工厂：拼全部父类身份字段（Id/ConfigId/Name/Type/Level/MapId/EntityId/Entity），业务字段由子类补充
        {
            NCharacterInfo info = new NCharacterInfo();
            info.Id = this.Id;
            info.ConfigId = this.ConfigId;
            info.Name = this.Name;
            info.Type = this.Type;
            info.Level = this.Level;
            info.mapId = this.MapId;

            info.EntityId = this.entityId;
            info.Entity = this.ToNEntity();
            return info;
        }
    }
}
