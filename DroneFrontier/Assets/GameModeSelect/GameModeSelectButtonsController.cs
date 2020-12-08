using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeSelectButtonsController : MonoBehaviour
{
    public void SelectBack()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.TITLE);
    }
}
