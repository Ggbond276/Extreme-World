using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SkillBridge.Message; 

public class UIChatMessage : MonoBehaviour
{
    public TextMeshProUGUI txtMessage;

    public void SetMessage(NChatMessage msg)
    {
        string channelTag = "";
        string nameTag = "";
        string content = msg.Message;

        switch (msg.Channel)
        {
            case ChatChannel.System:
                // 红底白字，外加一点透明度 
                channelTag = "<mark=#FF000088><color=#FFFFFF>系统</color></mark>";
                break;
            case ChatChannel.World:
                // 蓝底白字
                channelTag = "<mark=#0000FF88><color=#FFFFFF>世界</color></mark>";
                break;
            case ChatChannel.Guild:
                // 绿底白字
                channelTag = "<mark=#00800088><color=#FFFFFF>公会</color></mark>";
                break;
            case ChatChannel.Team:
                // 紫底白字
                channelTag = "<mark=#80008088><color=#FFFFFF>队伍</color></mark>";
                break;
            case ChatChannel.Private:
                // 粉底白字
                channelTag = "<mark=#FF149388><color=#FFFFFF>私聊</color></mark>";
                break;
            case ChatChannel.Local:
                // 黑底白字
                channelTag = "<mark=#00000088><color=#FFFFFF>当前</color></mark>";
                break;
        }

        // 2. 组装玩家名字 (系统消息不需要名字)
        if (msg.Channel != ChatChannel.System)
        {
            // 利用 <color> 改变名字颜色。
            // 利用 <link> 标签把名字包起来，后续你刚才生成的那个“邀请/加好友”弹窗，就靠监听这个 link 点击来触发！
            nameTag = $" <color=#00BFFF><link=\"{msg.fromId}\">[{msg.fromName}]</link></color> ";
        }

        // 3. 最终拼接：频道标 + 名字 + 冒号 + 聊天内容
        txtMessage.text = $"{channelTag}{nameTag}: <color=#E0E0E0>{content}</color>";
    }

}
