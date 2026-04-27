using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ListView : MonoBehaviour
{
    public UnityAction<ListViewItem> OnItemSelected;
    public List<ListViewItem> items = new List<ListViewItem>();
    public ListViewItem selectedItem = null ;

    public void OnSelected(ListViewItem selected)
    {
        if (selected == null) return;
        if (this.selectedItem == selected) return;
        if(this.selectedItem != null)
        {
            this.selectedItem.IsSelected = false;
        } 
        this.selectedItem = selected;
        selected.IsSelected = true;
        if (OnItemSelected != null)
        {
            this.OnItemSelected(selected);
        }
    }    

    public void AddItem(ListViewItem item)
    {
        item.Onwer = this;
        items.Add(item);
    }

    public void RemoveAll()
    {
        items.Clear();
    }

}

