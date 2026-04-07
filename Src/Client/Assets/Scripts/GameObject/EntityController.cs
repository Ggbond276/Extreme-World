using Entities;
using Managers;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityController : MonoBehaviour, IEntityNotify
{
    public Animator anim;
    public Rigidbody rb;

    private AnimatorStateInfo animatorBaseInfo;

    public Entity entity;

    public Vector3 position;
    public Vector3 direction;
    public Quaternion rotation;

    public Vector3 lastPosition;
    public Vector3 lastDirection;
    public Quaternion lastRotation;

    public bool isPlayer = false;

    void Start()
    {
        if (entity != null)
        {
            EntityManager.Instance.RegisterEntityChangeNotify(entity.entityId, this);
            this.updateTransform();
        }
        if (!this.isPlayer)
        {
            rb.useGravity = false;
        }
    }

    // 实现接口
    public void OnEntityRemoved()
    {
        // 移除姓名条
        if (UIWorldElementManager.Instance != null)
        {
            UIWorldElementManager.Instance.RemoveCharacterNameBar(this.transform);
        }
        //销毁实体
        Destroy(this.gameObject);
    }
    void updateTransform()
    {
        // ��ȡ�����е����� Ȼ�󽫼����������ݽ�������������
        this.position = GameObjectTool.LogicToWorld(entity.position);
        this.direction = GameObjectTool.LogicToWorld(entity.direction);


        if (this.direction != Vector3.zero)
        {
            this.rotation = Quaternion.LookRotation(this.direction);
        }

        // ������ק��ȥ Ȼ��ı䳯��
        this.rb.MovePosition(this.position);
        this.transform.rotation = this.rotation;

        // Ȼ�󽫵�ǰֵ�������
        this.lastPosition = this.position;
        this.lastDirection = this.direction;
        this.lastRotation = this.rotation;
    }
    void FixedUpdate()
    {
        if (this.entity == null)
            return;

        this.entity.OnUpdate(Time.fixedDeltaTime);

        if (!this.isPlayer)
        {
            this.updateTransform();
        }
    }
    public void OnEntityEvent(EntityEvent entityEvent)
    {
        switch (entityEvent)
        {
            case EntityEvent.Idle:
                anim.SetBool("Move", false);
                anim.SetTrigger("Idle");
                break;
            case EntityEvent.MoveFwd:
                anim.SetBool("Move", true);
                break;
            case EntityEvent.MoveBack:
                anim.SetBool("Move", true);
                break;
            case EntityEvent.Jump:
                anim.SetTrigger("Jump");
                break;
        }
    }
    void OnDestroy()
    {
        if (entity != null)
            Debug.LogFormat("{0} OnDestroy :ID:{1} POS:{2} DIR:{3} SPD:{4} ", this.name, entity.entityId, entity.position, entity.direction, entity.speed);

        if (UIWorldElementManager.Instance != null)
        {
            UIWorldElementManager.Instance.RemoveCharacterNameBar(this.transform);
        }
    }
}
