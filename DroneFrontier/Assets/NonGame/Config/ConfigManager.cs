using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    void Start()
    {
        
    }
    
    void Update()
    {
    }

    //設定を初期化する
    public static void InitConfig()
    {
        SoundManager.BaseBGMVolume = 1.0f;
        SoundManager.BaseSEVolume = 1.0f;
        BrightnessManager.BaseAlfa = 0;
        CameraManager.BaseSpeed = 1.0f;
    }
}
