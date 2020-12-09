using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeSelectButtonsController : MonoBehaviour
{
    //バトルモード
    public void SelectBattle()
    {
        MainGameManager.Mode = MainGameManager.GameMode.BATTLE;
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.KURIBOCCHI);
    }

    //レースモード
    public void SelectRace()
    {
        MainGameManager.Mode = MainGameManager.GameMode.RACE;
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.KURIBOCCHI);
    }

    //設定
    public void SelectConfig()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.CONFIG);
    }

    //ヘルプ
    public void SelectHelp()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.HELP);
    }

    //戻る
    public void SelectBack()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.TITLE);
    }
}
