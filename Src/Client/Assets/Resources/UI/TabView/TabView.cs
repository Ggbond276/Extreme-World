using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabView : MonoBehaviour
{
    public TabButton[] tabButtons;
    public GameObject[] tabPages;
    public int index = -1;
    
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
        if (this.index != index)
        {
            for(int i = 0; i < tabButtons.Length; i++)
            {   
                // 1.让index等于这个的button亮起来
                tabButtons[i].Select(i == index);
                // 2.让index等于这个的页面显示出来
                tabPages[i].SetActive(i == index);
            }
            this.index = index;
        }
    }



    
}
