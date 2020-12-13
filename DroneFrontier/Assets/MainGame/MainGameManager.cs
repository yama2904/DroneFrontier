using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainGameManager : MonoBehaviour
{
    //マルチモードか
    public static bool IsMulti { get; set; } = false;

    //アイテムを出現させるか
    public static bool IsItem { get; set; } = true;

    //メインゲーム中か
    public static bool IsMainGaming { get; private set; } = false;

    //設定画面を開いているか
    public static bool IsConfig { get; private set; } = false;

    //ゲームモード
    public enum GameMode
    {
        BATTLE,
        RACE,

        NONE
    }
    public static GameMode Mode { get; set; } = GameMode.NONE;


    //画面のマスク用変数
    static GameObject screenMask = null;
    const string SCREEN_MASK = "ScreenMask";    //画面のマスク用オブジェクトの名前
    const string SCREEN_MASK_CHILD = "Mask";    //子オブジェクトの名前

    //マスクする色
    const float MASK_COLOR_RED = 0;
    const float MASK_COLOR_GREEN = 0;
    const float MASK_COLOR_BLUE = 0;
    const float MASK_COLOR_ALFA = 0.5f;


    void Start()
    {
        IsMainGaming = true;
        BaseScreenManager.LoadScreen(BaseScreenManager.Screen.CONFIG);
        screenMask = GameObject.Find(SCREEN_MASK);
        screenMask.transform.Find(SCREEN_MASK_CHILD).GetComponent<Image>().color = 
            new Color(MASK_COLOR_RED, MASK_COLOR_GREEN, MASK_COLOR_BLUE, MASK_COLOR_ALFA);
        screenMask.SetActive(false);
        IsConfig = false;
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

    //設定画面からメインゲームに移動する
    public static void ConfigToMainGame()
    {
        screenMask.SetActive(false);
        BaseScreenManager.HideScreen();

        IsConfig = false;
    }

    void MoveConfig()
    {
        if (IsConfig)
        {
            ConfigToMainGame();
        }
        else
        {
            screenMask.SetActive(true);
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.CONFIG);

            IsConfig = true;
        }
    }
}
