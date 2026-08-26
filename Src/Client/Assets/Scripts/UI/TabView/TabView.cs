using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TabView : MonoBehaviour
{
    public UnityAction<int> OnTabSelected;
    public TabButton[] tabButtons;
    public GameObject[] tabPages;
    public int index = -1;
    
    // 初始化的时候没有问题
    IEnumerator Start()
    {
        for(int i  = 0; i < tabButtons.Length; i++)
        {
            tabButtons[i].tableView = this;
            tabButtons[i].index = i;
        }
        yield return new WaitForEndOfFrame();
        SelectTab(0);
    }
    public void SelectTab(int index)
    {
        // 只要点击的不是当前已经选中的标签，就执行切换逻辑
        if (this.index != index)
        {
            // 1. 无论有没有 Pages，永远先更新按钮的高亮状态！
            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].Select(i == index);
            }

            // 2. 如果存在 Pages，再去控制页面的显隐
            if (tabPages != null && tabPages.Length > 0)
            {
                for (int i = 0; i < tabPages.Length; i++)
                {
                    tabPages[i].SetActive(i == index);
                }
            }

            // 3. 更新当前选中的索引记录
            this.index = index;

            // 4. 触发外部事件（通知 UIChat 刷新聊天列表）
            if (this.OnTabSelected != null)
            {
                this.OnTabSelected(index);
            }
        }
    }
}
