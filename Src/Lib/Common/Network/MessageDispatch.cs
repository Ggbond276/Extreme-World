//WARNING: DON'T EDIT THIS FILE!!!
using Common;

namespace Network
{
    public class MessageDispatch<T> : Singleton<MessageDispatch<T>>
    {
        public void Dispatch(T sender, SkillBridge.Message.NetMessageResponse message)
        {
            if (message.userRegister != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.userRegister); }
            if (message.userLogin != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.userLogin); }
            if (message.createChar != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.createChar); }
            if (message.gameEnter != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.gameEnter); }
            if (message.gameLeave != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.gameLeave); }
            if (message.mapCharacterEnter != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.mapCharacterEnter); }
            if (message.mapCharacterLeave != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.mapCharacterLeave); }
            if (message.mapEntitySync != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.mapEntitySync); }   
            if (message.itemBuy != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.itemBuy); }
            if (message.statusNotify != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.statusNotify); }
            if (message.itemEquip != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.itemEquip); }
            if (message.questList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.questList); }
            if (message.questAccept != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.questAccept); }
            if (message.questSubmit != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.questSubmit); }
            if (message.questAbandon != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.questAbandon); }
            if (message.friendAdd != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.friendAdd); }
            if (message.friendList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.friendList); }
            if (message.friendRemove != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.friendRemove); }
            if (message.teamInviteRes != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.teamInviteRes); }
            if (message.teamInviteReq != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.teamInviteReq); }
            if (message.teamInfo != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.teamInfo); }
            if (message.teamLeave != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.teamLeave); }
            // 公会系统响应分发
            if (message.guildCreate != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildCreate); }
            if (message.guildDisband != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildDisband); }
            if (message.guildSettingModify != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildSettingModify); }
            if (message.guildJoinApply != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildJoinApply); }
            if (message.guildApplyProcess != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildApplyProcess); }
            if (message.guildLeave != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildLeave); }
            if (message.guildMemberList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildMemberList); }
            if (message.guildApplyList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildApplyList); }
            if (message.guildList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildList); }
            if (message.guildAdmin != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildAdmin); }

            // 公会组播广播分发
            if (message.guildMemberAddNotify != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildMemberAddNotify); }
            if (message.guildMemberLeaveNotify != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildMemberLeaveNotify); }
            if (message.guildInfoChangeNotify != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildInfoChangeNotify); }
            if (message.guildChatNotify != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildChatNotify); }
        }

        public void Dispatch(T sender, SkillBridge.Message.NetMessageRequest message)
        {
            if (message.userRegister != null) { MessageDistributer<T>.Instance.RaiseEvent(sender,message.userRegister); }
            if (message.userLogin != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.userLogin); }
            if (message.createChar != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.createChar); }
            if (message.gameEnter != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.gameEnter); }
            if (message.gameLeave != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.gameLeave); }
            if (message.mapCharacterEnter != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.mapCharacterEnter); }
            if (message.mapEntitySync != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.mapEntitySync); }
            if (message.mapTeleport != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.mapTeleport); }
            if (message.itemBuy != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.itemBuy); }
            if (message.itemEquip != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.itemEquip); }
            if (message.questList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.questList); }
            if (message.questAccept != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.questAccept); }
            if (message.questSubmit != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.questSubmit); }
            if (message.questAbandon != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.questAbandon); }
            if (message.friendAdd != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.friendAdd); }
            if (message.friendList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.friendList); }
            if (message.friendRemove != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.friendRemove); }
            if (message.teamInviteRes != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.teamInviteRes); }
            if (message.teamInviteReq != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.teamInviteReq); }
            if (message.teamInfo != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.teamInfo); }
            if (message.teamLeave != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.teamLeave); }
            // 公会系统请求分发
            if (message.guildCreate != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildCreate); }
            if (message.guildDisband != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildDisband); }
            if (message.guildSettingModify != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildSettingModify); }
            if (message.guildJoinApply != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildJoinApply); }
            if (message.guildApplyProcess != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildApplyProcess); }
            if (message.guildLeave != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildLeave); }
            if (message.guildChat != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildChat); } // 注意：Chat 只有 Request
            if (message.guildMemberList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildMemberList); }
            if (message.guildApplyList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildApplyList); }
            if (message.guildList != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildList); }
            if (message.guildAdmin != null) { MessageDistributer<T>.Instance.RaiseEvent(sender, message.guildAdmin); }
        }
    }
}