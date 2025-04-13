using Network;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeSceneManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _gameModeSelectUI;

    [SerializeField]
    private ConfigScreen _config;

    [SerializeField]
    private HelpScreen _help;

    [SerializeField]
    private SoloMultiSelectScreen _soloMultiSelect;

    [SerializeField]
    private WeaponSelectScreen _weaponSelect;

    [SerializeField]
    private CPUSelectScreen _cpuSelect;

    [SerializeField]
    private MatchingScreen _matching;

    [SerializeField]
    private NetworkWeaponSelectScreen _networkWeaponSelect;

    private void Start()
    {
        // 画面初期化
        _config.Initialize();
        _help.Initialize();
        _soloMultiSelect.Initialize();
        _weaponSelect.Initialize();
        _cpuSelect.Initialize();
        _matching.Initialize();
        _networkWeaponSelect.Initialize();

        // 設定画面のボタンイベント設定
        _config.OnButtonClick += OnButtonClickOfConfig;

        // ヘルプ画面のボタンイベント設定
        _help.OnButtonClick += OnButtonClickOfHelp;

        // ソロ/マルチ選択画面のボタンイベント設定
        _soloMultiSelect.OnButtonClick += OnButtonClickOfSoloMulti;

        // 武器選択画面のボタンイベント設定
        _weaponSelect.OnButtonClick += OnButtonClickOfWeaponSel;

        // CPU選択画面のボタンイベント設定
        _cpuSelect.OnButtonClick += OnButtonClickOfCpuSel;

        // マッチング画面のボタンイベント設定
        _matching.OnButtonClick += OnButtonClickOfMatching;

        // マルチ用武器選択画面のボタンイベント設定
        _networkWeaponSelect.OnButtonClick += OnButtonClickOfNetworkWeaponSel;

        // BGMが再生されていなかったら再生
        if (SoundManager.PlayingBGM != SoundManager.BGM.Home)
        {
            SoundManager.Play(SoundManager.BGM.Home, 0.8f);
        }
    }

    #region ボタンイベント

    /// <summary>
    /// バトルモード選択
    /// </summary>
    public void ClickBattle()
    {
        SoundManager.Play(SoundManager.SE.Select);
        _gameModeSelectUI.SetActive(false);
        _soloMultiSelect.gameObject.SetActive(true);
    }

    /// <summary>
    /// レースモード選択
    /// </summary>
    public void ClickRace()
    {
        SoundManager.Play(SoundManager.SE.Select);
        // ToDo
    }

    /// <summary>
    /// 設定選択
    /// </summary>
    public void ClickConfig()
    {
        SoundManager.Play(SoundManager.SE.Select);

        _gameModeSelectUI.SetActive(false);
        _config.gameObject.SetActive(true);
    }

    /// <summary>
    /// ヘルプ選択
    /// </summary>
    public void ClickHelp()
    {
        SoundManager.Play(SoundManager.SE.Select);

        _gameModeSelectUI.SetActive(false);
        _help.gameObject.SetActive(true);
    }

    /// <summary>
    /// 戻る選択
    /// </summary>
    public void ClickBack()
    {
        SoundManager.StopBGM();
        SoundManager.Play(SoundManager.SE.Cancel);
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
        _config.gameObject.SetActive(false);
    }

    /// <summary>
    /// ヘルプ画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfHelp(object sender, EventArgs e)
    {
        _gameModeSelectUI.SetActive(true);
        _help.gameObject.SetActive(false);
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
            _soloMultiSelect.gameObject.SetActive(false);
            _weaponSelect.gameObject.SetActive(true);
        }

        // マルチモード選択
        if (screen.SelectedButton == SoloMultiSelectScreen.ButtonType.MultiMode)
        {
            _soloMultiSelect.gameObject.SetActive(false);
            _matching.gameObject.SetActive(true);
        }

        // 戻る選択
        if (screen.SelectedButton == SoloMultiSelectScreen.ButtonType.Back)
        {
            _soloMultiSelect.gameObject.SetActive(false);
            _gameModeSelectUI.SetActive(true);
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
            _weaponSelect.gameObject.SetActive(false);
            _cpuSelect.gameObject.SetActive(true);
        }

        // 戻る選択
        if (screen.SelectedButton == WeaponSelectScreen.ButtonType.Back)
        {
            _weaponSelect.gameObject.SetActive(false);
            _soloMultiSelect.gameObject.SetActive(true);
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
            SceneManager.LoadScene("BattleScene");
        }

        // 戻る選択
        if (screen.SelectedButton == CPUSelectScreen.ButtonType.Back)
        {
            _cpuSelect.gameObject.SetActive(false);
            _weaponSelect.gameObject.SetActive(true);
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
            _matching.gameObject.SetActive(false);
            _networkWeaponSelect.gameObject.SetActive(true);
        }

        // 戻る選択
        if (screen.SelectedButton == MatchingScreen.ButtonType.Back)
        {
            _matching.gameObject.SetActive(false);
            _soloMultiSelect.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// マルチ用武器選択画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfNetworkWeaponSel(object sender, EventArgs e)
    {
        NetworkWeaponSelectScreen screen = sender as NetworkWeaponSelectScreen;

        // 決定選択
        if (screen.SelectedButton == NetworkWeaponSelectScreen.ButtonType.Ok)
        {
            SceneManager.LoadScene("NetworkBattleScene");
        }

        // 戻る選択
        if (screen.SelectedButton == NetworkWeaponSelectScreen.ButtonType.Back)
        {
            _networkWeaponSelect.gameObject.SetActive(false);
            _soloMultiSelect.gameObject.SetActive(true);
        }
    }

    #endregion
}
