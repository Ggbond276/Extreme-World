using Assets.Scripts.Models;
using Common.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIQuestItem : ListViewItem
{
    public Quest quest;

    [Header("文字渲染信息")]
    public Text mainOrBranch;
    public Text title;

    [Header("背景和切换图片")]
    public Image backGround;
    public Sprite normalImage;
    public Sprite activeImage;
    internal void SetQuestItem(Quest quest)
    {
        if (quest == null) return;
        if(quest.Define.Type == QuestType.Main)
        {
            mainOrBranch.text = "[主线]";
        }
        else
        {
            mainOrBranch.text = "[支线]";
        }
        title.text = quest.Define.Name;
    }

    public override void OnSelected(bool value)
    {
        backGround.overrideSprite = value ? activeImage : normalImage;
    }
}
