using UnityEngine;

class MessageBox
{
    static Object cacheObject = null;

    /// <summary>
    /// 弹出通用 UI 提示框
    /// </summary>
    /// <param name="message">弹窗内显示的正文内容 (必填)</param>
    /// <param name="title">弹窗左上角的标题 (选填，默认为空)</param>
    /// <param name="type">弹窗的类型，决定显示的图标和按钮数量 (选填，默认是单按钮提示框)</param>
    /// <param name="btnOK">确认按钮的文字 (选填，不传则使用Prefab中的默认字)</param>
    /// <param name="btnCancel">取消按钮的文字 (选填，不传则使用Prefab中的默认字)</param>
    /// <returns>返回生成的弹窗实例，可以通过返回值的 OnYes/OnNo 绑定点击事件</returns>
    public static UIMessageBox Show(string message, string title="", MessageBoxType type = MessageBoxType.Information, string btnOK = "", string btnCancel = "")
    {
        if(cacheObject==null)
        {
            cacheObject = Resloader.Load<Object>("UI/UIMessageBox");
        }

        GameObject go = (GameObject)GameObject.Instantiate(cacheObject);
        UIMessageBox msgbox = go.GetComponent<UIMessageBox>();
        msgbox.Init(title, message, type, btnOK, btnCancel);
        return msgbox;
    }
}

public enum MessageBoxType
{
    /// <summary>
    /// Information Dialog with OK button
    /// </summary>
    Information = 1,

    /// <summary>
    /// Confirm Dialog whit OK and Cancel buttons
    /// </summary>
    Confirm = 2,

    /// <summary>
    /// Error Dialog with OK buttons
    /// </summary>
    Error = 3
}