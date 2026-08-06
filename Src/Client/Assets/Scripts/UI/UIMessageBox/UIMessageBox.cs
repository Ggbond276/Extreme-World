using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 通用提示框的 UI 表现层脚本
/// 职责：只负责根据传入的数据修改自身的文本、图标、按钮状态，以及广播按钮点击事件
/// </summary>
public class UIMessageBox : MonoBehaviour {

    public Text title;
    public Text message;
    public Image[] icons;
    public Button buttonYes;
    public Button buttonNo;
    public Button buttonClose;

    public Text buttonYesTitle;
    public Text buttonNoTitle;

    public UnityAction OnYes;
    public UnityAction OnNo;

    /// <summary>
    /// 初始化弹窗的界面表现
    /// </summary>
    public void Init(string title, string message, MessageBoxType type = MessageBoxType.Information, string btnOK = "", string btnCancel = "")
    {
        if (!string.IsNullOrEmpty(title)) this.title.text = title;
        this.message.text = message;
        this.icons[0].enabled = type == MessageBoxType.Information;
        this.icons[1].enabled = type == MessageBoxType.Confirm;
        this.icons[2].enabled = type == MessageBoxType.Error;

        if (!string.IsNullOrEmpty(btnOK)) this.buttonYesTitle.text = btnOK;
        if (!string.IsNullOrEmpty(btnCancel)) this.buttonNoTitle.text = btnCancel;

        this.buttonYes.onClick.AddListener(OnClickYes);
        this.buttonNo.onClick.AddListener(OnClickNo);

        this.buttonNo.gameObject.SetActive(type == MessageBoxType.Confirm);
    }

    /// <summary>
    /// 内部点击确认逻辑：触发外部委托 -> 销毁弹窗自身
    /// </summary>
    public void OnClickYes()
    {
        if (this.OnYes != null)
            this.OnYes();
        Destroy(this.gameObject);
    }

    /// <summary>
    /// 内部点击取消逻辑：销毁弹窗自身 -> 触发外部委托
    /// </summary>
    public void OnClickNo()
    {
        Destroy(this.gameObject);
        if (this.OnNo != null)
            this.OnNo();
    }
}
