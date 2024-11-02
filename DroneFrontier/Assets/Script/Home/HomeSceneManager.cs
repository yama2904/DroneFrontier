using Offline;
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
    private WeaponSelectScreen _weaponSelectManager;

    [SerializeField]
    private CPUSelectScreen _cpuSelectManager;

    void Start()
    {
        //if (!isStarted)
        //{
        //    Instantiate(_createNetworkManager);
        //}
        //isStarted = true;

        // 設定画面の戻るボタン動作設定
        _configManager.ButtonClick += ClickConfigButton;

        // ヘルプ画面の戻るボタン動作設定
        _helpManager.ButtonClick += ClickHelpButton;

        // ソロ/マルチ選択画面のボタンイベント設定
        _soloMultiSelectManager.ButtonClick += ClickSoloMultiButton;

        // 武器選択画面のボタンイベント設定
        _weaponSelectManager.ButtonClick += ClickWeaponSelectButton;

        // CPU選択画面のボタンイベント設定
        _cpuSelectManager.ButtonClick += ClickCpuSelectButton;

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

    //バトルモード
    public void ClickBattle()
    {
        SoundManager.Play(SoundManager.SE.SELECT);
        _gameModeSelectUI.SetActive(false);
        _soloMultiSelectManager.gameObject.SetActive(true);
    }

    //レースモード
    public void ClickRace()
    {
        SoundManager.Play(SoundManager.SE.SELECT);
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.SOLO_MULTI_SELECT);
    }

    //設定
    public void ClickConfig()
    {
        SoundManager.Play(SoundManager.SE.SELECT);

        _gameModeSelectUI.SetActive(false);
        _configManager.gameObject.SetActive(true);
    }

    //ヘルプ
    public void ClickHelp()
    {
        SoundManager.Play(SoundManager.SE.SELECT);

        _gameModeSelectUI.SetActive(false);
        _helpManager.gameObject.SetActive(true);
    }

    //戻る
    public void ClickBack()
    {
        SoundManager.StopBGM();
        SoundManager.Play(SoundManager.SE.CANCEL);
        SceneManager.LoadScene("TitleScene");
    }

    /// <summary>
    /// 設定画面のボタンクリックイベント
    /// </summary>
    /// <param name="type">クリックされたボタン</param>
    private void ClickConfigButton(ConfigScreen.ButtonType type)
    {
        _gameModeSelectUI.SetActive(true);
        _configManager.gameObject.SetActive(false);
    }

    /// <summary>
    /// ヘルプ画面のボタンクリックイベント
    /// </summary>
    /// <param name="type">クリックされたボタン</param>
    private void ClickHelpButton(HelpScreen.ButtonType type)
    {
        _gameModeSelectUI.SetActive(true);
        _helpManager.gameObject.SetActive(false);
    }

    /// <summary>
    /// ソロ/マルチ選択画面のボタンクリックイベント
    /// </summary>
    /// <param name="type">クリックされたボタン</param>
    private void ClickSoloMultiButton(SoloMultiSelectScreen.ButtonType type)
    {
        // ソロモード選択
        if (type == SoloMultiSelectScreen.ButtonType.SoloMode)
        {
            _soloMultiSelectManager.gameObject.SetActive(false);
            _weaponSelectManager.gameObject.SetActive(true);
        }

        // 戻る選択
        if (type == SoloMultiSelectScreen.ButtonType.Back)
        {
            _soloMultiSelectManager.gameObject.SetActive(false);
            _gameModeSelectUI.SetActive(true);
        }
    }

    /// <summary>
    /// 武器選択画面のボタンクリックイベント
    /// </summary>
    /// <param name="type">クリックされたボタン</param>
    private void ClickWeaponSelectButton(WeaponSelectScreen.ButtonType type)
    {
        // 決定選択
        if (type == WeaponSelectScreen.ButtonType.OK)
        {
            _weaponSelectManager.gameObject.SetActive(false);
            _cpuSelectManager.gameObject.SetActive(true);
        }

        // 戻る選択
        if (type == WeaponSelectScreen.ButtonType.Back)
        {
            _weaponSelectManager.gameObject.SetActive(false);
            _soloMultiSelectManager.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// CPU選択画面のボタンクリックイベント
    /// </summary>
    /// <param name="type">クリックされたボタン</param>
    private void ClickCpuSelectButton(CPUSelectScreen.ButtonType type)
    {
        // 決定選択
        if (type == CPUSelectScreen.ButtonType.OK)
        {
            SceneManager.LoadScene("BattleMode_Offline");
        }

        // 戻る選択
        if (type == CPUSelectScreen.ButtonType.Back)
        {
            _cpuSelectManager.gameObject.SetActive(false);
            _weaponSelectManager.gameObject.SetActive(true);
        }
    }

    #endregion
}
