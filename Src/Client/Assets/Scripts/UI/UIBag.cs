using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Models;

namespace Assets.Scripts.UI
{
    class UIBag : UIWindow
    {
        public Transform[] pages;
        public GameObject bagItem;
        private List<Image> slots;
        public Text money;
        void Start()
        {
            if (slots == null)
            {
                slots = new List<Image>();
                for (int i = 0; i < pages.Length; i++)
                {
                    slots.AddRange(this.pages[i].GetComponentsInChildren<Image>(true));
                }
            }
            StartCoroutine(InitBag());
        }
        IEnumerator InitBag()
        {
            // 对背包内的数据进行遍历
            for (int i = 0; i < BagManager.Instance.bagItems.Length; i++)
            {
                var item = BagManager.Instance.bagItems[i];
                if (item.ItemId > 0)
                {
                    GameObject go = Instantiate(bagItem, slots[i].transform);
                    // 获取图标
                    string icon = ItemManager.Instance.Items[item.ItemId].define.Icon;
                    // 获取数量
                    int count = item.Count;
                    // 获取脚本
                    var ui = go.GetComponent<UIBagItem>();
                    ui.SetMainIcon(icon, count);
                }

            }

            for (int i = BagManager.Instance.bagItems.Length; i < slots.Count; i++)
            {
                slots[i].color = Color.gray;
            }

            money.text = User.Instance.CurrentCharacter.Gold.ToString();
            yield return null;
        }

        void Clear()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].transform.childCount > 0)
                {
                    Destroy(slots[i].transform.GetChild(0).gameObject);
                }
            }
        }

        public void OnReset()
        {
            BagManager.Instance.Reset();
            this.Clear();
            StartCoroutine(InitBag());
        }
    }
}