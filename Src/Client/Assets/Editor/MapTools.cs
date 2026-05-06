using Common.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTools
{
     [MenuItem("Map Tools/Export Teleporters")]
     public static void ExportTeleporters()
    {
        DataManager.Instance.Load();

        Scene current = EditorSceneManager.GetActiveScene();
        string currentScene = current.name;
        if(current.isDirty)
        {
            EditorUtility.DisplayDialog("提示", "请先保存当前场景", "确定");
            return;
        }

        List<TeleporterObject> allTeleporters = new List<TeleporterObject>();

        foreach(var map in  DataManager.Instance.Maps)
        {
            string sceneFile = "Assets/Levels/" + map.Value.Resource + ".unity";
            if(!System.IO.File.Exists(sceneFile))
            {
                Debug.LogWarningFormat("Scene {0} not existed!", sceneFile);
                continue;
            }
            EditorSceneManager.OpenScene(sceneFile, OpenSceneMode.Single);

            TeleporterObject[] teleporters = GameObject.FindObjectsOfType<TeleporterObject>();
            foreach (var teleporter in teleporters)
            {
                if(!DataManager.Instance.Teleporters.ContainsKey(teleporter.ID))
                {
                    EditorUtility.DisplayDialog("错误", string.Format("地图：{0} 中配置的Teleporter：[{1}]中不存在", map.Value.Resource, teleporter.ID), "确定");
                    return;
                }

                TeleporterDefine def = DataManager.Instance.Teleporters[teleporter.ID];

                if(def.MapID != map.Value.ID)
                {
                    EditorUtility.DisplayDialog("错误", string.Format("地图：{0} 中配置的Teleporter：[{1}]  MapID:{2} 错误", map.Value.Resource, teleporter.ID, def.MapID), "确定");
                    return;
                }

                def.Position = GameObjectTool.WorldToLogicN(teleporter.transform.position);
                def.Direction = GameObjectTool.WorldToLogicN(teleporter.transform.forward);
            }
        }

        DataManager.Instance.SaveTeleporters();
        EditorSceneManager.OpenScene("Assets/Levels/" + currentScene + ".unity");
        EditorUtility.DisplayDialog("提示", "传送点导入完成", "确定");
    }

    // 菜单栏魔法标签
    // 加上这个标签后，Unity 顶部的菜单栏会自动多出一个 "Map Tools" 的菜单。
    // 点击里面的 "Export SpawnPoints"，就会执行下面紧挨着的静态方法。
    [MenuItem("Map Tools/Export SpawnPoints")]
    public static void ExportSpawnPoints()
    {
        // 1. 先把本地的数据表加载进内存，确保我们要操作的 DataManager 是最新的
        DataManager.Instance.Load();

        // 2. 获取当前正在编辑的场景（因为等会我们要来回切场景，切完还得回来，所以先记下现在的名字）
        Scene current = EditorSceneManager.GetActiveScene();
        string currentScene = current.name;

        // 安全防呆设计：isDirty 表示场景被修改过但还没按 Ctrl+S 保存
        if (current.isDirty)
        {
            // 弹出一个警告对话框
            EditorUtility.DisplayDialog("提示", "请先保存当前场景", "确定");
            return; // 直接终止代码执行
        }

        // 3. 确保我们要存放数据的字典已经实例化了
        // 这个嵌套字典的结构是：Dictionary<地图ID, Dictionary<刷怪点ID, 刷怪点数据>>
        if (DataManager.Instance.SpawnPoints == null)
            DataManager.Instance.SpawnPoints = new Dictionary<int, Dictionary<int, SpawnPointDefine>>();

        // 4. 开始遍历配置表里的所有地图，准备挨个打开它们的场景
        foreach (var map in DataManager.Instance.Maps)
        {
            // 拼凑出场景文件在项目里的真实路径
            string sceneFile = "Assets/Levels/" + map.Value.Resource + ".unity";

            // 如果路径下没有这个文件，就打印一个黄色的警告，并跳过这张图，继续看下一张
            if (!System.IO.File.Exists(sceneFile))
            {
                Debug.LogWarningFormat("Scene {0} not existed!", sceneFile);
                continue;
            }

            // 自动化核心操作 
            // 强制 Unity 编辑器打开这个场景（OpenSceneMode.Single 表示替换掉当前场景，不叠加）
            EditorSceneManager.OpenScene(sceneFile, OpenSceneMode.Single);

            // 搜刮核心操作
            // 因为现在场景已经切过来了，直接在场景里搜索所有挂载了 SpawnPoint 脚本的物体！
            SpawnPoint[] spawnpoints = GameObject.FindObjectsOfType<SpawnPoint>();

            // 如果字典里还没建这张地图的“文件夹”（内部字典），就建一个
            if (!DataManager.Instance.SpawnPoints.ContainsKey(map.Value.ID))
            {
                DataManager.Instance.SpawnPoints[map.Value.ID] = new Dictionary<int, SpawnPointDefine>();
            }

            // 5. 遍历在这个场景里搜刮到的每一个标记点
            foreach (var sp in spawnpoints)
            {
                // 如果字典里还没有这个 ID 的坑位，就 new 一个数据模型准备存数据
                if (!DataManager.Instance.SpawnPoints[map.Value.ID].ContainsKey(sp.ID))
                {
                    DataManager.Instance.SpawnPoints[map.Value.ID][sp.ID] = new SpawnPointDefine();
                }

                // 获取数据模型引用，开始疯狂赋值
                SpawnPointDefine def = DataManager.Instance.SpawnPoints[map.Value.ID][sp.ID];
                def.ID = sp.ID;
                def.MapID = map.Value.ID;

                //  坐标系转换 
                // Unity 是左手坐标系，而游戏逻辑（寻路等）通常用逻辑坐标系。
                // 调用 GameObjectTool.WorldToLogicN 把 Vector3 转换成服务器能看懂的 NVector3
                def.Position = GameObjectTool.WorldToLogicN(sp.transform.position);
                def.Direction = GameObjectTool.WorldToLogicN(sp.transform.forward);
            }
        }

        // 6. 收尾工作
        // 数据搜刮完了，调用 Save 方法把内存里的字典序列化写进硬盘文件里
        DataManager.Instance.SaveSpawnPoints();

        // 完事之后，把一开始记录的那个老场景重新打开，造成一种“我没动过”的假象
        EditorSceneManager.OpenScene("Assets/Levels/" + currentScene + ".unity");

        // 弹个窗告诉策划：活干完了！
        EditorUtility.DisplayDialog("提示", "刷怪点导出完成", "确定");
    }
}
