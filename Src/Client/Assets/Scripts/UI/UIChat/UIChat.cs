using Assets.Scripts.Managers;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.UIChat
{
    class UIChat : UIWindow
    {
        [Header("UI组件引用")]
        public TabView tabView;
        public Transform node_Content;
        public GameObject itemPrefab;

        [Header("输入区组件引用")]
        public TMP_InputField inputMessage; // 聊天输入框
        public GameObject img_PrivateTarget;// 私聊目标底板 (Image_PrivateTarget)
        public TextMeshProUGUI txt_TargetName; // 私聊目标名字

        private int currentTabIndex = 0;

        private int privateTargetId = 0;
        private string privateTargetName = "";

        private void Start()
        {
            if(tabView != null)
            {
                tabView.OnTabSelected += OnChannelSwitched;
            }

            ChatManager.Instance.OnChatUpdated += OnChatDataUpdate;
            ChatManager.Instance.OnChatSendSuccess += OnChatSendSuccess;

            img_PrivateTarget.SetActive(false);
            RefreshChatList();
        }

        private void OnDestroy()
        {
            if(tabView != null)
            {
                tabView.OnTabSelected -= OnChannelSwitched;
            } 
            if(ChatManager.Instance != null)
            {
                ChatManager.Instance.OnChatUpdated -= OnChatDataUpdate;
                ChatManager.Instance.OnChatSendSuccess -= OnChatSendSuccess;
            }
        }

        private void RefreshChatList()
        {
            foreach(Transform child in node_Content)
            {
                Destroy(child.gameObject);
            }

            List<NChatMessage> currentList = GetMessagesByIndex(currentTabIndex);

            foreach(var msg in currentList)
            {
                GameObject go = Instantiate(itemPrefab, node_Content);
                UIChatMessage itemScript = go.GetComponent<UIChatMessage>();
                if (itemScript != null)
                    itemScript.SetMessage(msg);
            }
        }

        private ChatChannel GetChannelByIndex(int index)
        {
            switch (index)
            {
                case 1: return ChatChannel.System;
                case 2: return ChatChannel.World;
                case 3: return ChatChannel.Guild;
                case 4: return ChatChannel.Team;
                case 5: return ChatChannel.Private;
                default: return ChatChannel.Local;
            }
        }

        private List<NChatMessage> GetMessagesByIndex(int index)
        {
            switch(index)
            {
                case 0: return ChatManager.Instance.AllMessages;
                case 1: return ChatManager.Instance.SystemMessages;
                case 2: return ChatManager.Instance.WorldMessages;
                case 3: return ChatManager.Instance.GuildMessages;
                case 4: return ChatManager.Instance.TeamMessages;
                case 5: return ChatManager.Instance.PrivateMessages;
                default: return new List<NChatMessage>();
            }
        }

        private void OnChannelSwitched(int index)
        {
            this.currentTabIndex = index;

            if(this.currentTabIndex == 5 && privateTargetId != 0)
            {
                img_PrivateTarget.SetActive(true);
                txt_TargetName.text = $"@{privateTargetName}";
            }else
            {
                img_PrivateTarget.SetActive(false);
            }
            RefreshChatList();
        }


        private void OnChatDataUpdate(ChatChannel updatedChannel)
        {
            if(currentTabIndex == 0 || GetChannelByIndex(currentTabIndex) == updatedChannel)
            {
                RefreshChatList();
            }
        }

        private void OnChatSendSuccess()
        {
            inputMessage.text = "";
        }

        public void OnClickSend()
        {
            // 获取输入框文字
            string content = inputMessage.text.Trim();
            if (string.IsNullOrEmpty(content)) return;

            // 判断频道
            ChatChannel channel = GetChannelByIndex(currentTabIndex);

            // 系统频道禁止发言
            if(channel == ChatChannel.System)
            {
                MessageBox.Show("系统频道禁止发言");
                return;
            }

            // 私聊频道需要选择私聊对象
            if(channel == ChatChannel.Private && privateTargetId == 0)
            {
                MessageBox.Show("请选择私聊对象");
                return;
            }
        }



    }
}
