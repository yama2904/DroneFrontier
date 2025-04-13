using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Offline
{
    public class BattleManager : MonoBehaviour
    {
        /// <summary>
        /// CPU情報
        /// </summary>
        public class CpuData
        {
            public string Name { get; set; }
            public WeaponType Weapon { get; set; }
        }

        /// <summary>
        /// CPUリスト
        /// </summary>
        public static List<CpuData> CpuList = new List<CpuData>();

        /// <summary>
        /// プレイヤーのサブ武器
        /// </summary>
        public static WeaponType PlayerWeapon { get; set; } = WeaponType.NONE;

        /// <summary>
        /// アイテムを出現させるか
        /// </summary>
        public static bool IsItemSpawn { get; set; } = true;

        [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
        private DroneSpawnManager _droneSpawnManager = null;

        [SerializeField, Tooltip("アイテムスポーン管理オブジェクト")]
        private ItemSpawnManager _itemSpawnManager = null;

        [SerializeField, Tooltip("観戦モード用オブジェクト")]
        private DroneWatcher _watchingGame = null;

        [SerializeField, Tooltip("残り時間を表示するTextUI")]
        private Text _timeText = null;

        [SerializeField, Tooltip("制限時間(分)")]
        private int _gameTime = 5;

        [SerializeField, Tooltip("ゲーム終了アニメーター")]
        private Animator _finishAnimator = null;

        /// <summary>
        /// ドローン情報
        /// </summary>
        private class DroneData
        {
            /// <summary>
            /// ドローン本体情報
            /// </summary>
            public IBattleDrone Drone { get; set; } = null;

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
        /// 各ドローン情報
        /// </summary>
        private Dictionary<string, DroneData> _droneDatas = new Dictionary<string, DroneData>();

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

        private void Awake()
        {
            // 乱数のシード値の設定
            UnityEngine.Random.InitState(DateTime.Now.Millisecond);
        }

        private async void Start()
        {
            // カーソルロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // プレイヤードローンをスポーン
            IBattleDrone spawnDrone = _droneSpawnManager.SpawnDrone("Player", PlayerWeapon, true);
            spawnDrone.Initialize();
            DroneData droneData = new DroneData()
            {
                Drone = spawnDrone,
                StockNum = spawnDrone.StockNum,
                DestroyTime = 0
            };
            _droneDatas.Add(spawnDrone.Name, droneData);

            // カウントダウン終了までスクリプト無効化
            (spawnDrone as BattleDrone).enabled = false;

            // CPUドローンをスポーン
            foreach (CpuData cpu in CpuList)
            {
                spawnDrone = _droneSpawnManager.SpawnDrone(cpu.Name, cpu.Weapon, false);
                spawnDrone.Initialize();
                droneData = new DroneData()
                {
                    Drone = spawnDrone,
                    StockNum = spawnDrone.StockNum,
                    DestroyTime = 0
                };
                _droneDatas.Add(cpu.Name, droneData);

                // カウントダウン終了までスクリプト無効化
                (spawnDrone as CpuBattleDrone).enabled = false;
            }

            // ドローン破壊イベント設定
            _droneSpawnManager.DroneDestroyEvent += DroneDestroy;

            // アイテムスポナー初期化
            _itemSpawnManager.Initialize(IsItemSpawn);

            // BGM停止
            SoundManager.StopBGM();

            // 3秒後にカウントダウンSE再生
            await UniTask.Delay(TimeSpan.FromSeconds(3));
            SoundManager.Play(SoundManager.SE.StartCountDownD);

            // カウントダウンSE終了後にゲーム開始
            await UniTask.Delay(TimeSpan.FromSeconds(4.5));
            SoundManager.Play(SoundManager.BGM.Loop, 0.4f);
            StartCountDown().Forget();

            // 各ドローンのスクリプト有効化
            foreach (DroneData drone in _droneDatas.Values)
            {
                if (drone.Drone is BattleDrone player)
                {
                    player.enabled = true;
                }
                else
                {
                    (drone.Drone as CpuBattleDrone).enabled = true;
                }
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
        }

        /// <summary>
        /// ゲーム終了時の初期化処理
        /// </summary>
        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _droneSpawnManager.DroneDestroyEvent -= DroneDestroy;
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
                FinishGame();
            }
            catch (OperationCanceledException)
            {
                // ゲーム終了によるカウントダウンキャンセル

                // 残り時間非表示
                _timeText.enabled = false;
            }
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン</param>
        private void DroneDestroy(IBattleDrone destroyDrone, IBattleDrone respawnDrone)
        {
            // 破壊されたドローン情報取得
            DroneData droneData = _droneDatas[destroyDrone.Name];

            // リスポーンドローンがnullの場合は残機無し
            if (respawnDrone == null)
            {
                // プレイヤーの場合は観戦モード起動
                if (destroyDrone is BattleDrone)
                {
                    _watchingGame.enabled = true;
                }
            }
            else
            {
                respawnDrone.Initialize();
            }

            // 破壊されたドローン情報更新
            droneData.Drone = respawnDrone;
            droneData.StockNum--;
            droneData.DestroyTime = Time.time;

            // 残り1人になった場合はゲーム終了
            List<DroneData> aliveDrones = _droneDatas.Where(x => x.Value.Drone == null)
                                                     .Select(x => x.Value)
                                                     .ToList();
            if (aliveDrones.Count == 1)
            {
                FinishGame();
            }
        }

        /// <summary>
        /// ゲーム終了処理
        /// </summary>
        private async void FinishGame()
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
            SoundManager.Play(SoundManager.SE.Finish);

            // [残ストック数 DESC, 破壊された時間 DESC]でソートしてランキング設定
            string[] ranking = _droneDatas.OrderByDescending(x => x.Value.StockNum)
                                          .ThenByDescending(x => x.Value.DestroyTime)
                                          .Select(x => x.Key).ToArray();
            ResultSceneManager.SetRank(ranking);

            // 3秒後リザルト画面に移動
            await UniTask.Delay(TimeSpan.FromSeconds(3));
            SceneManager.LoadScene("ResultScene");
        }
    }
}