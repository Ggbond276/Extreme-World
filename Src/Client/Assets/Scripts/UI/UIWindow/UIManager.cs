using Assets.Scripts.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    class UIElement
    {
        public string Resources;
        public bool Cache;
        public GameObject Instance;
    }
    // 1.Type是什么
    private Dictionary<Type, UIElement> UIResources = new Dictionary<Type, UIElement>();

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
    }

    // 这里应该是一个上波浪号 2.析构函数到底是什么
    ~UIManager()
    {
        // 这里写销毁前的逻辑
    }

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
