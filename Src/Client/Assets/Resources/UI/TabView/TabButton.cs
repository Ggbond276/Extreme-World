using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    public TabView tableView;
    public int index = 0;

    private Sprite normalImage;
    public Sprite activeImage;


    private Image tabImage;
    private void Start()
    {
        tabImage = this.GetComponent<Image>();
        normalImage = tabImage.sprite;

        this.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        this.tableView.SelectTab(index);
    }

    public void Select(bool isSelect)
    {
        tabImage.overrideSprite = isSelect ? activeImage : normalImage;
    }
}
