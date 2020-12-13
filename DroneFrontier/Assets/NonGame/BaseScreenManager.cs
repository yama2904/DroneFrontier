using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseScreenManager
{
    const string SCREEN_PATH = "Screen/";

    public enum Screen
    {
        TITLE,
        GAME_MODE_SELECT,
        HELP,
        CONFIG,
        KURIBOCCHI,
        CPU_SELECT,
        WEAPON_SELECT,
        MATCHING,
        RESULT,

        NONE
    }
    static int nowScreen;

    static GameObject[] screens = new GameObject[(int)Screen.NONE];
    static string[] paths = new string[(int)Screen.NONE];

    static BaseScreenManager()
    {
        paths[(int)Screen.TITLE] = "TitleScreen";
        paths[(int)Screen.GAME_MODE_SELECT] = "GameModeSelectScreen";
        paths[(int)Screen.HELP] = "HelpScreen";
        paths[(int)Screen.CONFIG] = "ConfigScreen";
        paths[(int)Screen.KURIBOCCHI] = "KuribocchiScreen";
        paths[(int)Screen.CPU_SELECT] = "CPUSelectScreen";
        paths[(int)Screen.WEAPON_SELECT] = "WeaponSelectScreen";
        paths[(int)Screen.MATCHING] = "MatchingScreen";
        paths[(int)Screen.RESULT] = "ResultScreen";

        nowScreen = (int)Screen.NONE;
    }

    //画面をロードする
    public static void LoadScreen(Screen screen)
    {
        screens[(int)screen] = GameObject.Instantiate(Resources.Load(SCREEN_PATH + paths[(int)screen])) as GameObject;
        screens[(int)screen].SetActive(false);
    }

    //画面を表示する
    public static void SetScreen(Screen next)
    {
        HideScreen();

        screens[(int)next].SetActive(true);
        nowScreen = (int)next;
    }

    //画面を非表示にする
    public static void HideScreen()
    {
        if (nowScreen < (int)Screen.NONE)
        {
            if (screens[nowScreen] != null)
            {
                screens[nowScreen].SetActive(false);
            }
        }
    }
}
