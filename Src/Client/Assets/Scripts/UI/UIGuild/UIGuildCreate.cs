using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGuildCreate : UIWindow
{
    [Header("UI控件绑定")]
    public InputField inputName;      // 名字输入框
    public InputField inputNotice;    // 宗旨输入框
    public Slider sliderLevel;        // 加入等级滑动条 (你拼好的那个金边条)
    public Text textLevel;            // 滑动条左边那个 "Lv.10" 的文本
    public Button buttonCreate;       // 确认创建按钮


    private void Start()
    {
        if(sliderLevel != null)
        {
            sliderLevel.onValueChanged.AddListener(OnLevelChanged);

            OnLevelChanged(sliderLevel.value);
        }

        if(buttonCreate != null)
        {
            buttonCreate.onClick.AddListener(OnClickCreate);
        }
    }
    private void OnDestroy()
    {
        
    }

    /// <summary>
    /// 纯视觉逻辑：把滑动条的浮点数，变成整数写进 Text 里
    /// </summary>
    /// <param name="value"></param>
    private void OnLevelChanged(float value)
    {
        if(textLevel != null)
        {
            textLevel.text = "Lv." + (float)value;
        }
    }

    /// <summary>
    /// 点击之后就会创建公会
    /// </summary>
    public void OnClickCreate()
    {
        string name = inputName.text.Trim();
        string notice = inputNotice.text.Trim();
        int level = (int)sliderLevel.value;
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(notice))
        {
            MessageBox.Show("公会名称和宗旨不能为空！", "提示", MessageBoxType.Information);
            return;
        }

        GuildManager.Instance.CreateGuild(name, notice, level);
    }
}
