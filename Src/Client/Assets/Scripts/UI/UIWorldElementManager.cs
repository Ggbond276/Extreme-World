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
        // 这里的 Instance 永远指向那个“最先被发现的、唯一的真身” (Manager_A)
        // 这里的 this 指的是“当前正在运行这段代码的脚本实例”

        if (Instance != this)
        {
            // 如果我（this，也就是 Manager_B）发现老大（Instance）不是我
            // 说明我是多余的副本，我得消失，否则场景里就有两个管家在打架
            Destroy(this.gameObject);
            return; // 后面的逻辑不再执行，避免重复初始化
        }

        // 如果代码能运行到这里，说明 Instance == this
        // 我就是那个唯一的真身，可以开始干活了
        base.OnStart();
    }
    void Update () {
		
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
