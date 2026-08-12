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
    class UITeamSystem : UIWindow
    {
        [Header("核心引用")]
        public Text text_title;
        public GameObject prefab;
        public ListView listMain;


        /// <summary>
        /// 当前玩家用鼠标选中的队员条目引用
        /// </summary>
        public UITeamItem selectedItem;



        /// <summary>
        /// 面板初始化：注册数据监听大喇叭，并执行首次渲染
        /// </summary>
        private void Start()
        {
            TeamManager.Instance.OnTeamChanged += OnTeamChanged;
            this.listMain.OnItemSelected += OnItemSelected;
            RefreshUI();
        }
        /// <summary>
        /// 面板销毁：严格注销所有事件监听，防止内存泄漏和野指针报错
        /// </summary>
        private void OnDestroy()
        {
            if (TeamManager.Instance != null)
                TeamManager.Instance.OnTeamChanged -= OnTeamChanged;

                
            if (this.listMain.OnItemSelected != null)
                this.listMain.OnItemSelected -= OnItemSelected;
        }



        /// <summary>
        /// 听到 TeamManager 队伍数据变化的广播后触发
        /// </summary>
        private void OnTeamChanged()
        {
            ClearList();
            RefreshUI();
        }

        /// <summary>
        /// 当玩家点击了具体的某个队员条目时触发
        /// </summary>
        /// <param name="item">被选中的底层 ListViewItem 对象</param>
        private void OnItemSelected(ListViewItem item)
        {
            this.selectedItem = item as UITeamItem;
        }




        /// <summary>
        /// 执行 UI 全量渲染 SOP（提取数据 -> 基础渲染 -> 列表克隆）
        /// </summary>
        private void RefreshUI()
        {
            NTeamInfo CurrentTeam = TeamManager.Instance.CurrentTeam;
            if (CurrentTeam == null)
            {
                this.Close();
                return;
            }

            int id = CurrentTeam.Id;
            int leader = CurrentTeam.Leader;
            List<NCharacterInfo> members = CurrentTeam.Members;


            if (this.text_title != null)
            {
                this.text_title.text = $"我的队伍({members.Count}/5)";
            }


            foreach (var m in members)
            {
                GameObject go = Instantiate(prefab, this.listMain.transform);

                UITeamItem uiTeamItem = go.GetComponent<UITeamItem>();

                this.listMain.AddItem(uiTeamItem);

                uiTeamItem.SetItemInfo(m, leader);
            }
        }

        /// <summary>
        /// 清理流水线：销毁所有动态生成的预制体，重置逻辑花名册
        /// </summary>
        private void ClearList()
        {
            listMain.RemoveAll();

            for (int i = listMain.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(this.listMain.transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 点击事件:发送离队请求
        /// </summary>
        public void OnClickLeaveTeam()
        {
            TeamManager.Instance.SendLeave();
        }
    }
    
}
