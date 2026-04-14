using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIWindow : MonoBehaviour
{
    // 需要知道是谁 点击的结果是什么
    public delegate void CloseHandler(UIWindow sender, WindowResult result);
    // 因为UI界面不管点击什么按钮都是OnClose所以统一为OnClose
    public event CloseHandler OnClose;
    
    public virtual Type type
    {
        get
        {
            return this.GetType();
        }
    }

    public enum WindowResult
    {
        None = 0,
        Yes,
        No,
    }

    // 这个是点击直接关闭的结果
    public void Close(WindowResult result = WindowResult.None)
    {
        UIManager.Instance.Close(this.type);
        // 如果OnClose 不为空就执行OnClose
        if (this.OnClose != null)
            this.OnClose(this, result);
        this.OnClose = null;
    }
    // 点击关闭按钮时触发
    public virtual void OnCloseClick()
    {
        this.Close();
    }
    // 点击确定按钮时触发
    public virtual void OnYesClick()
    {
        this.Close(WindowResult.Yes);
    }

    private void OnMouseDown()
    {
        Debug.LogFormat(this.name + " Clicked");
    }
}
