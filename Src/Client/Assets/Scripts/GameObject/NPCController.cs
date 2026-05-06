using Assets.Scripts.Models;
using Common.Data;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static QuestManager;

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
        // 以下两个不是核心逻辑 只是处理高亮逻辑的
        myrenderder = this.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        orignColor = myrenderder.sharedMaterial.color;
        StartCoroutine(DoAction());

        // 状态变化监听
        RefreshNpcStatus();
        QuestManager.Instance.OnQuestStatusChanged += OnQuestStatusChanged;
    }
    void OnDestroy()
    {
        // 1. 注销全局事件监听，超度“幽灵”，防止切换场景时报错
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStatusChanged -= OnQuestStatusChanged;
        }

        // 2. 通知 UI 管家，把挂在我头顶的任务状态 UI 彻底销毁
        if (UIWorldElementManager.Instance != null)
        {
            UIWorldElementManager.Instance.RemoveNpcQuestStatus(this.transform); // 假设你的清理方法叫这个
        }
    }

    /// <summary>
    /// 无交互状态下的动作部分
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// 点击交互逻辑部分
    /// </summary>
    void OnMouseDown()
    {
        // 执行交互逻辑
        this.Interactive();
        // 测试返回状态
        // Debug.LogErrorFormat("[{0}]", (int)QuestManager.Instance.GetQuestStatusByNpc(this.ID));
    }
    void Interactive()
    {
        if(!inInteractive)
        {
            this.inInteractive = true;
            StartCoroutine(DoInteractive());
        }
    }
    IEnumerator FaceToPlayer()
    {
        // 具体的面向玩家的逻辑
        Vector3 faceto = (User.Instance.CurrentCharacterObject.transform.position - this.transform.position).normalized;
        if (Mathf.Abs(Vector3.Angle(this.transform.forward, faceto)) > 5)
        {
            // Debug.LogError("朝向逻辑执行");
            this.transform.forward = Vector3.Lerp(this.transform.forward, faceto, Time.deltaTime * 5f);
            yield return null;
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
    
    /// <summary>
    /// 高亮逻辑部分
    /// </summary>
    private void OnMouseOver()
    {
        // 鼠标离开取消高亮
        //Debug.LogError("高亮执行结束");
        this.Highlight(true);
    }
    private void OnMouseEnter()
    {
        // 鼠标进入显示高亮
        // Debug.LogError("高亮执行开始");
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

    /// <summary>
    /// npc任务状态变化刷新逻辑部分
    /// </summary>
    /// <param name="quest"></param>
    public void OnQuestStatusChanged(Quest quest)
    {
        RefreshNpcStatus();
    }
    public void RefreshNpcStatus()
    {
        NpcQuestStatus status = QuestManager.Instance.GetQuestStatusByNpc(this.ID);
        UIWorldElementManager.Instance.AddNpcQuestStatus(this.transform, status);
    }
}
