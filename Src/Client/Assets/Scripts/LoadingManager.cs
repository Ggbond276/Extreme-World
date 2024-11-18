using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

using SkillBridge.Message;
using ProtoBuf;
using Services;

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
        //Code related to log printing initialization
        log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net.xml"));
        UnityLogger.Init();
        Common.Log.Init("Unity");
        Common.Log.Info("LoadingManager start");

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

        //Get the Loading data and return to caller
        yield return DataManager.Instance.LoadData();

        //perfrom initialization operations related to user service functions
        //UserService.Instance.Init();

        //Fake Loading Simulate
        for (float i = 0 ; i < 100;)
        {
            i += Random.Range(0.1f, 1.5f);
            progressBar.value = i;
            yield return new WaitForEndOfFrame();
        }

        UILoading.SetActive(false);
        UILogin.SetActive(true);
        yield return null;
    }


    void Update()
    {

    }
}
