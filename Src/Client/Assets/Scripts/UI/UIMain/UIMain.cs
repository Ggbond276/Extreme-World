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

    protected override void OnStart()
    {
        this.UpdateAvatar();
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

    // 点击打开商店1
    public void OnClickShop1()
    {
        UIShop shop = UIManager.Instance.Show<UIShop>();
        shop.SetShop(DataManager.Instance.Shops[1]);
    }
    // 点击打开商店2
    public void OnClickShop2()
    {
        UIShop shop = UIManager.Instance.Show<UIShop>();
        shop.SetShop(DataManager.Instance.Shops[2]);
    }
    // 点击打开装备栏
    public void OnClickCharEquip()
    {
        UIManager.Instance.Show<UICharEquip>();
    }
    // 点击打开任务系统
    public void OnClickQuestSystem()
    {
        UIManager.Instance.Show<UIQuestSystem>();
    }
}   
