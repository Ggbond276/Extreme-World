using Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWorldElementManager : MonoSingleton<UIWorldElementManager>
{
	public GameObject infoBarPrefab;
	public Dictionary<Transform, GameObject> elements = new Dictionary<Transform, GameObject>();
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
}
