using Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static QuestManager;

public class UIWorldElementManager : MonoSingleton<UIWorldElementManager>
{
	public GameObject infoBarPrefab;
	public Dictionary<Transform, GameObject> elements = new Dictionary<Transform, GameObject>();

	public GameObject questStautsPrefab;
	public Dictionary<Transform, GameObject> questElement = new Dictionary<Transform, GameObject>();
    protected override void OnStart()
    {
       
    }
	public void AddCharacterNameBar(Transform owner, Character character)
    {
		GameObject goInfoBar = Instantiate(infoBarPrefab, this.transform);
		goInfoBar.name = "NameBar" + character.Info.Id;
		goInfoBar.GetComponent<UIWorldElement>().owner = owner;
		goInfoBar.GetComponent<UINameBar>().character = character;
		this.elements[owner] = goInfoBar;
    }
	public void RemoveCharacterNameBar(Transform owner)
    {
		if(this.elements.ContainsKey(owner))
        {
			Destroy(this.elements[owner]);
			this.elements.Remove(owner);
        }
    }

	public void AddNpcQuestStatus(Transform owner, NpcQuestStatus status)
    {
        if(status == NpcQuestStatus.None)
        {
            RemoveNpcQuestStatus(owner);
            return;
        }
		if(this.questElement.TryGetValue(owner, out GameObject go))
        {
            // 预制体存在 就刷新
            go.GetComponent<UIQuestStatus>().SetQuestStatus(status);
        } else
        {
            // 预制体不存在就 添加
            go = Instantiate(questStautsPrefab, this.transform);
            go.name = "QuestStatus_" + owner.name;
            go.GetComponent<UIWorldElement>().owner = owner;
            go.GetComponent<UIQuestStatus>().SetQuestStatus(status);
            this.questElement[owner] = go;
        }
    }
    public void RemoveNpcQuestStatus(Transform owner)
    {
        if(this.questElement.ContainsKey(owner))
        {
            Destroy(this.questElement[owner]);
            this.questElement.Remove(owner);
        }
    }
}
