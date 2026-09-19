using Models;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Data;

namespace Managers
{
    /// <summary>
    /// 客户端道具管理器（Model / Manager层）。
    /// 职责：负责在客户端内存中维护玩家拥有的所有道具数据，并监听服务器下发的道具状态同步指令（增加/扣除）。
    /// </summary>
    class ItemManager : Singleton<ItemManager> 
    {
        public Dictionary<int, Item> Items = new Dictionary<int, Item>(); // 核心数据池：以道具ID为键，存放当前玩家拥有的所有道具实体

        /// <summary>
        /// 玩家上线时的道具初始化操作。
        /// 执行后：清空旧缓存，将服务器下发的初始道具列表存入字典，并向状态同步中心注册【道具增删回调】。
        /// </summary>
        public void Init(List<NItemInfo> list)
        {
            this.Items.Clear(); // 登录初始化时先清空本地残留数据

            foreach (var info in list) // 遍历服务器下发的网络协议包数据
            {
                Items.Add(info.Id, new Item(info));
            }

            StatusService.Instance.RegisterStatusNotify(StatusType.Item, OnItemNotify); // 核心：向状态机订阅服务器的 Item 变化推送
        }

        // ========================================== [ 预留接口 ] ==========================================

        /// <summary>
        /// 获取指定道具的静态配置数据（外围系统查表接口）。
        /// 执行后：根据传入的道具 ID，去全局配置表中检索该道具的具体属性（如名称、图标、类型等）。当前预留占位，待后续业务接入。
        /// </summary>
        public ItemDefine GetItem(int itemId)
        {
            return null; // TODO: 预留的查表接口，尚未对接 DataManager.Instance.Items
        }

        /// <summary>
        /// 快捷使用道具（通过道具 ID 发起）。
        /// 执行后：校验玩家背包中是否拥有该道具，若满足条件则向服务器发送“使用道具”的网络请求。当前预留占位。
        /// </summary>
        public bool UseItem(int itemId)
        {
            return false; // TODO: 预留的道具使用入口，尚未对接网络发包协议
        }

        /// <summary>
        /// 完整使用道具（通过道具配置实体发起）。
        /// 执行后：解析传入的道具数据定义，执行具体的数值干预（如吃药回血、增加 BUFF）或触发特殊机制。当前预留占位。
        /// </summary>
        public bool UseItem(ItemDefine item)
        {
            return false; // TODO: 预留的具体生效逻辑，尚未实现数值层联动
        }

        /// <summary>
        /// 接收服务器下发的状态变更推送（核心同步逻辑）。
        /// 执行后：根据服务器下发的 Action（Add或Delete），将请求分发给对应的本地处理函数。
        /// </summary>
        private bool OnItemNotify(NStatus status)
        {
            if (status.Action == StatusAction.Add) // 分支 A：服务器通知新增道具
                this.AddItem(status.Id, status.Value); 

            if (status.Action == StatusAction.Delete) // 分支 B：服务器通知扣除道具
                this.DeleteItem(status.Id, status.Value);

            return true; // 消费掉该状态通知
        }

        /// <summary>
        /// 执行本地道具增加逻辑。
        /// 执行后：更新内存字典中的道具数量（若无则新建），并同步通知 UI 表现层的 BagManager 刷新背包。
        /// </summary>
        private void AddItem(int itemId, int count)
        {
            // ========================================== [ 1. 内存字典更新 ] ==========================================
            Item item = null; 
            if(this.Items.TryGetValue(itemId,out item)) // 判定：玩家原本是否已经拥有该道具
            {
                item.Count += count; // 已经拥有，直接叠加数量
            } else
            {
                item = new Item(itemId, count); // 原本没有，实例化新的道具对象
                this.Items.Add(itemId, item); // 加入内存字典
            }

            // ========================================== [ 2. 联动表现层刷新 ] ========================================

            // TODO: 这里应该使用事件委托 要不会导致道具系统和背包系统的耦合度过高
            BagManager.Instance.AddItem(itemId, count); // 数据层处理完毕，呼叫背包管理器将新增的道具渲染到 UI 格子里
        }


        /// <summary>
        /// 执行本地道具扣除逻辑。
        /// 执行后：检查数量是否充足，扣除内存字典中的数量，并同步通知 BagManager 刷新背包UI。
        /// </summary>
        private void DeleteItem(int itemId, int count)
        {
            // ========================================== [ 1. 容错与校验拦截 ] ========================================
            if (!this.Items.ContainsKey(itemId))
                return;
            Item item = this.Items[itemId]; // 提取道具引用
            if (item.Count < count) // 容错：本地道具数量不足以扣除（理论上服务器也会拦截，这里是双重保险），直接阻断
                return;

            // ========================================== [ 2. 内存字典更新 ] ==========================================

            // TODO: 这里还需要处理 count 归零时从字典移除的逻辑
            item.Count -= count; // 扣减内存中的道具数量（注意：这里通常还需要处理 count 归零时从字典移除的逻辑，可后续完善）

            // ========================================== [ 3. 联动表现层刷新 ] ========================================

            // TODO: 这里应该使用事件委托 要不会导致道具系统和背包系统的耦合度过高
            BagManager.Instance.RemoveItem(itemId, count);
        }
    }
}