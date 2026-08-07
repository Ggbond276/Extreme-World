using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有 UI 面板的抽象基类，封装了统一的关闭行为和事件回调
/// </summary>
public abstract class UIWindow : MonoBehaviour
{
    // 需要知道是谁 点击的结果是什么
    public delegate void CloseHandler(UIWindow sender, WindowResult result);
    // 因为UI界面不管点击什么按钮都是OnClose所以统一为OnClose
    public event CloseHandler OnClose;


    /// <summary>
    /// 获取当前脚本的具体类型 (用于传给 UIManager 作为字典的 Key)
    /// </summary>
    public virtual Type type
    {
        get
        {
            return this.GetType();
        }
    }

    /// <summary>
    /// 窗口关闭的结果枚举
    /// </summary>
    public enum WindowResult
    {
        None = 0,
        Yes,
        No,
    }

    /// <summary>
    /// 核心方法：关闭当前窗口
    /// </summary>
    /// <param name="result">关闭操作携带的标识</param>
    public void Close(WindowResult result = WindowResult.None)
    {
        // 这个内核实际上也是调用了Manager的方法
        UIManager.Instance.Close(this.type);
        // 如果OnClose 不为空就执行OnClose
        if (this.OnClose != null)
            this.OnClose(this, result);
        this.OnClose = null;
    }

    /// <summary>
    /// 供关闭按钮 (X) 绑定的点击事件
    /// </summary>
    public virtual void OnCloseClick()
    {
        this.Close(WindowResult.No);
    }

    /// <summary>
    /// 供确认按钮 (Yes/OK) 绑定的点击事件
    /// </summary>
    public virtual void OnYesClick()
    {
        this.Close(WindowResult.Yes);
    }

    private void OnMouseDown()
    {
        Debug.LogFormat(this.name + " Clicked");
    }
}
