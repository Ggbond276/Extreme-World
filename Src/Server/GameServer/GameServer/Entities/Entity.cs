using GameServer.Core;
using SkillBridge.Message;

namespace GameServer.Entities
{
    /// <summary>
    /// 领域实体基类 — 持有纯物理属性（位置/方向/速度），零业务零网络依赖。
    /// </summary>
    public class Entity
    {
        public int entityId;                    // 全局唯一实体ID（运行时由 EntityManager 分配，内存动态维护）
        private Vector3Int position;           // 世界坐标（运行时动态变化，来源于移动指令/传送/同步）
        private Vector3Int direction;          // 朝向向量（运行时动态变化，来源于移动指令）
        private int speed;                     // 当前移动速度（运行时动态变化，来源于移动状态机）

        // ======================== 数据访问 ========================
        public Vector3Int Position { get { return position; } set { position = value; } }   // 世界坐标
        public Vector3Int Direction { get { return direction; } set { direction = value; } } // 朝向向量
        public int Speed { get { return speed; } set { speed = value; } }     // 当前速度

        // ======================== 构造与初始化 ========================
        public Entity(Vector3Int pos, Vector3Int dir)          // pos:初始位置 / dir:初始朝向（来源：数据库或出生点）
        {
            this.position = pos;
            this.direction = dir;
            this.speed = 0;
        }

        // ======================== 网络数据映射 ========================
        public virtual NEntity ToNEntity()                     // 纯工厂：每次 new 新实例，打包当前物理快照
        {
            NEntity nEntity = new NEntity();
            nEntity.Id = this.entityId;
            nEntity.Position = this.position;
            nEntity.Direction = this.direction;
            nEntity.Speed = this.speed;
            return nEntity;
        }
    }
}
