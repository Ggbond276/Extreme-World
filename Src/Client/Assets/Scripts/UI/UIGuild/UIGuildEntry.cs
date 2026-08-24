using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGuildEntry : UIWindow
{
    [Header("选项卡片（使用Toggle 组件）")]
    public Toggle toggleCreate;
    public Toggle toggleJoin;

    [Header("操作按钮")]
    public Button btnConfirm;
    
    /// <summary>
    ///  生命周期函数
    /// </summary>
    private void Start()
    {
        if(btnConfirm != null)
        {
            btnConfirm.onClick.AddListener(OnClickConfirm);
        }
    }

    /// <summary>
    /// 点击确认按钮
    /// </summary>
    private void OnClickConfirm()
    {
        if(toggleCreate != null && toggleCreate.isOn)
        {
            this.Close();
            UIManager.Instance.Show<UIGuildCreate>();
        }
        else if(toggleJoin != null && toggleJoin.isOn)
        {
            this.Close();
            UIManager.Instance.Show<UIGuildJoin>();
        }
        else
        {
            MessageBox.Show("请先选择一项操作！", "提示", MessageBoxType.Information);
        }
    }


}
