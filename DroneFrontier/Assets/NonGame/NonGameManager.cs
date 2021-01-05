using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NonGameManager : MonoBehaviour
{
    static bool isStart = false;
    static List<MainGameManager.PlayerData> playerDatas = new List<MainGameManager.PlayerData>();

    void Start()
    {
        if (!isStart)
        {
            ConfigManager.InitConfig();
            MainGameManager.IsMulti = false;
        }
        isStart = true;
        playerDatas.Clear();

        //全ての画面のロード
        for (int screen = 0; screen < (int)BaseScreenManager.Screen.NONE; screen++)
        {
            BaseScreenManager.LoadScreen((BaseScreenManager.Screen)screen);
        }

        BaseScreenManager.SetScreen(BaseScreenManager.Screen.TITLE);
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
}
