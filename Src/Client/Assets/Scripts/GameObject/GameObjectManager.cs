using Assets.Scripts.Managers;
using Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectManager : MonoSingleton<GameObjectManager>
{
    // 创建字典用来管理界面中所有的GameObject
    Dictionary<int, GameObject> Characters = new Dictionary<int, GameObject>();
    protected override void OnStart()
    {
        StartCoroutine(InitGameObjects());
        CharacterManager.Instance.OnCharacterEnter += OnCharacterEnter;
        CharacterManager.Instance.OnCharacterLeave += OnCharacterLeave;
    }
    private void OnDestroy()
    {
            CharacterManager.Instance.OnCharacterEnter -= OnCharacterEnter;
            CharacterManager.Instance.OnCharacterLeave -= OnCharacterLeave;
    }
    void Update()
    {

    }

    /// <summary>
    /// 异步初始化场景中已有的游戏物体。
    /// 触发时机：通常在刚切完地图，数据层已经有了角色列表，但表现层（画面）还没生成模型时调用。
    /// </summary>
    IEnumerator InitGameObjects()
    {
        foreach (var cha in CharacterManager.Instance.Characters.Values)
        {
            CreateCharacterObject(cha);
            yield return null;
        }   
    }

    /// <summary>
    /// 实体表现层工厂方法：负责从硬盘拉取模型资源并实例化。
    /// 调试重点：如果怪物模型没加载出来，或者报资源找不到的错，重点查这里的 Resource 路径和 obj。
    /// </summary>
    /// <param name="character">包含配置表 TID 和资源路径的逻辑层实体数据</param>
    private void CreateCharacterObject(Character character)
    {
        // --- 第一板块：安全准入与资源准备 ---
        // 1.如果我们的字典中不存在当前要生成的角色
        if (!Characters.ContainsKey(character.entityId) || Characters[character.entityId] == null)
        {
            // 2.拉取模型资源
            Object obj = Resloader.Load<Object>(character.Define.Resource);

            // 3.判断是否拉取到了模型资源
            if (obj == null)
            {
                // 4.如果没有拉取到模型资源就要打印报错
                Debug.LogErrorFormat("Character[{0}] Resource[{1}] not existed.", character.Define.TID, character.Define.Resource);
                return;
            }

            // --- 第二板块：肉体生成与时空定位 ---（完成这一步之后 我们的肉体就呈现在屏幕中了）
            // 1.根据拉取的模型资源生成GameObject
            GameObject go = (GameObject)Instantiate(obj, this.transform);
            // 4.将这个物体加入字典
            Characters[character.entityId] = go;
            // --- 第五板块：环境交互（UI表现） ---
            UIWorldElementManager.Instance.AddCharacterNameBar(go.transform, character);
        }

        this.InitGameObject(Characters[character.entityId], character);

    }

    /// <summary>
    /// 实体组件装配车间：负责为刚刚生成的 3D 模型注入灵魂（设置坐标、挂载控制脚本、分配权限）。
    /// 调试重点：如果怪物刷出来后不动、位置错乱，或者玩家操控了怪物，重点排查此处的权限分配 (pc.enabled)。
    /// </summary>
    /// <param name="go">刚刚实例化出来的 3D 模型 GameObject</param>
    /// <param name="character">服务端发来的该实体的数据对象</param>
    private void InitGameObject(GameObject go, Character character)
    {
        // 2.给予GameObject名字 方便调试
        go.name = "Character_" + character.entityId + "_" + character.Info.Name;
        // 3.给GameObject设置位置和方向
        go.transform.position = GameObjectTool.LogicToWorld(character.position);
        go.transform.forward = GameObjectTool.LogicToWorld(character.direction);

        // --- 第三板块：灵魂绑定（注入表现层驱动） ---
        EntityController ec = go.GetComponent<EntityController>();
        if (ec != null)
        {
            ec.entity = character;
            ec.isPlayer = character.IsPlayer;
        }

        // --- 第四板块：权限分配（区分主角与路人） ---
        PlayerInputController pc = go.GetComponent<PlayerInputController>();
        if (character.Info.Id == Models.User.Instance.CurrentCharacter.Id)
        {
            Models.User.Instance.CurrentCharacterObject = go;
            // 第二次进入的问题是这里的主摄像机实体为空
            MainPlayerCamera.Instance.player = go;

            pc.enabled = true;
            pc.character = character;
            pc.entityController = ec;
        }
        else
        {
            if (pc != null)
            {
                pc.enabled = false;
            }
        }
    }

    /// <summary>
    /// 响应事件：当有新的角色/怪物进入玩家的视野范围（或刷新）时触发。
    /// </summary>
    /// <param name="cha">新进入的实体数据</param>
    void OnCharacterEnter(Character cha)
    {
        CreateCharacterObject(cha);
    }

    /// <summary>
    /// 响应事件：当角色/怪物离开视野，或者怪物死亡时触发。
    /// 主要职责：销毁 3D 模型，释放内存，并将其从管理字典中剔除。
    /// </summary>
    /// <param name="cha">要离开的实体数据</param>
    void OnCharacterLeave(Character cha)
    {
        Debug.LogFormat("OnCharacterLeave()");
        if (!Characters.ContainsKey(cha.entityId))
            return;
        if (Characters[cha.entityId] != null)
        {
            Destroy(Characters[cha.entityId]);
            this.Characters.Remove(cha.entityId);
        }
    }
}
