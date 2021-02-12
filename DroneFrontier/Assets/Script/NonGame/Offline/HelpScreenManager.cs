using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpScreenManager : MonoBehaviour
{
    [SerializeField] Image HelpBasicOperationImage = null;
    [SerializeField] Image HelpBattleModeImage = null;
    [SerializeField] Image HelpRaceModeImage = null;
    enum Help
    {
        BASIC,
        BATTLE,
        RACE,

        NONE
    }
    Help selectHelp = Help.NONE;

    private void Start()
    {
        HelpBasicOperationImage.enabled = false;
        HelpBattleModeImage.enabled = false;
        HelpRaceModeImage.enabled = false;
    }


    //基本操作
    public void SelectBasicOperation()
    {
        HelpBasicOperationImage.enabled = true;
        selectHelp = Help.BASIC;
    }

    //バトルモード
    public void SelectBattleModeHelp()
    {
        HelpBattleModeImage.enabled = true;
        selectHelp = Help.BATTLE;
    }

    //レースモード
    public void SelectRaceModeHelp()
    {
        HelpRaceModeImage.enabled = true;
        selectHelp = Help.RACE;
    }

    //戻る
    public void SelectBack()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.BaseSEVolume);

        if (selectHelp == Help.BASIC)
        {
            HelpBasicOperationImage.enabled = false;
            selectHelp = Help.NONE;
            return;
        }
        if (selectHelp == Help.BATTLE)
        {
            HelpBattleModeImage.enabled = false;
            selectHelp = Help.NONE;
            return;
        }
        if (selectHelp == Help.RACE)
        {
            HelpRaceModeImage.enabled = false;
            selectHelp = Help.NONE;
            return;
        }
        else
        {
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
        }
    }
}
