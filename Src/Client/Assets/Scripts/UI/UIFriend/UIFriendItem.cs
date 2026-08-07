using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFriendItem : ListViewItem
{
    [Header("UI节点绑定")]
    public Text Text_Name;
    public Text Text_Class;
    public Text Text_Level;
    public Text Text_Status;
    public Image Image_Bg;

    [Header("状态表现")]
    public Sprite normalBg;
    public Sprite selectedBg;

    // 缓存自身的好友数据
    public NFriendInfo Info { get; private set; }

    /// <summary>
    /// 被点击之后判断用什么图片
    /// </summary>
    /// <param name="selected"></param>
    public override void OnSelected(bool selected)
    {
        if(this.selectedBg != null)
        {
            this.Image_Bg.overrideSprite = selected ? selectedBg : normalBg;
        }
    }

    /// <summary>
    /// 初始化UI数据
    /// </summary>
    /// <param name="item"></param>
    public void SetFriendInfo(NFriendInfo item)
    {
        this.Info = item;

        if (this.Text_Name != null) this.Text_Name.text = this.Info.friendInfo.Name;
        if (this.Text_Class != null) this.Text_Class.text = this.Info.friendInfo.Class.ToString();
        if (this.Text_Level != null) this.Text_Level.text = this.Info.friendInfo.Level.ToString();

        if(this.Text_Status != null)
        {
            // 1代表在线 0代表下线 在线用绿色字体 下线用红色字体
            this.Text_Status.text = this.Info.Status == 1 ? "<color=#00FF00>在线</color>" : "<color=#808080>离线</color>";
        }
    }
}
