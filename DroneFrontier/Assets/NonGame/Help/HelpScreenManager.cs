using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpScreenManager : MonoBehaviour
{
    //基本操作
    public void SelectBasicOperation()
    {

    }

    //バトルモード
    public void SelectBattleModeHelp()
    {

    }

    //レースモード
    public void SelectRaceModeHelp()
    {

    }

    //戻る
    public void SelectBack()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.BaseSEVolume);

        BaseScreenManager.SetScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
    }
}
