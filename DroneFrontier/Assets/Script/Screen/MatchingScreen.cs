using Battle.Network;
using Common;
using Cysharp.Threading.Tasks;
using Network;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Screen
{
    public class MatchingScreen : MonoBehaviour, IScreen
    {
        /// <summary>
        /// ボタン種類
        /// </summary>
        public enum ButtonType
        {
            /// <summary>
            /// 決定
            /// </summary>
            Ok,

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
        /// 前画面
        /// </summary>
        public GameObject PreScreen { get; set; } = null;

        /// <summary>
        /// ボタンクリックイベント
        /// </summary>
        public event EventHandler OnButtonClick;

        /// <summary>
        /// 募集中枠のテキスト
        /// </summary>
        private const string NON_PLAYER_TEXT = "参加者受付中...";

        /// <summary>
        /// ホスト切断時のエラーメッセージ
        /// </summary>
        private const string HOST_DISCONNECT = "ホストとの通信が切断されました。";

        [SerializeField, Tooltip("名前入力欄のCanvas")]
        private Canvas _inputFieldCanvas = null;

        [SerializeField, Tooltip("名前入力欄")]
        private InputField _inputField = null;

        [SerializeField, Tooltip("ホスト探索中のマスク")]
        private Image _discoverMask = null;

        [SerializeField, Tooltip("プレイヤー一覧のCanvas")]
        private Canvas _playerListCanvas = null;

        [SerializeField, Tooltip("1P表示テキスト")]
        private Text _1pText = null;

        [SerializeField, Tooltip("2P表示テキスト")]
        private Text _2pText = null;

        [SerializeField, Tooltip("3P表示テキスト")]
        private Text _3pText = null;

        [SerializeField, Tooltip("4P表示テキスト")]
        private Text _4pText = null;

        [SerializeField, Tooltip("決定ボタン")]
        private GameObject _okButton = null;

        [SerializeField, Tooltip("エラーメッセージのCanvas")]
        private Canvas _errMsgCanvas = null;

        [SerializeField, Tooltip("エラーメッセージ表示用テキスト")]
        private Text _errMsgText = null;

        [SerializeField, Tooltip("プレイヤーのテキスト色")]
        private Color _playerTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [SerializeField, Tooltip("募集中のテキスト色")]
        private Color _nonPlayerTextColor = new Color(0.32f, 0.41f, 0.72f, 1f);

        /// <summary>
        /// 入力したプレイヤー名
        /// </summary>
        private string _playerName = string.Empty;

        /// <summary>
        /// 通信相手探索中であるか
        /// </summary>
        private bool _isDiscovery = false;

        /// <summary>
        /// 通信中にエラーが発生したか
        /// </summary>
        private bool _isError = false;

        public void Initialize()
        {
            // 名前入力欄表示
            _inputFieldCanvas.enabled = true;

            // エラーメッセージ非表示
            _errMsgCanvas.enabled = false;
            _errMsgText.text = "";
            _isError = false;

            // 決定ボタン非表示
            _okButton.SetActive(false);

            // 名前入力欄で初期化
            ShowPlayerList(false);

            NetworkManager.Initialize();
            NetworkBattleManager.Initialize();
        }

        /// <summary>
        /// 募集ボタン選択
        /// </summary>
        public void ClickHost()
        {
            // 名前を入力していなかったら処理しない
            if (_inputField.text == "") return;

            // 入力したプレイヤー名保存
            _playerName = _inputField.text;

            // 通信イベント設定
            NetworkManager.OnConnected += OnConnected;
            NetworkManager.OnDisconnected += OnDisconnected;
            NetworkManager.OnDiscoveryCompleted += OnDiscoveryCompleted;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // ホストとして通信を開始
            UniTask.Void(async () =>
            {
                try
                {
                    _isDiscovery = true;
                    await NetworkManager.StartAccept(_playerName);
                }
                catch (NetworkException ex)
                {
                    _errMsgCanvas.enabled = true;
                    _errMsgText.text = ex.Message;
                    _isError = true;
                }
            });

            // プレイヤー一覧へ移動
            UpdatePlayerNames();
            ShowPlayerList(true);
        }

        /// <summary>
        /// 参加ボタン選択
        /// </summary>
        public void ClickClient()
        {
            // 名前を入力していなかったら処理しない
            if (_inputField.text == "") return;

            // 入力したプレイヤー名保存
            _playerName = _inputField.text;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // プレイヤー名表示
            UpdatePlayerNames();

            // 通信イベント設定
            NetworkManager.OnConnected += OnConnected;
            NetworkManager.OnDisconnected += OnDisconnected;
            NetworkManager.OnDiscoveryCompleted += OnDiscoveryCompleted;

            // クライアントとして通信を開始
            UniTask.Void(async () =>
            {
                try
                {
                    _isDiscovery = true;
                    await NetworkManager.StartDiscovery(_playerName);
                }
                catch (NetworkException ex)
                {
                    _errMsgCanvas.enabled = true;
                    _errMsgText.text = ex.Message;
                    _isError = true;
                }
            });

            // ホスト探索中マスク表示
            _discoverMask.enabled = true;
        }

        /// <summary>
        /// 決定選択
        /// </summary>
        public void ClickOk()
        {
            // プレイヤー探索終了
            NetworkManager.StartGame();
        }

        /// <summary>
        /// 名前入力欄の戻るボタン選択
        /// </summary>
        public void ClickBackOfNameInput()
        {
            // SE再生
            SoundManager.Play(SoundManager.SE.Cancel);

            // 探索中の場合は切断
            if (_isDiscovery)
            {
                NetworkManager.Disconnect();
                NetworkManager.OnConnected -= OnConnected;
                NetworkManager.OnDisconnected -= OnDisconnected;
                NetworkManager.OnDiscoveryCompleted -= OnDiscoveryCompleted;
                _isDiscovery = false;
            }

            // マスク表示中の場合はマスク解除
            if (_discoverMask.enabled)
            {
                _discoverMask.enabled = false;
                return;
            }

            // 前画面へ戻る
            PreScreen = null;
            SelectedButton = ButtonType.Back;
            OnButtonClick(this, EventArgs.Empty);
        }

        /// <summary>
        /// プレイヤー一覧の戻る選択
        /// </summary>
        public void ClickBackOfPlayerList()
        {
            // SE再生
            SoundManager.Play(SoundManager.SE.Cancel);

            // 切断
            NetworkManager.Disconnect();
            NetworkManager.OnConnected -= OnConnected;
            NetworkManager.OnDisconnected -= OnDisconnected;
            NetworkManager.OnDiscoveryCompleted -= OnDiscoveryCompleted;
            _isDiscovery = false;

            // 決定ボタン非表示
            _okButton.SetActive(false);

            // プレイヤー名入力欄へ戻る
            ShowPlayerList(false);
        }

        private void Update()
        {
            if (_isError && Input.GetMouseButtonUp(0))
            {
                // SE再生
                SoundManager.Play(SoundManager.SE.Select);

                // 通信イベント削除
                NetworkManager.OnConnected -= OnConnected;
                NetworkManager.OnDisconnected -= OnDisconnected;
                NetworkManager.OnDiscoveryCompleted -= OnDiscoveryCompleted;
                _isDiscovery = false;

                // 初期化して前画面へ戻る
                PreScreen = null;
                SelectedButton = ButtonType.Back;
                OnButtonClick(this, EventArgs.Empty);
            }
        }

        private void OnEnable()
        {
            Initialize();
        }

        /// <summary>
        /// 通信接続イベント
        /// </summary>
        /// <param name="player">通信相手のプレイヤー名</param>
        /// <param name="type">接続したプレイヤーのホスト/クライアント種別</param>
        private void OnConnected(string player, PeerType type)
        {
            // ホストの場合は決定ボタン表示
            if (NetworkManager.PeerType == PeerType.Host)
            {
                if (!_okButton.activeSelf)
                    _okButton.SetActive(true);
            }
            else
            {
                // クライアントの場合はプレイヤー一覧へ移動
                if (_inputField.enabled)
                {
                    ShowPlayerList(true);
                }
            }

            // プレイヤー名更新
            UpdatePlayerNames();
        }

        /// <summary>
        /// プレイヤー切断イベント
        /// </summary>
        /// <param name="name">切断したプレイヤー名</param>
        /// <param name="type">切断したプレイヤーのホスト/クライアント種別</param>
        private void OnDisconnected(string name, PeerType type)
        {
            // ホストかつ、参加者が0人になった場合は決定ボタン非表示
            if (NetworkManager.PeerType == PeerType.Host && NetworkManager.PlayerCount <= 1)
            {
                _okButton.SetActive(false);
            }

            // ホストから切断された場合はエラーメッセージ表示
            if (type == PeerType.Host)
            {
                // エラーメッセージ表示
                _errMsgCanvas.enabled = true;
                _errMsgText.text = HOST_DISCONNECT;
                _isError = true;
                return;
            }

            // プレイヤー名更新
            UpdatePlayerNames();
        }

        /// <summary>
        /// プレイヤー探索完了イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDiscoveryCompleted(object sender, EventArgs e)
        {
            // イベント削除
            NetworkManager.OnConnected -= OnConnected;
            NetworkManager.OnDisconnected -= OnDisconnected;
            NetworkManager.OnDiscoveryCompleted -= OnDiscoveryCompleted;

            // 探索終了
            _isDiscovery = false;

            // ボタン選択イベント発火
            SoundManager.Play(SoundManager.SE.Select);
            SelectedButton = ButtonType.Ok;
            OnButtonClick(this, EventArgs.Empty);
        }

        /// <summary>
        /// プレイヤー一覧画面の表示を切り替える
        /// </summary>
        /// <param name="show">表示する場合はtrue</param>
        private void ShowPlayerList(bool show)
        {
            _inputFieldCanvas.enabled = !show;
            _playerListCanvas.enabled = show;
            PreScreen?.SetActive(!show);
            _discoverMask.enabled = false;
        }

        /// <summary>
        /// 各プレイヤー名を最新に更新
        /// </summary>
        private void UpdatePlayerNames()
        {
            // 募集中へ初期化
            _2pText.text = NON_PLAYER_TEXT;
            _2pText.color = _nonPlayerTextColor;
            _3pText.text = NON_PLAYER_TEXT;
            _3pText.color = _nonPlayerTextColor;
            _4pText.text = NON_PLAYER_TEXT;
            _4pText.color = _nonPlayerTextColor;

            // プレイヤー名更新
            for (int i = 0; i < NetworkManager.PlayerCount; i++)
            {
                string player = NetworkManager.PlayerNames[i];

                switch (i)
                {
                    case 0:
                        _1pText.text = player;
                        _1pText.color = _playerTextColor;
                        break;

                    case 1:
                        _2pText.text = player;
                        _2pText.color = _playerTextColor;
                        break;

                    case 2:
                        _3pText.text = player;
                        _3pText.color = _playerTextColor;
                        break;

                    case 3:
                        _4pText.text = player;
                        _4pText.color = _playerTextColor;
                        break;
                }
            }
        }
    }
}