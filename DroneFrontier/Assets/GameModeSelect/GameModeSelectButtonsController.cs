using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeSelectButtonsController : MonoBehaviour
{
    //バトルモード
    public void SelectBattle()
    {

    }

    //レースモード
    public void SelectRace()
    {

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
