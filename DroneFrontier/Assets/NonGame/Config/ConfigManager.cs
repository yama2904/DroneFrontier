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
        SoundManager.SetBaseVolumeBGM(1);
        SoundManager.SetBaseVolumeSE(1);
        BrightnessManager.SetBaseAlfa(0);
        CameraManager.SetBaseSpeed(1);
    }
}
