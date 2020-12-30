using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NonGameManager : MonoBehaviour
{
    static bool isStart = false;

    void Start()
    {
        if (!isStart)
        {
            ConfigManager.InitConfig();
            MainGameManager.IsMulti = false;
        }
        isStart = true;

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

    public static void LoadMainGameScene()
    {
        SceneManager.LoadScene("MainGameScene");
    }
}
