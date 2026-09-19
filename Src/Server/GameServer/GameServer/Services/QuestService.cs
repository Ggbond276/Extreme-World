using Common;
using GameServer.Entities;
using GameServer.Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
    /// <summary>
    /// 服务器任务网络服务层（Service层）。
    /// 职责：作为网络消息的分发与响应入口，专门负责拦截客户端发来的任务请求，转交给 Manager 处理后，将结果打包返回给客户端。
    /// </summary>
    class QuestService : Singleton<QuestService>
    {
        public QuestService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<QuestAcceptRequest>(this.OnQuestAccept);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<QuestSubmitRequest>(this.OnQuestSubmit);
        }

        public void Init()
        {
            // 预留的初始化接口，供全局 Server 启动时按需调用
        }

        /// <summary>
        /// 接收并处理客户端发起的【接取任务】请求。
        /// 执行后：调用具体玩家内存里的 QuestManager 进行业务校验。成功则下发包含新任务状态的封包；失败则下发附带错误信息的封包。
        /// </summary>
        public void OnQuestAccept(NetConnection<NetSession> sender, QuestAcceptRequest request)
        {

            // ============================================== 日志 ==============================================

            Log.InfoFormat("OnQuestAccept questId : [{0}]", request.QuestId); // 打印网络日志，方便后端追踪查错

            // ======================================== 下发Manager处理 =========================================

            Character character = sender.Session.Character; // 从当前网络会话(Session)中提取出发起请求的玩家角色实体
            int questId = request.QuestId; // 解析请求包中的目标任务 ID

            Result result = character.questManager.AcceptQuest(questId, out Quest quest); // 将核心业务逻辑下发给该角色的 Manager 去处理，并获取处理结果和任务实体引用

            // ======================================== 发包发送网络信息 ========================================

            sender.Session.Response.questAccept = new QuestAcceptResponse(); // 实例化准备下发给客户端的回应包

            if (result == Result.Success && quest != null) // 分支 A：接取成功
            {
                sender.Session.Response.questAccept.Quest = quest.ToNQuestInfo(); // 将服务器内存任务转为网络传输格式(NQuestInfo)并塞入回应包
            }
            else // 分支 B：接取失败（由于等级不够、前置未完成、或已接取等原因）
            {
                sender.Session.Response.questAccept.Errormsg = "条件不足 任务无法接取"; // 填充错误提示，客户端会直接展示此文案
            }

            sender.Session.Response.questAccept.Result = result; // 赋值协议层的枚举结果码 (Success / Failed)
            sender.SendResponse(); // 通过底层网络队列，将整个 Response 封包异步发送回客户端
        }

        /// <summary>
        /// 接收并处理客户端发起的【交付任务】请求。
        /// 执行后：调用具体玩家内存里的 QuestManager 进行交付校验与发奖。成功则下发更新后的任务状态；失败则下发错误提示。
        /// </summary>
        public void OnQuestSubmit(NetConnection<NetSession> sender, QuestSubmitRequest request)
        {
            // ============================================== 日志 ==============================================

            Log.InfoFormat("OnQuestSubmit quest: [{0}]", request.QuestId); // 打印网络日志

            // ======================================== 下发Manager处理 =========================================
            Character character = sender.Session.Character; // 提取请求上下文中的玩家角色实体
            int questId = request.QuestId; // 解析请求包中的目标任务 ID
            Result result = character.questManager.SubmitQuest(questId, out Quest quest); // 调用 Manager 执行核心交付逻辑（包含发奖和写库操作）

            // ======================================== 发包发送网络信息 ========================================

            sender.Session.Response.questSubmit = new QuestSubmitResponse(); // 实例化回应包
            if (result == Result.Success) // 分支 A：交付成功
            {
                sender.Session.Response.questSubmit.Quest = quest.ToNQuestInfo(); // 将状态已变更为 Finished 的网络数据同步给客户端
            }
            else // 分支 B：交付失败（如打怪进度没满、背包空间不足等拦截）
            { 
                sender.Session.Response.questSubmit.Errormsg = "条件不足 任务无法提交"; // 填充错误弹窗提示文案
            } 
            sender.Session.Response.questSubmit.Result = result; // 赋值协议层的枚举结果码
            sender.SendResponse(); // 推送网络包回客户端
        }

    }
}
