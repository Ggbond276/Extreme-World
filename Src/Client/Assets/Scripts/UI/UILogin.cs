using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;
using Services;
using SkillBridge.Message;

//单例模式
public class UILogin : MonoBehaviour
{
    #region 获取UI组件
    public static UILogin instance;
    public InputField InputUsername;
    public InputField InputPassword;
    public Button LoginButton;
    #endregion

    private void Start()
    {
        UserService.Instance.OnLogin = this.OnLogin;
    } 
    
    #region 以弹窗的形式告知用户服务端的响应情况
    void OnLogin(Result result, string message)
    {
        if (result == Result.Success)
        {
            //MessageBox.Show("登录成功，准备角色选择" + message, "提示", MessageBoxType.Information);
            SceneManager.Instance.LoadScene("CharSelect");
        }
        else
        {
            MessageBox.Show(message, "错误", MessageBoxType.Error);
        }
    }
    #endregion

    #region  按下登录按钮后 将登录数据发送给UserService逻辑层
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
    #endregion


}
