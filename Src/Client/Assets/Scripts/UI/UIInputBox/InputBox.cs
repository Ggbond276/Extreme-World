using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputBox
{
    public static Object CachePrefab;
    public static UIInputBox Show(string prompt, string title = "提示", string buttonConfirm = "确认", string buttonCancel = "取消")
    {
        if(CachePrefab == null)
        {
            CachePrefab = Resources.Load<Object>("UI/UIInputBox");
        }

        GameObject go = (GameObject)GameObject.Instantiate(CachePrefab);

        UIInputBox inputBox = go.GetComponent<UIInputBox>();

        inputBox.Init(title, prompt, buttonConfirm, buttonCancel);

        return inputBox;
    }
}
