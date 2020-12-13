using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeSelectButtonsController : MonoBehaviour
{
    //バトルモード
    public void SelectBattle()
    {
        MainGameManager.Mode = MainGameManager.GameMode.BATTLE;
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.KURIBOCCHI);
    }

    //レースモード
    public void SelectRace()
    {
        MainGameManager.Mode = MainGameManager.GameMode.RACE;
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.KURIBOCCHI);
    }

    //設定
    public void SelectConfig()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.CONFIG);
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
