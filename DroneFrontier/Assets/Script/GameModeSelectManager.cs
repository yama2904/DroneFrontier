using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeSelectManager : MonoBehaviour
{
    //ゲームモード
    public enum GameMode
    {
        BATTLE,   //バトルモード
        RACE,     //レースモード

        NONE
    }
    public static GameMode Mode { get; set; } = GameMode.NONE;  //選んだゲームモード

    [SerializeField] 
    private ConfigManager _configScreenManager;

    //バトルモード
    public void SelectBattle()
    {
        Mode = GameMode.BATTLE;
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.SOLO_MULTI_SELECT);
    }

    //レースモード
    public void SelectRace()
    {
        Mode = GameMode.RACE;
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.SOLO_MULTI_SELECT);
    }

    //設定
    public void SelectConfig()
    {
        _configScreenManager.gameObject.SetActive(true);
    }

    //ヘルプ
    public void SelectHelp()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.HELP);
    }

    //戻る
    public void SelectBack()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.TITLE);
    }
}
