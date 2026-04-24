using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Services;
using SkillBridge.Message;

//单例模式
public class UILogin : MonoBehaviour
{
    public static UILogin instance;
    public InputField InputUsername;
    public InputField InputPassword;
    public Button LoginButton;
    private void Start()
    {
        UserService.Instance.OnLogin = this.OnLogin;
    } 
    void OnLogin(Result result, string message)
    {
        if (result == Result.Success)
        {
            MessageBox.Show("登录成功，准备角色选择" + message, "提示", MessageBoxType.Information);
            SceneManager.Instance.LoadScene("CharSelect");
        }
        else
        {
            MessageBox.Show(message, "错误", MessageBoxType.Error);
        }
    }
    public void OnClickLogin()
    {
        if(string.IsNullOrEmpty(this.InputUsername.text))
        {
            MessageBox.Show("请输入账号");
            return;
        }
        if (string.IsNullOrEmpty(this.InputPassword.text))
        {
            MessageBox.Show("请输入密码");
            return;
        }
        UserService.Instance.SendLogin(this.InputUsername.text, this.InputPassword.text);
    }
}
