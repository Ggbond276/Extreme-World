using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SkillBridge.Message;
using Services;

public class UIRegister : MonoBehaviour
{
    #region 获取UI组件
    public InputField InputEmail;
    public InputField InputPassword;
    public InputField InputRepassword;
    public Button RegisterButton;
    #endregion

    private void Start()
    {
        UserService.Instance.OnRegister = this.OnRegister;
    }

    #region 以弹窗的形式告知用户注册情况
    //Use pop-up windows to notify the registration results
    void OnRegister(SkillBridge.Message.Result result, string msg)
    {
        MessageBox.Show(string.Format("结果: {0} 信息:{1}", result, msg));
    }
    #endregion 

    #region 按下注册按钮后 核验之后将注册信息发送给UserService逻辑层
    //After ClickRegister
    public void OnClickRegister()
    {
        if (string.IsNullOrEmpty(this.InputEmail.text))
        {
            MessageBox.Show("请输入账号");
            return;
        }
        if (string.IsNullOrEmpty(this.InputPassword.text))
        {
            MessageBox.Show("请输入密码");
            return;
        }
        if (string.IsNullOrEmpty(this.InputRepassword.text))
        {
            MessageBox.Show("请输入确认密码");
            return;
        }
        if (this.InputPassword.text != this.InputRepassword.text)
        {
            MessageBox.Show("两次输入的密码不一致");
            return;
        }
        //将输入的数据发送给服务层 让服务层处理数据
        UserService.Instance.SendRegister(this.InputEmail.text, this.InputPassword.text);
    }
    #endregion

}



