using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NonGameManager : MonoBehaviour
{
    [SerializeField] GameObject createNetworkManager = null;
    static BaseScreenManager.Screen startScreen = BaseScreenManager.Screen.TITLE;
    static bool isStarted = false;

    void Start()
    {
        //BGMの再生
        SoundManager.StopBGM();
        SoundManager.Play(SoundManager.BGM.DRONE_UP, SoundManager.BaseBGMVolume * 0.8f);

        if (!isStarted)
        {
            Instantiate(createNetworkManager);
        }
        isStarted = true;

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


    public static void LoadMainGameScene()
    {
        SoundManager.StopBGM();
        SceneManager.LoadScene("BattleMode_Offline");
    }

    public static void LoadNonGameScene(BaseScreenManager.Screen startScreen)
    {
        NonGameManager.startScreen = startScreen;
        SceneManager.LoadScene("NonGameScene");
    }
}
