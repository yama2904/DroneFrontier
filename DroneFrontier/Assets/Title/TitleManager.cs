using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    void Start()
    {
        
    }

   void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
        }
    }
}
