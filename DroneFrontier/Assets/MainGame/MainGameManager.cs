using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameManager : MonoBehaviour
{
    //マルチモードか
    public static bool IsMulti { get; set; }

    //ゲームモード
    public enum GameMode
    {
        BATTLE,
        RACE,

        NONE
    }
    public static GameMode Mode { get; set; }

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
