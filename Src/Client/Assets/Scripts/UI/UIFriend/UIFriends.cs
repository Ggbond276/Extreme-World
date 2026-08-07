using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFriends : UIWindow
{
    [Header("核心引用")]
    public GameObject itemPrefab;
    // 这个其实就是容器了 ListView挂载在Content上ScrollView的Content上
    public ListView listMain;

    // 当前鼠标选中的好友条目
    private UIFriendItem selectedItem;



    /// <summary>
    /// 生命周期函数 当好友面板组件被创建出来的那一刻马上生效
    /// </summary>
    private void Start()
    {
        // 1.Manager层发生变化就要立马刷新UI界面
        FriendManager.Instance.OnFriendDataChanged += RefreshUI;
        // 2.列表中有东西被选中要干的事情
        this.listMain.OnItemSelected += this.OnFriendSelected;
        // 3.打开面板的时候刷新UI
        this.RefreshUI();
    }

    /// <summary>
    /// 销毁时注销所有事件监听
    /// </summary>
    private void OnDestroy()
    {
        if(FriendManager.Instance != null)
        {
            FriendManager.Instance.OnFriendDataChanged -= RefreshUI;
        }
    }

    /// <summary>
    /// 列表项选中事件
    /// </summary>
    public void OnFriendSelected(ListViewItem item)
    {
        this.selectedItem = item as UIFriendItem;
    }



    /// <summary>
    /// 听到广播后直接全量覆盖
    /// </summary>
    private void RefreshUI()
    {
        this.ClearFriendList();
        this.InitFriendItems();
    }

    /// <summary>
    /// 初始化所有的好友数据 包括UI渲染 和数据载入
    /// </summary>
    private void InitFriendItems()
    {
        foreach(var item in FriendManager.Instance.allFriends)
        {
            GameObject go = Instantiate(itemPrefab, this.listMain.transform);
            UIFriendItem ui = go.GetComponent<UIFriendItem>();
            ui.SetFriendInfo(item);
            this.listMain.AddItem(ui);
        }
    }

    /// <summary>
    ///  清除所有的好友数据
    /// </summary>
    private void ClearFriendList()
    {
        this.listMain.RemoveAll();
        for (int i = this.listMain.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(this.listMain.transform.GetChild(i).gameObject);
        }
    }



    /// <summary>
    /// 点击Button_Add （添加好友，调用该功能）
    /// </summary>
    public void OnClickFriendAdd()
    {
        InputBox.Show("请输入想要添加的好友名称或ID", "添加好友").OnSubmit += OnFriendAddSubmit;
    }

   /// <summary>
   /// 处理输入姓名后提交的方法
   /// </summary>
   /// <returns></returns>
    private bool OnFriendAddSubmit(string inputText, out string errorMsg)
    {
        errorMsg = "";
        int friendId = 0;
        string friendName = "";

        // 判断玩家输入的是姓名还是ID
        if (!int.TryParse(inputText, out friendId))
            friendName = inputText;

        // 做基础拦截
        if(friendId == User.Instance.CurrentCharacter.Id || friendName == User.Instance.CurrentCharacter.Name)
        {
            errorMsg = "不能添加自己为好友";
            return false;
        }

        // 丢给Service层去发包
        FriendService.Instance.SendAddRequest(friendId, friendName);
        return true;
    }

    /// <summary>
    /// 点击Button_Chat（好友聊天，调用该功能）
    /// </summary>
    public void OnClickFriendChat()
    {
        MessageBox.Show("聊天功能尚未开放", "提示");
    }
    /// <summary>
    /// 点击Button_Delete（删除好友，调用该功能）
    /// </summary>
    public void OnClickFriendRemove()
    {
        if(selectedItem != null)
        {
            int requesterId = User.Instance.CurrentCharacter.Id;
            //问题出现在这一条语句friendInfo中几乎没有任何信息
            int targetId = selectedItem.Info.Id;
            FriendService.Instance.SendRemoveRequest(requesterId, targetId);
        }
    }

}
