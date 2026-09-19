using Common.Data;
using SkillBridge.Message;
using UnityEngine;

namespace Entities
{
    /// <summary>
    /// 业务实体基类 — 在物理属性之上叠加"身份属性 + 图纸引用"。
    /// 所有身份字段必须由本类构造函数一次性收口初始化，子类禁止越权赋值。
    /// </summary>
    public class CharacterBase : Entity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ConfigId { get; set; }
        public int Level { get; set; }
        public CharacterType Type { get; set; }
        public int MapId { get; set; }
        public CharacterDefine Define;

        protected CharacterBase(Vector3Int pos, Vector3Int dir) : base(pos, dir) { }
    }
}
