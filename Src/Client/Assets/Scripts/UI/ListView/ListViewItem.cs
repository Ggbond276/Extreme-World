using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListViewItem : MonoBehaviour, IPointerClickHandler
{
    public ListView Owner;

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            this.isSelected = value;
            this.OnSelected(value);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 自己不管理状态 也不管理任何的逻辑 全部交给管理者去进行操作
        // 只要涉及到代码中没有被赋值的属性 全部要做排空处理
        if (this.Owner != null)
            this.Owner.OnSelected(this);
    }

    public virtual void OnSelected(bool value)
    {

    }
}
