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

    //ゲームモード
    public enum GameMode
    {
        BATTLE,
        RACE,

        NONE
    }
    public static GameMode Mode { get; set; } = GameMode.NONE;

    void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    public static void LoadMainGameScene()
    {
        SceneManager.LoadScene("MainGameScene");
    }
}
