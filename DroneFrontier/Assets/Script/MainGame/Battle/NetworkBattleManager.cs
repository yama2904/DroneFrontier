using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Network
{
    public class NetworkBattleManager : MyNetworkBehaviour
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

        [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
        private NetworkDroneSpawnManager _droneSpawnManager = null;

        [SerializeField, Tooltip("アイテムスポーン管理オブジェクト")]
        private ItemSpawnManager _itemSpawnManager = null;

        [SerializeField, Tooltip("観戦モード用オブジェクト")]
        private NetworkDroneWatchar _watchingGame = null;

        [SerializeField, Tooltip("残り時間を表示するTextUI")]
        private Text _timeText = null;

        [SerializeField, Tooltip("制限時間(分)")]
        private int _gameTime = 5;

        [SerializeField, Tooltip("ゲーム終了アニメーター")]
        private Animator _finishAnimator = null;

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

        [SerializeField, Tooltip("デバッグソロモード")]
        private bool _debug = false;

        protected override void Awake()
        {
            base.Awake();

            if (_debug)
            {
                PlayerData player = new PlayerData()
                {
                    Name = "Player",
                    Weapon = WeaponType.SHOTGUN,
                    IsControl = true
                };
                PlayerList.Add(player);
            }
        }

        private async void Start()
        {
            // カーソルロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // BGM停止
            SoundManager.StopBGM();

            // ドローンをスポーン
            if (MyNetworkManager.Singleton.IsHost)
            {
                foreach (var player in PlayerList)
                {
                    NetworkBattleDrone spawnDrone = _droneSpawnManager.SpawnDrone(player.Name, player.Weapon);
                    spawnDrone.enabled = false;
                    spawnDrone.Initialize();
                    player.Drone = spawnDrone;
                    player.StockNum = spawnDrone.StockNum;
                    player.DestroyTime = 0;

                    NetworkObjectSpawner.Spawn(spawnDrone);
                }

                // ドローン破壊イベント設定
                _droneSpawnManager.DroneDestroyEvent += DroneDestroy;
                
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
                    if (drones.Length < MyNetworkManager.Singleton.PlayerCount)
                    {
                        await UniTask.Delay(100);
                        continue;
                    }

                    foreach (var drone in drones)
                    {
                        drone.enabled = false;

                        PlayerData player = new PlayerData();
                        player.Drone = drone;
                        player.StockNum = drone.StockNum;
                        player.DestroyTime = 0;
                        PlayerList.Add(player);
                    }
                    break;
                }
            }

            await new SyncHandler().WaitAsync();

            // 3秒後にカウントダウンSE再生
            await UniTask.Delay(TimeSpan.FromSeconds(3));
            SoundManager.Play(SoundManager.SE.StartCountDownD, SoundManager.MasterSEVolume);

            // カウントダウンSE終了後にゲーム開始
            await UniTask.Delay(TimeSpan.FromSeconds(4.5));
            SoundManager.Play(SoundManager.BGM.Loop, SoundManager.MasterBGMVolume * 0.4f);
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
                    Debug.Log("カメラロック");
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Debug.Log("カメラロック解除");
                }
            }
        }

        /// <summary>
        /// ゲーム終了時の初期化処理
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _droneSpawnManager.DroneDestroyEvent -= DroneDestroy;
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン</param>
        private void DroneDestroy(NetworkBattleDrone destroyDrone, NetworkBattleDrone respawnDrone)
        {
            // 破壊されたドローン情報取得
            PlayerData droneData = PlayerList.Where(x => x.Name == destroyDrone.Name).FirstOrDefault();

            // リスポーンドローンがnullの場合は残機無し
            if (respawnDrone == null)
            {
                // 操作ドローンの場合は観戦モード起動
                if (destroyDrone.IsControl)
                {
                    _watchingGame.enabled = true;
                }
                else
                {
                    // ★ToDo:観戦送信
                }
            }
            else
            {
                // ドローン初期化
                respawnDrone.Initialize();
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
        /// 全クライアントにゲーム終了を知らせる
        /// </summary>
        private void SendFinishGame()
        {
            if (MyNetworkManager.Singleton.IsClient) return;

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

            // キャンセルトークン発行
            _cancelToken.Cancel();

            // ゲーム終了アニメーション再生
            _finishAnimator.SetBool("SetFinish", true);

            // ゲーム終了SE再生
            SoundManager.Play(SoundManager.SE.Finish, SoundManager.MasterSEVolume);

            // 3秒後リザルト画面に移動
            await UniTask.Delay(TimeSpan.FromSeconds(3));
            ResultSceneManager.SetRank(ranking);
            SceneManager.LoadScene("ResultScene");
        }
    }
}