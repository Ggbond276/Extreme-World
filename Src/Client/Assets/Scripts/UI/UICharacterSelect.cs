using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SkillBridge.Message;
using Services;
public class UICharacterSelect : MonoBehaviour
{
    //获取两个面板
    public GameObject SelectPanel;
    public GameObject CreatePanel;
    public UICharacterView UICharacterView;
    public InputField InputPlayerName;
    public Image[] titles;
    public Text[] names;
    private CharacterClass characterClass;
    void Start()
    {
        InitSelectCharacter(true);
        UserService.Instance.OnCharacterCreate = OnCharacterCreate;
    }

    //初始化角色选择界面
    public void InitSelectCharacter(bool init)
    {
        SelectPanel.SetActive(true);
        CreatePanel.SetActive(false);
    }
    //在角色选择界面点击选择创建新角色
    public void OnClickCreateCharacter()
    {
        SelectPanel.SetActive(false);
        CreatePanel.SetActive(true);
        UICharacterView.CurrentCharacter = 1;
    }




    //在角色创建界面点击返回按钮
    public void OnClickBack()
    {
        SelectPanel.SetActive(true);
        CreatePanel.SetActive(false);
    }
    //在角色创建界面点击选择职业
    public void OnClickSelectCareer(int index)
    {
        characterClass = (CharacterClass)index;
        //显示选择到的角色 不显示没选择的角色
        UICharacterView.CurrentCharacter = index - 1;
        //显示角色的名字和显示角色的概括信息
        for (int i = 0; i < 3; i++)
        {
            if (i == index)
            {
                titles[i].gameObject.SetActive(true);
            }
            else
            {
                titles[i].gameObject.SetActive(false);
            }
            //Data是用于读取表中的数据的
            names[i].text = DataManager.Instance.Characters[i + 1].Name;
        }
    }
    //在角色创建界面点击创建角色
    public void OnClickCreate()
    {
        if (string.IsNullOrEmpty(this.InputPlayerName.text))
        {
            MessageBox.Show("请输入角色名称");
        }
        UserService.Instance.SendCharacterCreate(InputPlayerName.text, characterClass);
    }
    //点击角色创建之后的回调方法
    public void OnCharacterCreate(Result result, string message)
    {
        if (result == Result.Success)
        {
            InitSelectCharacter(true);
        }
        else
        {
            MessageBox.Show(message, "错误", MessageBoxType.Error);
        }
    }

}
