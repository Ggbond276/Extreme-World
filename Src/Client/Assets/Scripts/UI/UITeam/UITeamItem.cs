using Assets.Scripts.Managers;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.UITeam
{
    class UITeamItem : ListViewItem
    {
        [Header("核心渲染")]
        public Image image_class;
        public Text text_level;
        public Text text_name;
        public Image image_leader;
        public Image image_hp;

        [Header("数据源")]
        public NCharacterInfo sourceData;
        public int leaderId;

        internal void SetItemInfo(NCharacterInfo m, int leaderId)
        {
            this.sourceData = m;
            this.leaderId = leaderId;
            if (image_class != null) this.image_class.overrideSprite = SpriteManager.Instance.GetClassSprite(m.Class);
            if (text_level != null) this.text_level.text = m.Level.ToString();
            if (text_name != null) this.text_name.text = m.Name;
            if (image_leader != null) image_leader.gameObject.SetActive(m.Id == leaderId);   
        }
    }
}
