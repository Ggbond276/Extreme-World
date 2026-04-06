using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SkillBridge.Message;
using Services;
using Models;
public class UICharacterSelect : MonoBehaviour
{
    //获取两个面板
    public GameObject SelectPanel;
    public GameObject CreatePanel;
    public UICharacterView characterView;
    public InputField InputPlayerName;
    public Image[] titles;
    public Text[] names;
    public GameObject uiCharInfo;
    public Transform uiCharList;
    public List<GameObject> uiChars = new List<GameObject>();
    private CharacterClass characterClass;
    private int selectCharacterIdx = -1;

    void Start()
    {
        InitSelectCharacter(true);
        UserService.Instance.OnCharacterCreate += OnCharacterCreate;
    }

    void OnDestroy()
    {
        UserService.Instance.OnCharacterCreate -= OnCharacterCreate;
    }

    //初始化角色选择界面
    public void InitSelectCharacter(bool init)
    {
        SelectPanel.SetActive(true);
        CreatePanel.SetActive(false);

        if (init)
        {
            foreach (var old in uiChars)
            {
                Destroy(old);
            }
            uiChars.Clear();

            //这段代码用于根据用户的角色信息创建可选择的角色UI展示项及相关点击事件处理

            //循环遍历User.Instance.Info.Player.Characters中的每一个元素
            //User.Instance.Info.Player.Characters是存储用户角色信息的集合
            for (int i = 0; i < User.Instance.Info.Player.Characters.Count; i++)
            {
                //创建一个用于展示角色信息的UI元素实例
                //实例化一个名为uiCharInfo的对象
                GameObject CharInfo = Instantiate(uiCharInfo, this.uiCharList);
                //获取刚实例化的UICharInfo组件
                UICharInfo UICharInfo = CharInfo.GetComponent<UICharInfo>();
                //将User.Instance.Info.Player.Characters中当前索引对应的角色信息赋值给charInfo的info属性
                UICharInfo.info = User.Instance.Info.Player.Characters[i];
                //获取刚实例化的游戏对象的button组件
                Button button = CharInfo.GetComponent<Button>();
                //记录当前循环的索引
                int index = i;
                //当按钮点击事件会调用OnClickSelectCharacater方法 并传入索引值
                button.onClick.AddListener(() =>
                {
                    OnClickSelectCharacater(index);
                });
                //将实例化对象添加到uiChars中
                uiChars.Add(CharInfo);
                //设置实例化对象为可见
                CharInfo.SetActive(true);
            }
        }
    }
    //在角色选择界面点击选择创建新角色
    public void OnClickCreateCharacter()
    {
        SelectPanel.SetActive(false);
        CreatePanel.SetActive(true);
        characterView.CurrentCharacter = 1;
    }
    //在角色选择界面中点击选择角色
    public void OnClickSelectCharacater(int index)
    {
        //获取选中角色的下标
        this.selectCharacterIdx = index;
        //通过下标获取到对应的玩家信息
        SkillBridge.Message.NCharacterInfo cha = User.Instance.Info.Player.Characters[index];
        //在日志中打印出玩家信息
        Debug.LogFormat("Select Char: [{0}]{1}[{2}]", cha.Id, cha.Name, cha.Class);
        //设置当前的角色是cha
        User.Instance.CurrentCharacter = cha;
        //显示对应的3D角色试图
        characterView.CurrentCharacter = (int)cha.Class - 1 ;
    }
    //在角色创建界面点击进入游戏
    public void OnClickEnterGame()
    {
        //判断有没有选择角色
        if (selectCharacterIdx >= 0)
        {
            //发送角色进入游戏请求给客户端    
            UserService.Instance.SendGameEnter(selectCharacterIdx);
        }
    }
    //在角色创建界面点击返回按钮
    public void OnClickBack()
    {
        SelectPanel.SetActive(true);
        CreatePanel.SetActive(false);
    }
    //在角色创建界面点击选择职业
    public void OnClickSelectCareer(int index)
    {
        characterClass = (CharacterClass)index;
        //显示选择到的角色 不显示没选择的角色
        characterView.CurrentCharacter = index - 1;
        //显示角色的名字和显示角色的概括信息
        for (int i = 0; i < 3; i++)
        {
            if (i == index - 1)
            {
                titles[i].gameObject.SetActive(true);
            }
            else
            {
                titles[i].gameObject.SetActive(false);
            }
            //Data是用于读取表中的数据的
            //names[i].text = DataManager.Instance.Characters[i + 1].Name;
        }
    }
    //在角色创建界面点击创建角色
    public void OnClickCreate()
    {
        if (string.IsNullOrEmpty(this.InputPlayerName.text))
        {
            MessageBox.Show("请输入角色名称");
        }
        UserService.Instance.SendCharacterCreate(InputPlayerName.text, characterClass);
    }
    //点击角色创建之后的回调方法
    public void OnCharacterCreate(Result result, string message)
    {
        if (result == Result.Success)
        {
            InitSelectCharacter(true);
        }
        else
        {
            MessageBox.Show(message, "错误", MessageBoxType.Error);
        }
    }
    public void OnSelectCharacter(int idx)
    {
        this.selectCharacterIdx = idx;
        var cha = User.Instance.Info.Player.Characters[idx];
        Debug.LogFormat("Select Char:[{0}]{1}[{2}]", cha.Id, cha.Name, cha.Class);
        User.Instance.CurrentCharacter = cha;
        characterView.CurrentCharacter = ((int)cha.Class - 1);

        for (int i = 0; i < User.Instance.Info.Player.Characters.Count; i++)
        {
            UICharInfo ci = this.uiChars[i].GetComponent<UICharInfo>();
            ci.Selected = idx == i;
        }
    }
    public void OnClickPlay()
    {
        if (selectCharacterIdx >= 0)
        {
            // 携带选中的角色的id将进入游戏的请求发送给服务端
            UserService.Instance.SendGameEnter(selectCharacterIdx);
        }
    }

}
