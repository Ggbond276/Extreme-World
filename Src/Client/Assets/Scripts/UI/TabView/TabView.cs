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
        if (tabPages == null ||  tabPages.Length == 0)
        {
            this.OnTabSelected(index);
        } 
        else
        {
            if (this.index != index)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    tabButtons[i].Select(i == index);
                    tabPages[i].SetActive(i == index);
                }
                this.index = index;
            }
        }
    }
}
