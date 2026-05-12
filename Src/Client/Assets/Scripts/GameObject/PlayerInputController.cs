using Assets.Scripts.Services;
using Entities;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    // 这个角色要从哪里获取
    public Character character;
    public EntityController entityController;
    CharacterState state;

    public Rigidbody rb;

    public float rotateSpeed = 2.0f;
    public float turnAngle = 50;

    public int speed;

    // 暂时不知道啥作用的属性
    public bool onAir = false;
    //float lastSync = 0;


    //（这里的start是为了测试的场景写的）
    void Start()
    {
        // 设置角色出生的状态
        state = SkillBridge.Message.CharacterState.Idle;
        // 如果当前角色信息为空
        if (this.character == null)
        {
            DataManager.Instance.Load();
            NCharacterInfo cinfo = new NCharacterInfo();
            cinfo.Id = 1;
            cinfo.Name = "Test";
            cinfo.ConfigId = 1;
            cinfo.Entity = new NEntity();
            cinfo.Entity.Position = new NVector3();
            cinfo.Entity.Direction = new NVector3();
            cinfo.Entity.Direction.X = 0;
            cinfo.Entity.Direction.Y = 100;
            cinfo.Entity.Direction.Z = 0;
            this.character = new Character(cinfo);

            if (entityController != null) entityController.entity = this.character;
        }
    }
    void FixedUpdate()
    {
        if (character == null)
            return;

        // 1.获取垂直输入（WS）
        float v = Input.GetAxis("Vertical");
        // 2. v > 0 游戏对象目前要向前运动
        if (v > 0.01)
        {
            if (state != CharacterState.Move)
            {
                // 移动同步逻辑
                state = CharacterState.Move;
                character.MoveForward();
                this.SendEntityEvent(EntityEvent.MoveFwd);
            }
            Vector3 verticalVelocity = rb.velocity.y * Vector3.up;
            Vector3 dir = GameObjectTool.LogicToWorld(character.direction);
            float speed = (character.speed + 9.81f) / 100f;
            Vector3 horizontalVelocity = speed * dir;
            this.rb.velocity = verticalVelocity + horizontalVelocity;
        } else if (v < -0.01)
        {
            if (state != CharacterState.Move)
            {
                state = CharacterState.Move;
                // 修改实体速度
                character.MoveBack();
                // 动画播放逻辑
                this.SendEntityEvent(EntityEvent.MoveBack);
            }
            // 这些是在计算表现层刚体的速度 不用管 
            Vector3 VerticalVelocity = rb.velocity.y * Vector3.up;
            Vector3 dir = GameObjectTool.LogicToWorld(character.direction);
            float speed = (character.speed + 9.81f) / 100f;
            Vector3 horizontalVelocity = speed * dir;
            this.rb.velocity = VerticalVelocity + horizontalVelocity;
        } else
        {
            if (state != CharacterState.Idle)
            {
                state = CharacterState.Idle;
                this.rb.velocity = Vector3.zero;
                this.character.Stop();
                this.SendEntityEvent(EntityEvent.Idle);
            }
        }

        // 1.检测空格输入
        if (Input.GetButtonDown("Jump"))
        {
            // 执行动画播放逻辑 但是跳跃不是会产生垂直的速度变化吗 为什么没有写出来
            this.SendEntityEvent(EntityEvent.Jump);
        }

        // 1.获取水平输入（AD）
        float h = Input.GetAxis("Horizontal");
        // 2.只有玩家确实在按按键 也就是在推按键时（死区大于0.1）才执行转向的逻辑
        if (h < -0.1 || h > 0.1)
        {
            // 3.先改变表现层 让Unity中的人物模型立即发生转向
            this.transform.Rotate(0, h * rotateSpeed, 0);

            // 4.计算数据层和表现层的角度偏差
            Vector3 dir = GameObjectTool.LogicToWorld(character.direction);
            Quaternion rot = new Quaternion();
            rot.SetFromToRotation(dir, this.transform.forward);
            // 5.判断是否需要进行数据层和逻辑层的数据同步 为了防止轻微抖动就向服务器发包
            if (rot.eulerAngles.y > this.turnAngle && rot.eulerAngles.y < (360 - this.turnAngle))
            {
                // 6.更改逻辑层数据
                character.SetDirection(GameObjectTool.WorldToLogic(this.transform.forward));
                // 7.改变刚体的朝向数据
                rb.transform.forward = this.transform.forward;
                // 8.发送一个空事件 为了触发网络层 让网络层将新的朝向数据通知给其他的在线玩家
                this.SendEntityEvent(EntityEvent.None);
            }
        }
    }
    Vector3 lastPos;
    void LateUpdate()
    {
        // 1.如果角色还没有初始化就直接跳过
        if (this.character == null)
            return;

        // 2.计算当前帧 相对于 上一帧跑了多远
        Vector3 offset = this.rb.transform.position - this.lastPos;
        // 3.根据位置偏差计算当前速度(这个速度是提供给逻辑层的 所以要换算成以cm为单位)
        this.speed = (int)(offset.magnitude * 100f / Time.deltaTime);
        // 4.记录当前帧位置
        this.lastPos = this.rb.transform.position;


        // 5.如果逻辑位置和物理位置存在巨大的偏差 就要强行纠正逻辑位置50cm
        if((GameObjectTool.WorldToLogic(this.rb.transform.position) - this.character.position).magnitude > 50)
        {
            this.character.SetPosition(GameObjectTool.WorldToLogic(this.rb.transform.position));
            this.SendEntityEvent(EntityEvent.None);
        }

        // 6.确保刚体和模型位置始终是在一起的
        this.transform.position = this.rb.transform.position;
    }
    void SendEntityEvent(EntityEvent entityEvent)
    {
        if (entityController != null)
            entityController.OnEntityEvent(entityEvent);
        MapService.Instance.SendMapEntitySync(entityEvent, this.character.EntityData);
    }

}
