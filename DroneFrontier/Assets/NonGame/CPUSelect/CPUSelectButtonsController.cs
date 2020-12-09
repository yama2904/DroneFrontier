using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPUSelectButtonsController : MonoBehaviour
{
    public void Debug()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.WEAPON_SELECT);
    }

    public void SelectBack()
    {
        //ソロモードなら戻る
        if (!MainGameManager.IsMulti)
        {
            BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.KURIBOCCHI);
        }
    }
}
