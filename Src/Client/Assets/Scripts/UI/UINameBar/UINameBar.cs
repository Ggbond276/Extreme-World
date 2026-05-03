using Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINameBar : MonoBehaviour {

    // 角色姓名显示框
    public Text characterInfo;
    
    public Character character;

    void Start () {
		if(this.character!=null)
        {
            this.UpdateInfo();
        }
	}

	void Update () {
        this.UpdateInfo();
        // 为了让视线对其摄像机
        this.transform.forward = Camera.main.transform.forward;
	}

    void UpdateInfo()
    {
        if (this.character != null)
        {
            string info = this.character.Name + " Lv." + this.character.Info.Level;

            if(info != this.characterInfo.text)
            {
                this.characterInfo.text = info;
            }
        }
    }
}
