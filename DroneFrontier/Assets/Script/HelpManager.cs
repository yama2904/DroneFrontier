using System;
using UnityEngine;

public class HelpManager : MonoBehaviour
{
    /// <summary>
    /// ボタン種類
    /// </summary>
    public enum ButtonType
    {
        /// <summary>
        /// 戻る
        /// </summary>
        Back
    }

    /// <summary>
    /// ボタンクリックイベントハンドラ
    /// </summary>
    /// <param name="type">クリックされたボタン</param>
    public delegate void ButtonClickHandler(ButtonType type);

    /// <summary>
    /// ボタンクリックイベント
    /// </summary>
    public event ButtonClickHandler ButtonClick;

    [SerializeField] 
    private GameObject HelpBasicOperationDescription = null;

    [SerializeField] 
    private GameObject HelpBattleModeDescription = null;

    [SerializeField] 
    private GameObject HelpRaceModeDescription = null;

    private enum Help
    {
        BASIC,
        BATTLE,
        RACE,

        NONE
    }
    Help selectHelp = Help.NONE;

    //基本操作
    public void ClickBasicOperation()
    {
        SoundManager.Play(SoundManager.SE.SELECT);

        HelpBasicOperationDescription.SetActive(true);
        selectHelp = Help.BASIC;
    }

    //バトルモード
    public void ClickBattleModeHelp()
    {
        SoundManager.Play(SoundManager.SE.SELECT);

        HelpBattleModeDescription.SetActive(true);
        selectHelp = Help.BATTLE;
    }

    //レースモード
    public void ClickRaceModeHelp()
    {
        SoundManager.Play(SoundManager.SE.SELECT);

        HelpRaceModeDescription.SetActive(true);
        selectHelp = Help.RACE;
    }

    //戻る
    public void ClickBack()
    {
        SoundManager.Play(SoundManager.SE.CANCEL);

        switch (selectHelp)
        {
            case Help.BASIC:
                HelpBasicOperationDescription.SetActive(false);
                break;

            case Help.BATTLE:
                HelpBattleModeDescription.SetActive(false);
                break;

            case Help.RACE:
                HelpRaceModeDescription.SetActive(false);
                break;

            default:
                ButtonClick(ButtonType.Back);
                break;
        }

        selectHelp = Help.NONE;
    }
}
