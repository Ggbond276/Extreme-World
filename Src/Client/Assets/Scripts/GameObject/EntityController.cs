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
    // 这里每一帧都会调用UpdateTransform实时同步表现层与逻辑层的数据
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
    void OnDestroy()
    {
        if (entity != null)
            Debug.LogFormat("{0} OnDestroy :ID:{1} POS:{2} DIR:{3} SPD:{4} ", this.name, entity.entityId, entity.position, entity.direction, entity.speed);

        if (UIWorldElementManager.Instance != null)
        {
            UIWorldElementManager.Instance.RemoveCharacterNameBar(this.transform);
        }
    }
    // 这个方法不是给玩家角色使用的 而是给非玩家角色使用的
    void updateTransform()
    {
        // 拿到Entity逻辑层的位置和方向数据
        this.position = GameObjectTool.LogicToWorld(entity.position);
        this.direction = GameObjectTool.LogicToWorld(entity.direction);
        // 由于拿到的逻辑层direction是三元数 所以同步实体行为的时候要改成四元数
        if (this.direction != Vector3.zero)
        {
            this.rotation = Quaternion.LookRotation(this.direction);
        }

        this.transform.rotation = this.rotation;
        this.transform.position = this.position;
        this.rb.MovePosition(this.position);

        this.lastPosition = this.position;
        this.lastDirection = this.direction;
        this.lastRotation = this.rotation;
    }
    // 当玩家被删除的时候
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
    // 当触发事件的时候
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

    public void OnEntityChanged(Entity entity)
    {
        Debug.LogFormat("OnEntityChanged : ID : {0} POS : {1} DIR : {2} SPD : {3}", entity.entityId, entity.position, entity.direction, entity.speed);
    }
}
