using Battle.Packet;
using Battle.Spawner;
using Battle.Weapon;
using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle.Network;
using Network;
using Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Battle.Network
{
    public class NetworkBattleManager : NetworkBehaviour
    {
        /// <summary>
        /// プレイヤー情報
        /// </summary>
        public class PlayerData
        {
            /// <summary>
            /// プレイヤー名
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// サブ武器
            /// </summary>
            public WeaponType Weapon { get; set; }

            /// <summary>
            /// 操作するドローンか
            /// </summary>
            public bool IsControl { get; set; }

            /// <summary>
            /// ドローン本体情報
            /// </summary>
            public NetworkBattleDrone Drone { get; set; } = null;

            /// <summary>
            /// 残ストック数
            /// </summary>
            public int StockNum { get; set; } = 0;

            /// <summary>
            /// 前回破壊された時間
            /// </summary>
            public float DestroyTime { get; set; } = 0;
        }

        /// <summary>
        /// プレイヤーリスト
        /// </summary>
        public static List<PlayerData> PlayerList = new List<PlayerData>();

        /// <summary>
        /// アイテムを出現させるか
        /// </summary>
        public static bool IsItemSpawn { get; set; } = true;

        /// <summary>
        /// 設定画面を開いているか
        /// </summary>
        public static bool IsConfig { get; set; } = false;

        [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
        private NetworkDroneSpawnManager _droneSpawnManager = null;

        [SerializeField, Tooltip("アイテムスポーン管理オブジェクト")]
        private ItemSpawnManager _itemSpawnManager = null;

        [SerializeField, Tooltip("残り時間を表示するTextUI")]
        private Text _timeText = null;

        [SerializeField, Tooltip("制限時間(分)")]
        private int _gameTime = 5;

        [SerializeField, Tooltip("ゲーム終了アニメーター")]
        private Animator _finishAnimator = null;

        [SerializeField, Tooltip("設定画面")]
        private ConfigScreen _config = null;

        [SerializeField, Tooltip("エラーメッセージのCanvas")]
        private Canvas _errMsgCanvas = null;

        [SerializeField, Tooltip("デバッグソロモード")]
        private bool _debug = false;

        /// <summary>
        /// 制限時間のカウントダウンキャンセルトークン
        /// </summary>
        private CancellationTokenSource _cancelToken = new CancellationTokenSource();

        /// <summary>
        /// ゲーム終了フラグ
        /// </summary>
        private bool _gameFinished = false;

        /// <summary>
        /// ロック用オブジェクト
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 通信エラーが発生したか
        /// </summary>
        private bool _isError = false;

        public static void Initialize()
        {
            PlayerList.Clear();
            IsItemSpawn = true;
            IsConfig = false;
        }

        /// <summary>
        /// 設定ボタン選択
        /// </summary>
        public void ClickConfig()
        {
            SwitchConfig();
        }

        protected override void Awake()
        {
            base.Awake();

            if (_debug)
            {
                PlayerData player = new PlayerData()
                {
                    Name = "Player",
                    Weapon = WeaponType.Shotgun,
                    IsControl = true
                };
                PlayerList.Add(player);
            }

            // イベント設定
            NetworkManager.OnDisconnected += OnDisconnect;
            _config.OnButtonClick += OnConfigBackClick;

            // Config初期化
            _config.Initialize();
            IsConfig = false;
        }

        private async void Start()
        {
            // カーソルロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // BGM停止
            SoundManager.StopBGM();

            // ランダムシード値設定
            int seed = DateTime.Now.Millisecond;
            UnityEngine.Random.InitState(seed);

            // ドローンをスポーン
            if (NetworkManager.PeerType == PeerType.Host)
            {
                foreach (var player in PlayerList)
                {
                    NetworkBattleDrone spawnDrone = _droneSpawnManager.SpawnDrone(player.Name, player.Weapon);
                    player.Drone = spawnDrone;
                    player.StockNum = spawnDrone.StockNum;
                    player.DestroyTime = 0;

                    NetworkObjectSpawner.Spawn(spawnDrone);
                }

                // ドローン破壊イベント設定
                _droneSpawnManager.OnDroneDestroy += OnDroneDestroy;

                // アイテムスポナー初期化
                _itemSpawnManager.Initialize(IsItemSpawn);
            }
            else
            {
                // アイテムスポナー初期化
                _itemSpawnManager.Initialize(false);

                while (true)
                {
                    // ドローン検索
                    var drones = GameObject.FindGameObjectsWithTag(TagNameConst.PLAYER).Select(x => x.GetComponent<NetworkBattleDrone>()).ToArray();

                    // 全プレイヤー分生成されていない場合は待機
                    if (drones.Length < NetworkManager.PlayerCount)
                    {
                        await UniTask.Delay(100);
                        continue;
                    }

                    foreach (var drone in drones)
                    {
                        PlayerData player = new PlayerData();
                        player.Drone = drone;
                        player.StockNum = drone.StockNum;
                        player.DestroyTime = 0;
                        PlayerList.Add(player);
                    }
                    break;
                }
            }

            // 同期してランダムシード値も共有
            object value = await new SyncHandler().SyncValueAsync(seed);
            if (NetworkManager.PeerType == PeerType.Client)
            {
                UnityEngine.Random.InitState(Convert.ToInt32(value));
            }

            // 3秒後にカウントダウンSE再生
            await UniTask.Delay(TimeSpan.FromSeconds(3));
            SoundManager.Play(SoundManager.SE.StartCountDownD);

            // カウントダウンSE終了後にゲーム開始
            await UniTask.Delay(TimeSpan.FromSeconds(4.5));
            SoundManager.Play(SoundManager.BGM.Loop, 0.4f);
            StartCountDown().Forget();

            // 各ドローンのスクリプト有効化
            foreach (var drone in PlayerList)
            {
                drone.Drone.enabled = true;
            }
        }

        private void Update()
        {
            // カメラロック切り替え
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            // 設定画面を開く
            if (Input.GetKeyDown(KeyCode.M))
            {
                SwitchConfig();
            }

            // 通信エラーの場合はクリックでホーム画面へ戻る
            if (_isError)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    SoundManager.Play(SoundManager.SE.Select);
                    SceneManager.LoadScene("HomeScene");
                }
            }
        }

        /// <summary>
        /// ゲーム終了時の初期化処理
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            // カーソル戻す
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // イベント削除
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;
            NetworkManager.OnDisconnected -= OnDisconnect;

            // 初期化
            Initialize();

            // キャンセルトークン発行
            _cancelToken.Cancel();

            // 切断
            NetworkManager.Disconnect();
        }

        /// <summary>
        /// プレイヤー切断イベント
        /// </summary>
        /// <param name="name">切断したプレイヤー名</param>
        /// <param name="type">切断したプレイヤーのホスト/クライアント種別</param>
        private async void OnDisconnect(string name, PeerType type)
        {
            // ホストから切断、又はプレイヤーが自分のみの場合はエラーメッセージ表示
            if (type == PeerType.Host || NetworkManager.PlayerCount == 1)
            {
                _errMsgCanvas.enabled = true;

                await UniTask.Delay(1000, ignoreTimeScale: true);
                _isError = true;
                return;
            }
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン</param>
        private void OnDroneDestroy(NetworkBattleDrone destroyDrone, NetworkBattleDrone respawnDrone)
        {
            // 破壊されたドローン情報取得
            PlayerData droneData = PlayerList.Where(x => x.Name == destroyDrone.Name).FirstOrDefault();

            // リスポーンドローンがnullの場合は残機無し
            if (respawnDrone == null)
            {
                // 操作ドローンの場合は観戦モード起動
                if (destroyDrone.IsControl)
                {
                    NetworkDroneWatcher.Run();
                }
                else
                {
                    // 観戦送信
                    NetworkManager.SendTcpToPlayer(new DroneWatchPacket(), destroyDrone.Name);
                }
            }
            else
            {
                NetworkObjectSpawner.Spawn(respawnDrone);
            }

            // 破壊されたドローン情報更新
            droneData.Drone = respawnDrone;
            droneData.StockNum--;
            droneData.DestroyTime = Time.time;

            // 残り1人になった場合はゲーム終了
            List<PlayerData> aliveDrones = PlayerList.Where(x => x.Drone == null).ToList();
            if (aliveDrones.Count == 1)
            {
                SendFinishGame();
            }
        }

        /// <summary>
        /// 設定画面の戻るボタン選択イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnConfigBackClick(object sender, EventArgs e)
        {
            if (_config.SelectedButton == ConfigScreen.ButtonType.Back)
            {
                SwitchConfig();
            }
        }

        /// <summary>
        /// 制限時間のカウントダウン開始
        /// </summary>
        /// <returns></returns>
        private async UniTask StartCountDown()
        {
            try
            {
                // 1分ごとに残り時間を表示する
                for (int time = _gameTime - 1; time > 0; time--)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(60), cancellationToken: _cancelToken.Token);

                    _timeText.enabled = true;
                    _timeText.text = $"残 り {time} 分";

                    // 4秒後に非表示
                    UniTask.Void(async () =>
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(4), cancellationToken: _cancelToken.Token);
                        _timeText.enabled = false;
                    });
                }

                // 残り10秒になったら毎秒残り時間を表示
                await UniTask.Delay(TimeSpan.FromSeconds(50), cancellationToken: _cancelToken.Token);
                _timeText.enabled = true;
                for (int i = 10; i > 0; i--)
                {
                    _timeText.text = $"残 り {i} 秒";
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: _cancelToken.Token);
                }

                // 制限時間によるゲーム終了
                SendFinishGame();
            }
            catch (OperationCanceledException)
            {
                // ゲーム終了によるカウントダウンキャンセル

                // 残り時間非表示
                _timeText.enabled = false;
            }
        }

        /// <summary>
        /// 設定画面の表示切り替え
        /// </summary>
        private void SwitchConfig()
        {
            _config.gameObject.SetActive(!IsConfig);
            IsConfig = !IsConfig;
        }

        /// <summary>
        /// 全クライアントにゲーム終了を知らせる
        /// </summary>
        private void SendFinishGame()
        {
            if (NetworkManager.PeerType == PeerType.Client) return;

            // [残ストック数 DESC, 破壊された時間 DESC]でソートしてランキング設定
            string[] ranking = PlayerList.OrderByDescending(x => x.StockNum)
                                         .ThenByDescending(x => x.DestroyTime)
                                         .Select(x => x.Name)
                                         .ToArray();
            SendMethod(() => FinishGame(ranking));
        }

        /// <summary>
        /// ゲーム終了処理
        /// </summary>
        private async void FinishGame(string[] ranking)
        {
            // ゲーム終了のバッティング防止
            lock (_lock)
            {
                if (_gameFinished) return;
                _gameFinished = true;
            }

            // 切断イベント削除
            NetworkManager.OnDisconnected -= OnDisconnect;

            // キャンセルトークン発行
            _cancelToken.Cancel();

            // ゲーム終了アニメーション再生
            _finishAnimator.SetBool("SetFinish", true);

            // ゲーム終了SE再生
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            SoundManager.Play(SoundManager.SE.Finish);

            // 3秒後リザルト画面に移動
            await UniTask.Delay(TimeSpan.FromSeconds(3));

            // 通信切断
            NetworkManager.Disconnect();

            // リザルト画面へ移動
            ResultSceneManager.SetRank(ranking);
            SceneManager.LoadScene("ResultScene");
        }
    }
}