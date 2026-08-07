using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIInputBox : MonoBehaviour
{
    public Text text_title;
    public Text text_prompt;
    public InputField text_input;
    public Text text_errorMsg;

    public Button button_confirm;
    public Button button_cancel;
    public Text text_buttonConfirm;
    public Text text_buttonCancel;

    public string defaultErrorMsg = "未输入任何文本";

    // 将事件委托给调用者处理
    public delegate bool SubmitHandler(string inputText, out string errorMsg);
    public event SubmitHandler OnSubmit;
    public UnityAction OnCancel;

    /// <summary>
    /// 外部传入参数，渲染组件
    /// </summary>
    /// <param name="title"></param>
    /// <param name="prompt"></param>
    /// <param name="buttonConfirm"></param>
    /// <param name="buttonCancel"></param>
    public void Init(string title, string prompt, string buttonConfirm, string buttonCancel)
    {
        if (!string.IsNullOrEmpty(title)) this.text_title.text = title;
        if (!string.IsNullOrEmpty(prompt)) this.text_prompt.text = prompt;
        if (!string.IsNullOrEmpty(buttonConfirm)) this.text_buttonConfirm.text = buttonConfirm;
        if (!string.IsNullOrEmpty(buttonCancel)) this.text_buttonCancel.text = buttonCancel;

        this.OnSubmit = null; 
        this.OnCancel = null;

        this.button_confirm.onClick.RemoveAllListeners();
        this.button_confirm.onClick.AddListener(OnClickYes);
        this.button_cancel.onClick.RemoveAllListeners();
        this.button_cancel.onClick.AddListener(OnClickCancel);
    }

    /// <summary>
    ///  点击确认
    /// </summary>
    public void OnClickYes()
    {
        // 第一个守卫
        if (string.IsNullOrEmpty(this.text_input.text))
        {
            this.text_errorMsg.text = this.defaultErrorMsg;
            return;
        }

        // 第二个守卫：1.如果有人注册了委托事件 2.并且委托事件的返回值是false
        if (OnSubmit != null && !OnSubmit(this.text_input.text, out string errorMsg) )
        {
            this.text_errorMsg.text = errorMsg;
            return;
        }

        Destroy(this.gameObject);
    }

    /// <summary>
    ///  点击取消
    /// </summary>
    public void OnClickCancel()
    {
        this.OnCancel?.Invoke();
        Destroy(this.gameObject);
    }
}
