using Assets.Scripts.Models;
using Assets.Scripts.UI;
using Models;
using Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : MonoSingleton<UIMain>
{
    public Text avatarName;
    public Text avatarLevel;
    // 注册监听
    protected override void OnStart()
    {
        this.UpdateAvatar();
        QuestManager.Instance.OnOpenQuestDialog += OnOpenQuestDialog;
    }
    // 注销监听
    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnOpenQuestDialog -= OnOpenQuestDialog;
    }
    void UpdateAvatar()
    {
        this.avatarName.text = string.Format("{0} [{1}]", User.Instance.CurrentCharacter.Name, User.Instance.CurrentCharacter.Id);
        this.avatarLevel.text = User.Instance.CurrentCharacter.Level.ToString();
    }
    public void BackToCharSelect()
    {
        SceneManager.Instance.LoadScene("CharSelect");
        UserService.Instance.SendGameLeave();
    }

    public void OnClickTest()
    {
        UIManager.Instance.Show<UITest>();
    }

    public void OnClickBag()
    {
        UIManager.Instance.Show<UIBag>();
    }

    /// <summary>
    /// 打开商店1
    /// </summary>
    public void OnClickShop1()
    {
        UIShop shop = UIManager.Instance.Show<UIShop>();
        shop.SetShop(DataManager.Instance.Shops[1]);
    }

    /// <summary>
    /// 打开商店2
    /// </summary>
    public void OnClickShop2()
    {
        UIShop shop = UIManager.Instance.Show<UIShop>();
        shop.SetShop(DataManager.Instance.Shops[2]);
    }

    /// <summary>
    /// 打开装备栏
    /// </summary>
    public void OnClickCharEquip()
    {
        UIManager.Instance.Show<UICharEquip>();
    }

    /// <summary>
    /// 打开任务系统
    /// </summary>
    public void OnClickQuestSystem()
    {
        UIManager.Instance.Show<UIQuestSystem>();
    }

    /// <summary>
    /// 打开任务对话面板
    /// </summary>
    /// <param name="targetQuest"></param>
    public void OnOpenQuestDialog(Quest targetQuest)
    {
         UIQuestDialog dlg = UIManager.Instance.Show<UIQuestDialog>();
         dlg.SetQuest(targetQuest);
    }

    /// <summary>
    /// 打开好友系统面板
    /// </summary>
    public void OnClickFriendSystem()
    {
        UIManager.Instance.Show<UIFriends>();
    }
}   
