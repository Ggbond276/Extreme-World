using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICharInfo : MonoBehaviour
{
    public SkillBridge.Message.NCharacterInfo info;

    public Text charClass;
    public Text charName;

    // Use this for initialization
    void Start()
    {
        //如果info信息不为空就在组件中显示出信息
        if (info != null)
        {
            //显示职业信息
            this.charClass.text = this.info.Class.ToString();
            //显示角色名称信息
            this.charName.text = this.info.Name;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
