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
    IEnumerator InitGameObjects()
    {
        foreach (var cha in CharacterManager.Instance.Characters.Values)
        {
            CrearteCharacterObject(cha);
            yield return null;
        }   
    }
    private void CrearteCharacterObject(Character character)
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
            MainPlayerCamera.Instance.player = go;

            pc.enabled = true;
            pc.character = character;
            pc.entityController = ec;
        }
        else
        {
            pc.enabled = false;
        }
    }
    void OnCharacterEnter(Character cha)
    {
        CrearteCharacterObject(cha);
    }
    // 这里的逻辑会对角色进行销毁操作
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
