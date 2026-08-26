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

        // 针对浅色羊皮纸底板，全面采用：高饱和深色 + 加粗 <b> 标签，彻底抛弃难看的 <mark> 底色
        switch (msg.Channel)
        {
            case ChatChannel.System:
                // 经典系统红 (深红)
                channelTag = "<color=#C62828><b>[系统]</b></color>";
                break;
            case ChatChannel.World:
                // 世界深蓝
                channelTag = "<color=#1565C0><b>[世界]</b></color>";
                break;
            case ChatChannel.Guild:
                // 公会深绿
                channelTag = "<color=#2E7D32><b>[公会]</b></color>";
                break;
            case ChatChannel.Team:
                // 队伍深紫
                channelTag = "<color=#6A1B9A><b>[队伍]</b></color>";
                break;
            case ChatChannel.Private:
                // 私聊深粉红
                channelTag = "<color=#AD1457><b>[私聊]</b></color>";
                break;
            case ChatChannel.Local:
                // 当前深灰蓝
                channelTag = "<color=#37474F><b>[当前]</b></color>";
                break;
        }

        // 2. 组装玩家名字 (系统消息不需要名字)
        if (msg.Channel != ChatChannel.System)
        {
            // 名字使用深青色 (Dark Cyan)，既能和频道区分开，又能和内容区分开
            nameTag = $" <color=#00838F><link=\"{msg.fromId}\">[{msg.fromName}]</link></color> ";
        }

        // 3. 最终拼接：聊天内容使用极深灰色(#212121)，绝对不要用纯黑，深灰色在羊皮纸上看着最舒服、不刺眼
        txtMessage.text = $"{channelTag}{nameTag}: <color=#212121>{content}</color>";
    }
}
