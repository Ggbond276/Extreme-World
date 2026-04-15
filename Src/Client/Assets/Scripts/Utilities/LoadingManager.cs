using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

using SkillBridge.Message;
using ProtoBuf;
using Services;
using Assets.Scripts.Services;
using Managers;

public class LoadingManager : MonoBehaviour
{
    //使用public是为了方便在面板中获取组件
    public GameObject UITips;
    public GameObject UILoading;
    public GameObject UILogin;

    public Slider progressBar;
    public Text progressText;
    public Text progressNumber;
    IEnumerator Start()
    {
        #region  1.初始化日志 并打印相关日志
        //Code related to log printing initialization
        log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net.xml"));
        UnityLogger.Init();
        Common.Log.Init("Unity");
        Common.Log.Info("LoadingManager start");
        #endregion

        #region 2.启动一系列界面
        //Display GameTips
        UITips.SetActive(true);
        //Do not display the LoadingPanel
        UILoading.SetActive(false);
        //Do not display the LoginPanel
        UILogin.SetActive(false);
        yield return new WaitForSeconds(2f);
        //Display Loading progress bar
        UILoading.SetActive(true);
        yield return new WaitForSeconds(1f);
        UITips.SetActive(false);
        #endregion

        #region 3.使用DataManager加载数据
        //Get the Loading data and return to caller
        yield return DataManager.Instance.LoadData();
        #endregion

        #region 4.启动UserService 
        //perfrom initialization operations related to user service functions
        MapService.Instance.Init();
        UserService.Instance.Init();
        TestManager.Instance.Init();
        #endregion

        #region 5.进度条加载
        //Fake Loading Simulate
        for (float i = 0 ; i < 100;)
        {
            i += Random.Range(0.1f, 1.5f);
            progressBar.value = i;
            yield return new WaitForEndOfFrame();
        }
        #endregion

        #region 6.打开登录界面
        UILoading.SetActive(false);
        UILogin.SetActive(true);
        yield return null;
        #endregion
    }


    void Update()
    {

    }
}
