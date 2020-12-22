using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/*
 * 公開変数
 * static bool IsMulti          マルチモードか
 * static bool IsItem           アイテムを出現させるか
 * static bool IsMainGaming     メインゲーム中か
 * static bool IsConfig         設定画面を開いているか
 * static GameMode Mode         どのゲームモードを選んでいるか
 * 
 * 公開型
 * enum GameMode    ゲームモード一覧
 * 
 * 公開メソッド
 * static void LoadMainGameScene()  MainGameSceneへ移動する
 * static void ConfigToMainGame()   設定画面からゲーム画面に移動する(ほとんどConfigManager専用)
 */
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
        BATTLE,   //バトルモード
        RACE,     //レースモード

        NONE
    }
    public static GameMode Mode { get; set; } = GameMode.NONE;  //選んでいるゲームモード


    //設定画面移動時のマスク用変数
    static GameObject screenMask = null;        //画面を暗くする画像を持っているオブジェクト
    const string SCREEN_MASK = "ScreenMask";    //画面のマスク用オブジェクトの名前
    const string SCREEN_MASK_CHILD = "Mask";    //子オブジェクトの名前

    //マスクする色
    const float MASK_COLOR_RED = 0;     //赤
    const float MASK_COLOR_GREEN = 0;   //緑
    const float MASK_COLOR_BLUE = 0;    //青
    const float MASK_COLOR_ALFA = 0.5f; //アルファ


    //デバッグ用
    public static bool IsCursorLock { get; private set; } = true;


    void Start()
    {
        IsMainGaming = true;
        BaseScreenManager.LoadScreen(BaseScreenManager.Screen.CONFIG);  //メインゲームを始めた時点で設定画面をロードする

        //設定画面に移動した際のマスクの暗さと色を設定
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
            if (IsConfig)
            {
                ConfigToMainGame();
            }
            else
            {
                MainGameToConfig();
            }
        }


        //デバッグ用
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            IsCursorLock = !IsCursorLock;
            if (IsCursorLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
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

        if (IsCursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
        IsConfig = false;
    }

    void MainGameToConfig()
    {
        screenMask.SetActive(true);     //設定画面の背景にマスクをつける
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.CONFIG);

        Cursor.lockState = CursorLockMode.None;
        IsConfig = true;
    }
}