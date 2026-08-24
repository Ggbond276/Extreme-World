using Assets.Scripts.UI;
using Assets.Scripts.UI.UITeam;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 全局UI管理器，负责管理所有UI面板的加载、显示、缓存和销毁
/// </summary>
public class UIManager : Singleton<UIManager>
{

    /// <summary>
    /// UI 元素的配置信息模型
    /// </summary>
    class UIElement
    {
        public string Resources; // 预制体在 Resources 文件夹下的路径
        public bool Cache; // 关闭时是否缓存（true=隐藏不销毁，false=直接销毁）
        public GameObject Instance; // 实例化出来的 GameObject 引用
    }

    // 字典：以 UI 脚本的类型 (Type) 作为身份证，映射它的配置信息
    private Dictionary<Type, UIElement> UIResources = new Dictionary<Type, UIElement>();


    /// <summary>
    /// 构造函数：在此处注册所有受系统管理的 UI 面板
    /// </summary>
    public UIManager()
    {
        // 这里写构造前的逻辑
        // 用来向字典中添加UI
        this.UIResources.Add(typeof(UITest), new UIElement() { Resources = "UI/UITest", Cache = true });
        this.UIResources.Add(typeof(UIBag), new UIElement() { Resources = "UI/UIBag", Cache = false });
        this.UIResources.Add(typeof(UIShop), new UIElement() { Resources = "UI/UIShop", Cache = false });
        this.UIResources.Add(typeof(UICharEquip), new UIElement() { Resources = "UI/UICharEquip", Cache = false });
        this.UIResources.Add(typeof(UIQuestSystem), new UIElement() { Resources = "UI/UIQuestSystem", Cache = false });
        this.UIResources.Add(typeof(UIQuestDialog), new UIElement() { Resources = "UI/UIQuestDialog", Cache = false });
        // 【注意】你的好友面板必须在这里注册！
        this.UIResources.Add(typeof(UIFriends), new UIElement() { Resources = "UI/UIFriends", Cache = false });
        this.UIResources.Add(typeof(UITeamSystem), new UIElement() { Resources = "UI/UITeamSystem", Cache = false });

        // ==========================================
        // 公会系统 UI 矩阵全量注册
        // 根据 Resources/UI/ 目录下的真实预制体命名严格对应
        // ==========================================

        // 1. 无公会状态：公会入口 / 列表大厅界面
        this.UIResources.Add(typeof(UIGuildEntry), new UIElement() { Resources = "UI/UIGuildEntry", Cache = false });
        this.UIResources.Add(typeof(UIGuildJoin), new UIElement() { Resources = "UI/UIGuildJoin", Cache = false });

        // 2. 创建公会界面 (我们刚拼好的绝美面板)
        this.UIResources.Add(typeof(UIGuildCreate), new UIElement() { Resources = "UI/UIGuildCreate", Cache = false });

        // 3. 有公会状态：公会主界面 (包含信息、成员、审批等切页)
        this.UIResources.Add(typeof(UIGuildMain), new UIElement() { Resources = "UI/UIGuildMain", Cache = false });

        // 4. 成员交互菜单 (点击成员头像弹出的踢人/升职操作面板)
        this.UIResources.Add(typeof(UIGuildPlayerInteract), new UIElement() { Resources = "UI/UIGuildPlayerInteract", Cache = false });
    
}

    /// <summary>
    /// 析构函数：在垃圾回收(GC)清理该对象时调用，用于释放非托管资源或兜底清理
    /// </summary>
    ~UIManager()
    {
        // 这里写销毁前的逻辑
    }


    /// <summary>
    /// 泛型方法：显示指定的 UI 面板
    /// </summary>
    /// <typeparam name="T">要打开的 UI 脚本类型 (必须继承自 UIWindow)</typeparam>
    /// <returns>返回该 UI 脚本的实例</returns>
    public T Show<T>()
    {
        // 1.拿到这个组件的身份证
        Type type = typeof(T);
        // 2.根据身份证确认这个组件是被Manager管理的组件
        if (this.UIResources.ContainsKey(type))
        {
            // 3.根据身份证 获取到这个组件信息  (1)预制体存在在哪里 (2)是否需要缓存 (3)是否已经被实例化了
            UIElement info = this.UIResources[type];
            // 4.如果已经被实例化了 (也就是说这个UI组件已经被实例化了 但是隐藏了 显示出来就好)
            if(info.Instance != null)
            {
                info.Instance.SetActive(true);
            }
            else
            {
                // 5.如果没有被实例化 就要拉取资源获取预制体
                UnityEngine.Object prefab = Resources.Load(info.Resources);
                if(prefab == null)
                {
                    // 6.根据这个T的身份和类型给他一个默认值 如果是int就给他0 如果是GameObject就给他null
                    return default(T);
                }
                // 7.如果预制体存在且资源不为空 我们就进行实例化 也就是这个预制体可以被挂载到屏幕上了
                info.Instance = (GameObject)GameObject.Instantiate(prefab);
            }
            // 8. 返回这个T类所挂载的那个实例
            return info.Instance.GetComponent<T>();
        }
        return default(T);
    }


    /// <summary>
    /// 关闭指定的 UI 面板
    /// </summary>
    /// <param name="type">要关闭的 UI 脚本类型</param>
    public void Close(Type type)
    {
        // 1.根据身份证确认这个组件是被Manager管理的组件
        if (this.UIResources.ContainsKey(type))
        {
            // 获取信息
            UIElement info = this.UIResources[type];
            // 如果需要缓存就隐藏显示就好
            if (info.Cache)
            {
                info.Instance.SetActive(false);
            }
            else
            {
                // 如果不需要缓存就将这实例直接删除 然乎Instance赋予控空
                GameObject.Destroy(info.Instance);
                info.Instance = null;
            }
        }
    }
}
