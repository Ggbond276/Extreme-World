using Common.Data;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    public int ID;
    private bool inInteractive = false;
    private NpcDefine npc;
    private Animator anim;
    SkinnedMeshRenderer myrenderder;
    Color orignColor;

    void Start()
    {
        npc = NPCManager.Instance.GetNPCDefine(ID);
        anim = this.gameObject.GetComponent<Animator>();
        myrenderder = this.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        orignColor = myrenderder.sharedMaterial.color;
        StartCoroutine(DoAction());
    }

    IEnumerator DoAction()
    {
        while(true)
        {
            if (inInteractive)
                yield return new WaitForSeconds(3f);
            else
                yield return new WaitForSeconds(Random.Range(5f, 10f));
            this.Relax();
        }
    }

    void Relax()
    {
        this.anim.SetTrigger("Relax");
    }
    void Talk()
    {
        this.anim.SetTrigger("Talk");
    }

    void Interactive()
    {
        if(!inInteractive)
        {
            this.inInteractive = true;
            StartCoroutine(DoInteractive());
        }
    }

    IEnumerator DoInteractive()
    {
        // 面向玩家
        yield return FaceToPlayer();
        // 将交互逻辑移交给Manager去管理
        if(NPCManager.Instance.Interactive(ID))
        {
            // 进行动画播放
            this.Talk();
        }
        yield return new WaitForSeconds(3f);
        this.inInteractive = false;
    }
    // 面向玩家的逻辑(朝向逻辑的执行好像有问题）
    IEnumerator FaceToPlayer()
    {
        // 具体的面向玩家的逻辑
        Vector3 faceto = (User.Instance.CurrentCharacterObject.transform.position - this.transform.position).normalized;
        if(Mathf.Abs(Vector3.Angle(this.transform.forward, faceto)) > 5)
        {
            Debug.LogError("朝向逻辑执行");
            this.transform.forward = Vector3.Lerp(this.transform.forward, faceto, Time.deltaTime * 5f);
            yield return null;
        }
    }
    void OnMouseDown()
    {
        // 执行交互逻辑
        this.Interactive();
    }

    private void OnMouseOver()
    {
        // 鼠标离开取消高亮
        //Debug.LogError("高亮执行结束");
        this.Highlight(true);
    }

    private void OnMouseEnter()
    {
        // 鼠标进入显示高亮
        Debug.LogError("高亮执行开始");
        this.Highlight(true);
    }

     void Highlight(bool highlight)
    {
        // 高亮逻辑
        if(highlight)
        {
            if (myrenderder.sharedMaterial.color != Color.white)
                myrenderder.sharedMaterial.color = Color.white;
         } else
        {
            if (myrenderder.sharedMaterial.color != orignColor)
                myrenderder.sharedMaterial.color = orignColor;
        }

    }
}
