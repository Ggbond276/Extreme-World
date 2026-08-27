using Common.Data;
using GameServer.Core;
using GameServer.Manager;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entities
{
    class CharacterBase : Entity
    {
        // ============ 【独立业务属性】纯领域字段 ============
        public int Id { get; set; }
        public string Name { get; set; }
        public int ConfigId { get; set; }
        public int Level { get; set; }
        public CharacterType Type { get; set; }
        public int MapId { get; set; }

        // EntityId 由基类 Entity.entityId 提供，无需重复声明

        // ============ 【图纸】独立引用 ============
        public CharacterDefine Define;

        public CharacterBase(Vector3Int pos, Vector3Int dir) : base(pos, dir)
        {
        }

        public CharacterBase(CharacterType type, int configId, int level, Vector3Int pos, Vector3Int dir) : base(pos, dir)
        {
            this.ConfigId = configId;
            this.Type = type;
            this.Level = level;

            // 【图纸】独立查表
            this.Define = DataManager.Instance.Characters[this.ConfigId];
            this.Name = this.Define.Name;
        }

        /// <summary>
        /// 将当前实体的所有字段打包为一个全新的 NCharacterInfo 网络快照并返回。
        /// 物理属性通过 ToNEntity() 实时生成，业务字段从独立属性读取。
        /// 每次调用都生成全新实例，保证发出去的是最新状态（纯工厂方法）。
        /// </summary>
        public virtual NCharacterInfo ToCharacterInfo()
        {
            NCharacterInfo info = new NCharacterInfo();
            info.EntityId = this.entityId;
            info.mapId = this.MapId;
            info.Entity = this.ToNEntity();
            return info;
        }
    }
}