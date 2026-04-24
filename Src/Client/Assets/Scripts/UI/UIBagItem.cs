using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    class UIBagItem : MonoBehaviour
    {
        public Image Icon;
        public Text Count;

        public void SetMainIcon(string icon, int count)
        {
            Icon.overrideSprite = Resloader.Load<Sprite>(icon);
            Count.text = count.ToString();
        }
    }
}
