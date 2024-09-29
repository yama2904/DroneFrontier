using UnityEngine;

public class BaseScreenManager
{
    private const string SCREEN_PATH = "Screen/";   //Resourcesフォルダのパス

    //画面一覧
    public enum Screen
    {
        TITLE,
        GAME_MODE_SELECT,
        HELP,
        CONFIG,
        SOLO_MULTI_SELECT,
        CPU_SELECT,
        WEAPON_SELECT,
        RESULT,

        NONE
    }
    private static int nowScreen;   //表示中の画面

    private static GameObject[] screens = new GameObject[(int)Screen.NONE];
    private static string[] paths = new string[(int)Screen.NONE];   //各画面をロードするパス

    static BaseScreenManager()
    {
        paths[(int)Screen.TITLE] = "TitleScreen";
        paths[(int)Screen.GAME_MODE_SELECT] = "GameModeSelectScreen";
        paths[(int)Screen.HELP] = "HelpScreen";
        paths[(int)Screen.CONFIG] = "ConfigScreen";
        paths[(int)Screen.SOLO_MULTI_SELECT] = "KuribocchiScreen";
        paths[(int)Screen.CPU_SELECT] = "CPUSelectScreen";
        paths[(int)Screen.WEAPON_SELECT] = "WeaponSelectScreen";
        paths[(int)Screen.RESULT] = "ResultScreen";

        nowScreen = (int)Screen.NONE;
    }

    //画面をロードする
    public static void LoadScreen(Screen screen)
    {
        screens[(int)screen] = GameObject.Instantiate(Resources.Load(SCREEN_PATH + paths[(int)screen])) as GameObject;  //Resourcesフォルダからロード
        screens[(int)screen].SetActive(false);  //一旦非表示
    }

    //画面を表示する
    public static void SetScreen(Screen next)
    {
        //今表示している画面を非表示
        HideScreen();

        //指定された画面を表示
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
