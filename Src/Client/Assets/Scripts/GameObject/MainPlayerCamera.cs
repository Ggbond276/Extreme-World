using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainPlayerCamera : MonoSingleton<MainPlayerCamera>
{
    public Camera mianCamera;
    public Transform viewPoint;
    public GameObject player;

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
    private void LateUpdate()
    {
        if(player == null)
        {
            player = User.Instance.CurrentCharacterObject;
        }
        if (player == null)
            return;

        this.transform.position = player.transform.position;
        this.transform.rotation = player.transform.rotation;
    }
}
