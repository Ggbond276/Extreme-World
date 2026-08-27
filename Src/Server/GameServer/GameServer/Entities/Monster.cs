using GameServer.Core;

namespace GameServer.Entities
{
    /// <summary>
    /// 怪物实体 — 仅持有物理属性 + 基础身份字段，零业务 Manager。
    /// </summary>
    public class Monster : CharacterBase
    {
        // ======================== 构造与初始化 ========================
        public Monster(int configId, int level, Vector3Int pos, Vector3Int dir) : base(id: 0, name: null, type: CharacterType.Monster, configId: configId, level: level, mapId: 0, pos: pos, dir: dir) // configId:来源 SpawnPoint.Define.SpawnMonID / level:来源 SpawnPoint.Define.SpawnLevel / pos:来源 SpawnPoint.Position / dir:来源 SpawnPoint.Direction
        {
        }
    }
}
