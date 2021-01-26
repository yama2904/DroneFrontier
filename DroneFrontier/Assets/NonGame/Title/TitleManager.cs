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
            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            BaseScreenManager.SetScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
        }
    }
}
