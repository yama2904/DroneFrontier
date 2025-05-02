using Common;
using Screen;
using Screen.Network;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeSceneManager : MonoBehaviour
{
    private enum GameMode
    {
        Battle,
        Rece,

        None
    }

    [SerializeField]
    private GameObject _gameModeSelectUI;

    [SerializeField]
    private GameObject _configButton;

    [SerializeField]
    private GameObject _helpButton;

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

    private GameMode _selectMode = GameMode.None;

    private void Start()
    {
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
        _selectMode = GameMode.Battle;
        _gameModeSelectUI.SetActive(false);

        _soloMultiSelect.Initialize();
        _soloMultiSelect.Show();
    }

    /// <summary>
    /// レースモード選択
    /// </summary>
    public void ClickRace()
    {
        SoundManager.Play(SoundManager.SE.Select);
        _selectMode = GameMode.Rece;

        _configButton.SetActive(false);
        _helpButton.SetActive(false);

        _matching.Initialize();
        _matching.PreScreen = _gameModeSelectUI;
        _matching.GameMode = _selectMode.ToString();
        _matching.Show();
    }

    /// <summary>
    /// 設定選択
    /// </summary>
    public void ClickConfig()
    {
        SoundManager.Play(SoundManager.SE.Select);

        _gameModeSelectUI.SetActive(false);
        _config.Initialize();
        _config.Show();
    }

    /// <summary>
    /// ヘルプ選択
    /// </summary>
    public void ClickHelp()
    {
        SoundManager.Play(SoundManager.SE.Select);

        _gameModeSelectUI.SetActive(false);
        _help.Initialize();
        _help.Show();
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
        _config.Hide();
    }

    /// <summary>
    /// ヘルプ画面のボタンクリックイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void OnButtonClickOfHelp(object sender, EventArgs e)
    {
        _gameModeSelectUI.SetActive(true);
        _help.Hide();
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
            _soloMultiSelect.Hide();
            _weaponSelect.Initialize();
            _weaponSelect.Show();
        }

        // マルチモード選択
        if (screen.SelectedButton == SoloMultiSelectScreen.ButtonType.MultiMode)
        {
            _matching.Initialize();
            _matching.PreScreen = _soloMultiSelect.gameObject;
            _matching.GameMode = _selectMode.ToString();
            _matching.Show();
        }

        // 戻る選択
        if (screen.SelectedButton == SoloMultiSelectScreen.ButtonType.Back)
        {
            _soloMultiSelect.Hide();
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
        if (screen.SelectedButton == WeaponSelectScreen.ButtonType.Ok)
        {
            _weaponSelect.Hide();
            _cpuSelect.Initialize();
            _cpuSelect.Show();
        }

        // 戻る選択
        if (screen.SelectedButton == WeaponSelectScreen.ButtonType.Back)
        {
            _weaponSelect.Hide();
            _soloMultiSelect.Show();
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
        if (screen.SelectedButton == CPUSelectScreen.ButtonType.Ok)
        {
            SceneManager.LoadScene("BattleScene");
        }

        // 戻る選択
        if (screen.SelectedButton == CPUSelectScreen.ButtonType.Back)
        {
            _cpuSelect.Hide();
            _weaponSelect.Show();
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
            _matching.Hide();

            if (_selectMode == GameMode.Battle)
            {
                _networkWeaponSelect.Initialize();
                _networkWeaponSelect.Show();
            }
            else
            {
                SceneManager.LoadScene("RaceScene");
            }
        }

        // 戻る選択
        if (screen.SelectedButton == MatchingScreen.ButtonType.Back)
        {
            _matching.Hide();

            if (_selectMode == GameMode.Rece)
            {
                _configButton.SetActive(true);
                _helpButton.SetActive(true);
            }
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
            _networkWeaponSelect.Hide();
            _soloMultiSelect.Show();
        }
    }

    #endregion
}
