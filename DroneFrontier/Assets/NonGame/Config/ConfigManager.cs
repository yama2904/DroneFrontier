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
        SoundManager.SettingVolumeBGM = 1.0f;
        SoundManager.SettingVolumeSE = 1.0f;
        BrightnessManager.SetBaseAlfa(0);
        CameraManager.BaseSpeed = 1.0f;
    }
}
