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

        NONE
    }
    static int nowScreen;

    static GameObject[] screens;

    void Start()
    {
        screens = new GameObject[(int)Screen.NONE];

        string[] paths = new string[(int)Screen.NONE];
        paths[(int)Screen.TITLE] = "TitleScreen";
        paths[(int)Screen.GAME_MODE_SELECT] = "GameModeSelectScreen";
        for (int i = 0; i < (int)Screen.NONE; i++)
        {
            screens[i] = GameObject.Instantiate(Resources.Load(SCREEN_PATH + paths[i])) as GameObject;
            screens[i].SetActive(false);                
        }

        nowScreen = (int)Screen.TITLE;
        screens[nowScreen].SetActive(true);
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
}
