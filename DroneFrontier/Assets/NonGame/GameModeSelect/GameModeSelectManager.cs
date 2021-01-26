using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeSelectManager : MonoBehaviour
{
    //バトルモード
    public void SelectBattle()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        MainGameManager.Mode = MainGameManager.GameMode.BATTLE;
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.KURIBOCCHI);
    }

    //レースモード
    public void SelectRace()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        MainGameManager.Mode = MainGameManager.GameMode.RACE;
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.KURIBOCCHI);
    }

    //設定
    public void SelectConfig()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        BaseScreenManager.SetScreen(BaseScreenManager.Screen.CONFIG);
    }

    //ヘルプ
    public void SelectHelp()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        BaseScreenManager.SetScreen(BaseScreenManager.Screen.HELP);
    }

    //戻る
    public void SelectBack()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.BaseSEVolume);

        BaseScreenManager.SetScreen(BaseScreenManager.Screen.TITLE);
    }
}
