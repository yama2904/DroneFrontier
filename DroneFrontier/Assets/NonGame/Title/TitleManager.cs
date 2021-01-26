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
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.KURIBOCCHI);
        }
    }
}
