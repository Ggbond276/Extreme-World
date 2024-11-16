using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRegister : MonoBehaviour
{
    public static UIRegister instance;
    private InputField InputEmail;
    private InputField InputPassword;
    private InputField InputRepassword;
    private Button RegisterButton;
    private Button CancelButton;
    private void Awake()
    {
        instance = this;
        //获取文本输入组件
        InputEmail = transform.Find("InputEmail").GetComponent<InputField>();
        InputPassword = transform.Find("InputPassword").GetComponent<InputField>();
        InputRepassword = transform.Find("InputRepassward").GetComponent<InputField>();
        //获取按钮组件
        RegisterButton = transform.Find("RegisterButton").GetComponent<Button>();
        CancelButton = transform.Find("Cancel").GetComponent<Button>();
    }

    private void RegisterButtonClick()
    {

    }

    private void CancelButtonClick()
    {

    }

    public void ShowRegisterPanel()
    {

    }
}
