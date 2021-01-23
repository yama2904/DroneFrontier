using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NonGameManager : MonoBehaviour
{
    static List<MainGameManager.PlayerData> playerDatas = new List<MainGameManager.PlayerData>();
    static BaseScreenManager.Screen startScreen = BaseScreenManager.Screen.TITLE;
    static bool isStarted = false;

    void Start()
    {
        if (!isStarted)
        {
            ConfigManager.InitConfig();
            MainGameManager.IsMulti = false;
        }
        isStarted = true;
        playerDatas.Clear();

        //全ての画面のロード
        for (int screen = 0; screen < (int)BaseScreenManager.Screen.NONE; screen++)
        {
            BaseScreenManager.LoadScreen((BaseScreenManager.Screen)screen);
        }

        if(startScreen == BaseScreenManager.Screen.NONE)
        {
            startScreen = BaseScreenManager.Screen.TITLE;
        }
        BaseScreenManager.SetScreen(startScreen);
    }

    void Update()
    {
    }

    //プレイヤー又はCPUを追加する
    public static void SetPlayer(string name, BaseWeapon.Weapon weapon, bool isPlayer)
    {
        MainGameManager.PlayerData pd = new MainGameManager.PlayerData();
        pd.name = name;
        pd.weapon = weapon;
        pd.isPlayer = isPlayer;

        playerDatas.Add(pd);
    }

    public static void ClearSetedPlayers()
    {
        playerDatas.Clear();
    }

    public static void LoadMainGameScene()
    {
        foreach (MainGameManager.PlayerData pd in playerDatas)
        {
            MainGameManager.SetPlayer(pd.name, pd.weapon, pd.isPlayer);
        }
        SceneManager.LoadScene("MainGameScene");
    }

    public static void LoadNonGameScene(BaseScreenManager.Screen startScreen)
    {
        NonGameManager.startScreen = startScreen;
        SceneManager.LoadScene("NonGameScene");
    }
}
