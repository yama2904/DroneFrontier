using Common;
using Cysharp.Threading.Tasks;
using Network;
using System;
using UnityEngine;
using UnityEngine.UI;

public class SoloMultiSelectScreen : MonoBehaviour, IScreen
{
    /// <summary>
    /// ボタン種類
    /// </summary>
    public enum ButtonType
    {
        /// <summary>
        /// ソロモード
        /// </summary>
        SoloMode,

        /// <summary>
        /// マルチモード
        /// </summary>
        MultiMode,

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
    /// 入力したプレイヤー名
    /// </summary>
    public string PlayerName { get; private set; } = string.Empty;

    /// <summary>
    /// ボタンクリックイベント
    /// </summary>
    public event EventHandler OnButtonClick;

    [SerializeField, Tooltip("ソロボタン")]
    private GameObject _soloButton = null;

    [SerializeField, Tooltip("名前入力UIのCanvas")]
    private Canvas _inputFieldCanvas = null;

    [SerializeField, Tooltip("名前入力UI")]
    private InputField _inputField = null;

    [SerializeField, Tooltip("募集ボタン")]
    private Button _hostButton = null;

    [SerializeField, Tooltip("参加ボタン")]
    private Button _clientButton = null;

    [SerializeField, Tooltip("ホスト探索中のマスク")]
    private Image _discoverMask = null;

    [SerializeField, Tooltip("エラーメッセージのCanvas")]
    private Canvas _errMsgCanvas = null;

    [SerializeField, Tooltip("エラーメッセージ表示用テキスト")]
    private Text _errMsgText = null;

    /// <summary>
    /// ホスト探索中であるか
    /// </summary>
    private bool _isDiscovery = false;

    /// <summary>
    /// 通信中にエラーが発生したか
    /// </summary>
    private bool _isError = false;

    public void Initialize() 
    {
        // 名前入力欄非表示
        _inputFieldCanvas.enabled = false;

        // エラーメッセージ非表示
        _errMsgCanvas.enabled = false;

        // レースモードの場合はソロボタン非表示
        if (GameModeSelectScreen.Mode == GameModeSelectScreen.GameMode.RACE)
        {
            _soloButton.SetActive(false);
        }
        else
        {
            _soloButton.SetActive(true);
        }
    }

    /// <summary>
    /// ソロモード選択
    /// </summary>
    public void ClickSolo()
    {
        SoundManager.Play(SoundManager.SE.Select);
        SelectedButton = ButtonType.SoloMode;
        OnButtonClick(this, EventArgs.Empty);
    }

    /// <summary>
    /// マルチモード選択
    /// </summary>
    public void ClickMulti()
    {
        // SE再生
        SoundManager.Play(SoundManager.SE.Select);

        // 名前入力欄表示
        _inputFieldCanvas.enabled = true;
    }

    /// <summary>
    /// 戻るボタン選択
    /// </summary>
    public void ClickBack()
    {
        SoundManager.Play(SoundManager.SE.Cancel);
        SelectedButton = ButtonType.Back;
        OnButtonClick(this, EventArgs.Empty);
    }

    /// <summary>
    /// 募集ボタン選択
    /// </summary>
    public void ClickHost()
    {
        // 名前を入力していなかったら処理しない
        if (_inputField.text == "") return;

        PlayerName = _inputField.text;

        // ホストとして通信を開始
        MyNetworkManager.Singleton.StartHost(PlayerName).Forget();

        // ボタン選択イベント発火
        SoundManager.Play(SoundManager.SE.Select);
        SelectedButton = ButtonType.MultiMode;
        OnButtonClick(this, EventArgs.Empty);
    }

    /// <summary>
    /// 参加ボタン選択
    /// </summary>
    public void ClickClient()
    {
        // 名前を入力していなかったら処理しない
        if (_inputField.text == "") return;

        PlayerName = _inputField.text;

        // SE再生
        SoundManager.Play(SoundManager.SE.Select);

        // クライアントとして通信を開始
        UniTask.Void(async () =>
        {
            // 通信相手発見イベント設定
            MyNetworkManager.Singleton.OnConnect += OnConnect;

            try
            {
                await MyNetworkManager.Singleton.StartClient(PlayerName);
            }
            catch (NetworkException ex)
            {
                _errMsgCanvas.enabled = true;
                _errMsgText.text = ex.Message;
                _isError = true;
            }
        });

        // 探索モード
        ChangeDiscovery(true);
    }

    /// <summary>
    /// 名前入力中の戻るボタン選択
    /// </summary>
    public void ClickBackOfInputField()
    {
        // SE再生
        SoundManager.Play(SoundManager.SE.Cancel, SoundManager.MasterSEVolume);

        // 探索中の場合は探索停止
        if (_isDiscovery)
        {
            MyNetworkManager.Singleton.Disconnect();
            MyNetworkManager.Singleton.OnConnect -= OnConnect;

            // 探索解除
            ChangeDiscovery(false);
        }
        else
        {
            // 名前入力欄非表示
            _inputFieldCanvas.enabled = false;
        }
    }

    private void Update()
    {
        if (_isError && Input.GetMouseButtonUp(0))
        {
            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // エラーメッセージ非表示
            _errMsgCanvas.enabled = false;
            _errMsgText.text = "";
            _isError = false;

            // 探索解除
            ChangeDiscovery(false);
        }
    }

    /// <summary>
    /// 通信接続イベント
    /// </summary>
    /// <param name="player">通信相手のプレイヤー名</param>
    private void OnConnect(string player)
    {
        // 本イベント削除
        MyNetworkManager.Singleton.OnConnect -= OnConnect;

        // 探索解除
        ChangeDiscovery(false);

        // ボタン選択イベント発火
        SelectedButton = ButtonType.MultiMode;
        OnButtonClick(this, EventArgs.Empty);
    }

    /// <summary>
    /// 探索中モードの変更
    /// </summary>
    /// <param name="flag">探索モードONの場合はtrue</param>
    private void ChangeDiscovery(bool flag)
    {
        // 各ボタンを活性に戻す
        _inputField.enabled = !flag;
        _hostButton.enabled = !flag;
        _clientButton.enabled = !flag;

        // 探索中マスク設定
        _discoverMask.enabled = flag;

        // 探索中フラグ初期化
        _isDiscovery = flag;
    }
}
