using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseScreenManager : MonoBehaviour
{
    const string SCREEN_PATH = "Screen/";

    public enum Screen
    {
        TITLE,
        GAME_MODE_SELECT,
        HELP,
        CONFIG,

        NONE
    }
    static int nowScreen;
    static GameObject[] screens;
    static bool isStart = false;

    void Start()
    {
        screens = new GameObject[(int)Screen.NONE];

        string[] paths = new string[(int)Screen.NONE];
        paths[(int)Screen.TITLE] = "TitleScreen";
        paths[(int)Screen.GAME_MODE_SELECT] = "GameModeSelectScreen";
        paths[(int)Screen.HELP] = "HelpScreen";
        paths[(int)Screen.CONFIG] = "ConfigScreen";
        for (int i = 0; i < (int)Screen.NONE; i++)
        {
            screens[i] = GameObject.Instantiate(Resources.Load(SCREEN_PATH + paths[i])) as GameObject;
            screens[i].SetActive(false);                
        }

        nowScreen = (int)Screen.TITLE;
        screens[nowScreen].SetActive(true);

        if (!isStart)
        {
            InitConfig();
        }
    }

    void Update()
    {
        
    }

    public static void SetNextScreen(Screen next)
    {
        screens[nowScreen].SetActive(false);

        screens[(int)next].SetActive(true);
        nowScreen = (int)next;        
    }

    public static void InitConfig()
    {
        SoundManager.SetBaseVolumeBGM(1);
        SoundManager.SetBaseVolumeSE(1);
        BrightnessManager.SetBaseAlfa(0);
        CameraManager.SetBaseSpeed(1);
    }
}
