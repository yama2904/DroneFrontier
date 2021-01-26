using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpButtonsController : MonoBehaviour
{
    //基本操作
    public void SelectBasicOperation()
    {

    }

    //バトルモード
    public void SelectBattleMode()
    {

    }

    //レースモード
    public void SelectRaceMode()
    {

    }

    //戻る
    public void SelectBack()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
    }
}
