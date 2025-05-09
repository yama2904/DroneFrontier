﻿using Battle.Drone;
using Battle.Spawner;
using Battle.Weapon;
using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Battle
{
    public class BattleManager : MonoBehaviour
    {
        /// <summary>
        /// ドローン情報
        /// </summary>
        private class DroneData
        {
            /// <summary>
            /// ドローン名
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// サブ武器
            /// </summary>
            public WeaponType Weapon { get; set; }

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
        /// 生存中のプレイヤー数
        /// </summary>
        public static int AliveDroneCount => _droneList.Where(x => x.Drone != null).Count();

        /// <summary>
        /// プレイヤーのサブ武器
        /// </summary>
        public static WeaponType PlayerWeapon { get; set; } = WeaponType.None;

        /// <summary>
        /// アイテムを出現させるか
        /// </summary>
        public static bool IsItemSpawn { get; set; } = true;

        /// <summary>
        /// 設定画面を開いているか
        /// </summary>
        public static bool IsConfig { get; set; } = false;

        [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
        private DroneSpawnManager _droneSpawnManager = null;

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

        /// <summary>
        /// 各ドローン情報
        /// </summary>
        private static List<DroneData> _droneList = new List<DroneData>();

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

        public static void Initialize()
        {
            _droneList.Clear();
            PlayerWeapon = WeaponType.None;
            IsItemSpawn = true;
            IsConfig = false;
        }

        public static void AddCpu(string name, WeaponType subWeapon)
        {
            _droneList.Add(new DroneData()
            {
                Name = name,
                Weapon = subWeapon
            });
        }

        /// <summary>
        /// 設定ボタン選択
        /// </summary>
        public void ClickConfig()
        {
            SwitchConfig();
        }

        private void Awake()
        {
            // イベント設定
            _config.OnButtonClick += OnConfigBackClick;

            // 乱数のシード値の設定
            UnityEngine.Random.InitState(DateTime.Now.Millisecond);

            // Config初期化
            _config.Initialize();
            IsConfig = false;
        }

        private async void Start()
        {
            // カーソルロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // CPUドローンをスポーン
            foreach (DroneData data in _droneList)
            {
                IBattleDrone cpuDrone = _droneSpawnManager.SpawnDrone(data.Name, data.Weapon, false);
                (cpuDrone as CpuBattleDrone).enabled = false;
                data.Drone = cpuDrone;
                data.StockNum = cpuDrone.StockNum;
                data.DestroyTime = 0;
            }

            // プレイヤードローンをスポーン
            IBattleDrone spawnDrone = _droneSpawnManager.SpawnDrone("Player", PlayerWeapon, true);
            (spawnDrone as BattleDrone).enabled = false;
            DroneData droneData = new DroneData()
            {
                Name = "Player",
                Weapon = PlayerWeapon,
                Drone = spawnDrone,
                StockNum = spawnDrone.StockNum,
                DestroyTime = 0
            };
            _droneList.Insert(0, droneData);

            // ドローン破壊イベント設定
            _droneSpawnManager.OnDroneDestroy += OnDroneDestroy;

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
            foreach (DroneData drone in _droneList)
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

            // 設定画面を開く
            if (Input.GetKeyDown(KeyCode.M))
            {
                SwitchConfig();
            }
        }

        /// <summary>
        /// ゲーム終了時の初期化処理
        /// </summary>
        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン</param>
        private void OnDroneDestroy(IBattleDrone destroyDrone, IBattleDrone respawnDrone)
        {
            // 破壊されたドローン情報取得
            DroneData droneData = _droneList.Where(x => x.Name == destroyDrone.Name).FirstOrDefault();

            // リスポーンドローンがnullの場合は残機無し
            if (respawnDrone == null)
            {
                // プレイヤーの場合は観戦モード起動
                if (destroyDrone is BattleDrone)
                {
                    DroneWatcher.Run();
                }
            }

            // 破壊されたドローン情報更新
            droneData.Drone = respawnDrone;
            droneData.StockNum--;
            droneData.DestroyTime = Time.time;

            // 残り1人になった場合はゲーム終了
            if (AliveDroneCount == 1)
            {
                FinishGame();
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

                // 残り時間非表示
                _timeText.enabled = false;

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
        /// 設定画面の表示切り替え
        /// </summary>
        private void SwitchConfig()
        {
            if (IsConfig)
            {
                _config.Hide();
            }
            else
            {
                _config.Show();
            }
            IsConfig = !IsConfig;
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
            string[] ranking = _droneList.OrderByDescending(x => x.StockNum)
                                         .ThenByDescending(x => x.DestroyTime)
                                         .Select(x => x.Name)
                                         .ToArray();
            ResultSceneManager.SetRank(ranking);

            // 3秒後リザルト画面に移動
            await UniTask.Delay(TimeSpan.FromSeconds(3));
            SceneManager.LoadScene("ResultScene");
        }
    }
}