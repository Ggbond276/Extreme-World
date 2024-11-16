using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;


//单例模式
public class UILogin : MonoBehaviour
{
    public static UILogin instance;
    private InputField InputUsername;
    private InputField InputPassword;
    private Button RegisterButton;
    private Button LoginButton;
    private void Awake()
    {
        instance = this;
        //获取文本输入组件
        InputUsername = transform.Find("InputUsername").GetComponent<InputField>();
        InputPassword = transform.Find("InputPassword").GetComponent <InputField>();
        //获取按钮组件
        RegisterButton = transform.Find("RegisterButton").GetComponent<Button>();
        LoginButton = transform.Find("LoginButton").GetComponent<Button>();
    }
    

    //注册按钮按下后
    private void RegisterButtonClick()
    {
        
    }
    //登录按钮按下后
    private void LoginButtonClick()
    {

    }
    //显示该面板
    public void ShowLoginPanel()
    {

    }
}
