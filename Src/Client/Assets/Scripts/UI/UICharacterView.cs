using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICharacterView : MonoBehaviour
{
    public GameObject[] Characters;  
    private int currentCharacter = 0;

    public int CurrentCharacter
    {
        get
        {
            return currentCharacter;
        }
        set
        {
            currentCharacter = value;
            this.UpdateCharacter();
        }
    }
    //只显示索引表示的角色
    private void UpdateCharacter()
    {
        for(int i = 0; i < 3; i++)
        {
            if(this.currentCharacter == i)
            {
                this.Characters[i].SetActive(true);
            } else
            {
                this.Characters[i].SetActive(false);
            }
        }
    }
}
