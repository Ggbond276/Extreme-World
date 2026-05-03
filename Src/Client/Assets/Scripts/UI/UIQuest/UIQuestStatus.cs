using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static QuestManager;

public class UIQuestStatus : MonoBehaviour
{
    public Image[] statusImages;
    public void SetQuestStatus(NpcQuestStatus status)
    {
        for(int i = 0; i < statusImages.Length; i++)
        {
            if(statusImages[i] != null)
            {
                statusImages[i].gameObject.SetActive(i == (int)status);
            }
        }
    }
}
