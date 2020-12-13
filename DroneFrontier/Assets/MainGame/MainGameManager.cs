using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameManager : MonoBehaviour
{
    //マルチモードか
    public static bool IsMulti { get; set; } = false;

    //アイテムを出現させるか
    public static bool IsItem { get; set; } = true;

    //メインゲーム中か
    public static bool isMainGaming { get; private set; } = false;

    //ゲームモード
    public enum GameMode
    {
        BATTLE,
        RACE,

        NONE
    }
    public static GameMode Mode { get; set; } = GameMode.NONE;

    [SerializeField] GameObject screenMask = null;
    bool isConfig;

    void Start()
    {
        isMainGaming = true;
        BaseScreenManager.LoadScreen(BaseScreenManager.Screen.CONFIG);
        isConfig = false;
        screenMask.SetActive(false);
    }
    
    void Update()
    {
        //設定画面を開く
        if (Input.GetKeyDown(KeyCode.M))
        {
            MoveConfig();
        }
    }

    public static void LoadMainGameScene()
    {
        SceneManager.LoadScene("MainGameScene");
    }

    void MoveConfig()
    {
        if (isConfig)
        {
            screenMask.SetActive(false);
            BaseScreenManager.HideScreen();

            isConfig = false;
        }
        else
        {
            screenMask.SetActive(true);
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.CONFIG);

            isConfig = true;
        }
    }
}
