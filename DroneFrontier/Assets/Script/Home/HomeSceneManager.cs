using Network;
using Offline;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeSceneManager : MonoBehaviour
{
    [SerializeField] 
    private GameObject _createNetworkManager;

    [SerializeField]
    private GameObject _gameModeSelectUI;

    [SerializeField]
    private ConfigScreen _configManager;

    [SerializeField]
    private HelpScreen _helpManager;

    [SerializeField]
    private SoloMultiSelectScreen _soloMultiSelectManager;

    [SerializeField]
    private MatchingScreen _matchingManager;

    [SerializeField]
    private WeaponSelectScreen _weaponSelectManager;

    [SerializeField]
    private CPUSelectScreen _cpuSelectManager;

    private void Start()
    {
        // 設定画面のボタンイベント設定
        _configManager.OnButtonClick += OnButtonClickOfConfig;

        // ヘルプ画面のボタンイベント設定
        _helpManager.OnButtonClick += OnButtonClickOfHelp;

        // ソロ/マルチ選択画面のボタンイベント設定
        _soloMultiSelectManager.OnButtonClick += OnButtonClickOfSoloMulti;

        // マッチング画面のボタンイベント設定
        _matchingManager.OnButtonClick += OnButtonClickOfMatching;

        // 武器選択画面のボタンイベント設定
        _weaponSelectManager.OnButtonClick += OnButtonClickOfWeaponSel;

        // CPU選択画面のボタンイベント設定
        _cpuSelectManager.OnButtonClick += OnButtonClickOfCpuSel;

        // BGMが再生されていなかったら再生
        if (SoundManager.PlayingBGM != SoundManager.BGM.DRONE_UP)
        {
            SoundManager.Play(SoundManager.BGM.DRONE_UP, SoundManager.BGMVolume * 0.8f);
        }
    }

    public static void LoadHomeScene(BaseScreenManager.Screen startScreen)
    {
        // 後で消す

        //HomeSceneManager.startScreen = startScreen;
        //SceneManager.LoadScene("HomeScene");
    }

    #region ボタンイベント

    /// <summary>
    /// バトルモード選択
    /// </summary>
    public void ClickBattle()
    {
        SoundManager.Play(SoundManager.SE.SELECT);
        _gameModeSelectUI.SetActive(false);
        _soloMultiSelectManager.gameObject.SetActive(true);
    }

    /// <summary>
    /// レースモード選択
    /// </summary>
    public void ClickRace()
    {
        SoundManager.Play(SoundManager.SE.SELECT);
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.SOLO_MULTI_SELECT);
    }

    /// <summary>
    /// 設定選択
    /// </summary>
    public void ClickConfig()
    {
        SoundManager.Play(SoundManager.SE.SELECT);

        _gameModeSelectUI.SetActive(false);
        _configManager.gameObject.SetActive(true);
    }

    /// <summary>
    /// ヘルプ選択
    /// </summary>
    public void ClickHelp()
    {
        SoundManager.Play(SoundManager.SE.SELECT);

        _gameModeSelectUI.SetActive(false);
        _helpManager.gameObject.SetActive(true);
    }

    /// <summary>
    /// 戻る選択
    /// </summary>
    public void ClickBack()
    {
        SoundManager.StopBGM();
        SoundManager.Play(SoundManager.SE.CANCEL);
        SceneManager.LoadScene("TitleScene");
    }

    /// <summary>
    /// 設定画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfConfig(object sender, EventArgs e)
    {
        _gameModeSelectUI.SetActive(true);
        _configManager.gameObject.SetActive(false);
    }

    /// <summary>
    /// ヘルプ画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfHelp(object sender, EventArgs e)
    {
        _gameModeSelectUI.SetActive(true);
        _helpManager.gameObject.SetActive(false);
    }

    /// <summary>
    /// ソロ/マルチ選択画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfSoloMulti(object sender, EventArgs e)
    {
        SoloMultiSelectScreen screen = sender as SoloMultiSelectScreen;

        // ソロモード選択
        if (screen.SelectedButton == SoloMultiSelectScreen.ButtonType.SoloMode)
        {
            _soloMultiSelectManager.gameObject.SetActive(false);
            _weaponSelectManager.gameObject.SetActive(true);
        }

        // マルチモード選択
        if (screen.SelectedButton == SoloMultiSelectScreen.ButtonType.MultiMode)
        {
            _soloMultiSelectManager.gameObject.SetActive(false);
            _matchingManager.gameObject.SetActive(true);
        }

        // 戻る選択
        if (screen.SelectedButton == SoloMultiSelectScreen.ButtonType.Back)
        {
            _soloMultiSelectManager.gameObject.SetActive(false);
            _gameModeSelectUI.SetActive(true);
        }
    }

    /// <summary>
    /// マッチング画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfMatching(object sender, EventArgs e)
    {
        MatchingScreen screen = sender as MatchingScreen;

        // 決定選択
        if (screen.SelectedButton == MatchingScreen.ButtonType.Ok)
        {
            _matchingManager.gameObject.SetActive(false);
            // ToDo
        }

        // 戻る選択
        if (screen.SelectedButton == MatchingScreen.ButtonType.Back)
        {
            _matchingManager.gameObject.SetActive(false);
            _soloMultiSelectManager.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 武器選択画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfWeaponSel(object sender, EventArgs e)
    {
        WeaponSelectScreen screen = sender as WeaponSelectScreen;

        // 決定選択
        if (screen.SelectedButton == WeaponSelectScreen.ButtonType.OK)
        {
            _weaponSelectManager.gameObject.SetActive(false);
            _cpuSelectManager.gameObject.SetActive(true);
        }

        // 戻る選択
        if (screen.SelectedButton == WeaponSelectScreen.ButtonType.Back)
        {
            _weaponSelectManager.gameObject.SetActive(false);
            _soloMultiSelectManager.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// CPU選択画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfCpuSel(object sender, EventArgs e)
    {
        CPUSelectScreen screen = sender as CPUSelectScreen;

        // 決定選択
        if (screen.SelectedButton == CPUSelectScreen.ButtonType.OK)
        {
            SceneManager.LoadScene("BattleMode_Offline");
        }

        // 戻る選択
        if (screen.SelectedButton == CPUSelectScreen.ButtonType.Back)
        {
            _cpuSelectManager.gameObject.SetActive(false);
            _weaponSelectManager.gameObject.SetActive(true);
        }
    }

    #endregion
}
