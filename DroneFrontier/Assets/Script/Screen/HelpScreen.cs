using Common;
using System;
using UnityEngine;

public class HelpScreen : MonoBehaviour, IScreen
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
    /// 選択したボタン
    /// </summary>
    public ButtonType SelectedButton { get; private set; }

    /// <summary>
    /// ボタンクリックイベント
    /// </summary>
    public event EventHandler OnButtonClick;

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

    public void Initialize() { }

    //基本操作
    public void ClickBasicOperation()
    {
        SoundManager.Play(SoundManager.SE.Select);

        HelpBasicOperationDescription.SetActive(true);
        selectHelp = Help.BASIC;
    }

    //バトルモード
    public void ClickBattleModeHelp()
    {
        SoundManager.Play(SoundManager.SE.Select);

        HelpBattleModeDescription.SetActive(true);
        selectHelp = Help.BATTLE;
    }

    //レースモード
    public void ClickRaceModeHelp()
    {
        SoundManager.Play(SoundManager.SE.Select);

        HelpRaceModeDescription.SetActive(true);
        selectHelp = Help.RACE;
    }

    //戻る
    public void ClickBack()
    {
        SoundManager.Play(SoundManager.SE.Cancel);

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
                SelectedButton = ButtonType.Back;
                OnButtonClick(this, EventArgs.Empty);
                break;
        }

        selectHelp = Help.NONE;
    }
}
