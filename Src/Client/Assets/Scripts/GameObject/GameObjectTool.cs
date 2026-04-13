using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectTool
{

    // 网络传输数据 转 Unity世界数据
   public static Vector3 LogicToWorld(Vector3Int vector)
    {
        return new Vector3(vector.x / 100f, vector.z / 100f, vector.y / 100f);
    }

    public static Vector3 LogicToWorld(NVector3 vector)
    {
        return new Vector3(vector.X / 100f, vector.Z / 100f, vector.Y / 100f);
    }

    public static float LogicToWorld(int val)
    {
        return val / 100f;
    }


    //Unity世界数据 转 网络传输世界数据
    public static Vector3Int WorldToLogic(Vector3 vector)
    {
        return new Vector3Int()
        {
            x = Mathf.RoundToInt(vector.x * 100),
            y = Mathf.RoundToInt(vector.z * 100),
            z = Mathf.RoundToInt(vector.y * 100),
        };
    }

    public static NVector3 WorldToLogicN(Vector3 vector)
    {
        return new NVector3()
        {
            X = Mathf.RoundToInt(vector.x * 100),
            Y = Mathf.RoundToInt(vector.z * 100),
            Z = Mathf.RoundToInt(vector.y * 100)
        };
    }

    public static int WorldToLogic(float val)
    {
        return Mathf.RoundToInt(val * 100f);
    }


    // 判断角色是否发生了移动和而运动
    public static bool EntityUpdate(NEntity entity, Vector3 position, Quaternion rotation, float speed)
    {
        NVector3 pos = WorldToLogicN(position);
        NVector3 dir = WorldToLogicN(rotation.eulerAngles);
        int spd = WorldToLogic(speed);
        bool updated = false;

        if(!entity.Position.Equal(pos))
        {
            entity.Position = pos;
            updated = true;
        }
        if (!entity.Direction.Equal(dir))
        {
            entity.Direction = dir;
            updated = true;
        }
        if (!entity.Speed.Equals(spd))
        {
            entity.Speed = spd;
            updated = true;
        }

        return updated;
    }
}
