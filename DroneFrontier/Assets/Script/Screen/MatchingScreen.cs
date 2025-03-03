using System;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class MatchingScreen : MyNetworkBehaviour
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
        /// 通信中にエラーが発生したか
        /// </summary>
        private bool _isError = false;
        
        /// <summary>
        /// 決定選択
        /// </summary>
        public void ClickOk()
        {
            // 全クライアントへOKボタン選択イベント送信
            SendMethod(() => MatchingCompleted(DateTime.Now.Millisecond));
        }

        /// <summary>
        /// 戻る選択
        /// </summary>
        public void ClickBack()
        {
            // イベント削除
            MyNetworkManager.Singleton.OnDiscovery -= OnDiscovery;
            MyNetworkManager.Singleton.OnDisconnect -= OnDisconnect;

            // 通信切断
            MyNetworkManager.Singleton.Disconnect();
            
            // ボタン選択イベント発火
            SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.SEVolume);
            SelectedButton = ButtonType.Back;
            OnButtonClick(this, EventArgs.Empty);
        }

        private void Update()
        {
            if (_isError && Input.GetMouseButtonUp(0))
            {
                // SE再生
                SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

                // エラーメッセージ非表示
                _errMsgCanvas.enabled = false;
                _errMsgText.text = "";
                _isError = false;

                // 前の画面に戻る
                ClickBack();
            }
        }

        private void OnEnable()
        {
            // プレイヤー名表示
            UpdatePlayerNames();

            // 通信イベント設定
            MyNetworkManager.Singleton.OnDiscovery += OnDiscovery;
            MyNetworkManager.Singleton.OnDisconnect += OnDisconnect;

            // ホスト、かつ参加者がいる場合は決定ボタン表示
            if (MyNetworkManager.Singleton.IsHost && MyNetworkManager.Singleton.PlayerCount >= 2)
            {
                _okButton.SetActive(true);
            }
            else
            {
                _okButton.SetActive(false);
            }
        }

        /// <summary>
        /// 通信相手発見イベント
        /// </summary>
        /// <param name="player">通信相手のプレイヤー名</param>
        private void OnDiscovery(string player)
        {
            // ホストの場合は決定ボタン表示
            if (MyNetworkManager.Singleton.IsHost && !_okButton.activeSelf)
            {
                _okButton.SetActive(true);
            }

            // プレイヤー名更新
            UpdatePlayerNames();
        }

        /// <summary>
        /// プレイヤー切断イベント
        /// </summary>
        /// <param name="name">切断したプレイヤー名</param>
        /// <param name="isHost">切断したプレイヤーがホストであるか</param>
        private void OnDisconnect(string name, bool isHost)
        {
            // ホストかつ、参加者が0人になった場合は決定ボタン非表示
            if (MyNetworkManager.Singleton.IsHost && MyNetworkManager.Singleton.PlayerCount <= 1)
            {
                _okButton.SetActive(false);
            }

            // ホストから切断された場合はエラーメッセージ表示
            if (isHost)
            {
                _errMsgCanvas.enabled = true;
                _errMsgText.text = HOST_DISCONNECT;
                _isError = true;
                return;
            }

            // プレイヤー名更新
            UpdatePlayerNames();
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
            for (int i = 0; i < MyNetworkManager.Singleton.PlayerCount; i++)
            {
                string player = MyNetworkManager.Singleton.PlayerNames[i];

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

        /// <summary>
        /// マッチング完了
        /// </summary>
        /// <param name="seed">全プレイヤーで共有する乱数のシード値</param>
        private void MatchingCompleted(int seed)
        {
            // イベント削除
            MyNetworkManager.Singleton.OnDiscovery -= OnDiscovery;
            MyNetworkManager.Singleton.OnDisconnect -= OnDisconnect;

            NetworkObjectSpawner.Initialize();

            // 探索停止
            MyNetworkManager.Singleton.StopDiscovery();

            // シード値設定
            UnityEngine.Random.InitState(seed);

            // ボタン選択イベント発火
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);
            SelectedButton = ButtonType.Ok;
            OnButtonClick(this, EventArgs.Empty);
        }
    }
}