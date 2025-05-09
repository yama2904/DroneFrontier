﻿using Common;
using Cysharp.Threading.Tasks;
using Drone.Race.Network;
using Network;
using Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Race.Network
{
    public class NetworkRaceManager : NetworkBehaviour
    {
        /// <summary>
        /// 設定画面を開いているか
        /// </summary>
        public static bool IsConfig { get; set; } = false;

        [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
        private NetworkSpawnManager _droneSpawnManager = null;

        [SerializeField, Tooltip("設定画面")]
        private ConfigScreen _config = null;

        [SerializeField, Tooltip("ゲーム終了アニメーター")]
        private Animator _finishAnimator = null;

        [SerializeField, Tooltip("エラーメッセージのCanvas")]
        private Canvas _errMsgCanvas = null;

        private List<string> _goalPlayers = new List<string>();

        /// <summary>
        /// ゴール用ロックオブジェクト
        /// </summary>
        private object _goalLock = new object();

        /// <summary>
        /// ゲーム終了フラグ
        /// </summary>
        private bool _isFinished = false;

        /// <summary>
        /// 通信エラーが発生したか
        /// </summary>
        private bool _isError = false;

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

            // イベント設定
            NetworkManager.OnDisconnected += OnDisconnect;
            _config.OnButtonClick += OnConfigBackClick;
            RaceGoalTrigger.OnGoal += OnGoal;

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
            List<NetworkRaceDrone> drones = new List<NetworkRaceDrone>();
            if (NetworkManager.PeerType == PeerType.Host)
            {
                foreach (string name in NetworkManager.PlayerNames)
                {
                    NetworkRaceDrone spawnDrone = _droneSpawnManager.SpawnDrone(name);
                    drones.Add(spawnDrone);
                }
            }
            else
            {
                while (true)
                {
                    // ドローン検索
                    drones = GameObject.FindGameObjectsWithTag(TagNameConst.PLAYER).Select(x => x.GetComponent<NetworkRaceDrone>()).ToList();

                    // 全プレイヤー分生成されていない場合は待機
                    if (drones.Count < NetworkManager.PlayerCount)
                    {
                        await UniTask.Delay(100);
                        continue;
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

            // 各ドローンのスクリプト有効化
            foreach (var drone in drones)
            {
                if (drone == null) continue;
                drone.enabled = true;
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
            NetworkManager.OnDisconnected -= OnDisconnect;

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
            // ホストから切断、又はプレイヤーが自分のみになった場合はエラーメッセージ表示
            if (type == PeerType.Host || NetworkManager.PlayerCount == 1)
            {
                _errMsgCanvas.enabled = true;

                await UniTask.Delay(1000, ignoreTimeScale: true);
                _isError = true;
                return;
            }

            // ホストのみ切断対応
            if (NetworkManager.PeerType == PeerType.Host)
            {
                lock (_goalLock)
                {
                    if (_isFinished) return;
                    CheckGameFinish();
                }
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
        /// 設定画面の戻るボタン選択イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnGoal(object sender, EventArgs e)
        {
            // ホストのみ処理
            if (NetworkManager.PeerType == PeerType.Client) return;

            RaceGoalTrigger trigger = sender as RaceGoalTrigger;

            lock (_goalLock)
            {
                if (_isFinished) return;

                _goalPlayers = trigger.GoalPlayers;
                CheckGameFinish();
            }
        }

        /// <summary>
        /// ゲーム終了チェック
        /// </summary>
        private void CheckGameFinish()
        {
            // 最後の1人が残ったら終了
            if (_goalPlayers.Count == NetworkManager.PlayerCount - 1)
            {
                // 最後のプレイヤー取得
                string lastPlayer = NetworkManager.PlayerNames.Where(x => !_goalPlayers.Contains(x)).First();

                // ゴール済みプレイヤーの最後に未ゴールプレイヤーを追加してランキング設定
                string[] ranking = _goalPlayers.Concat(new string[] { lastPlayer }).ToArray();

                // ゲーム終了
                SendMethod(() => FinishGame(ranking));
                _isFinished = true;
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
        private async void FinishGame(string[] ranking)
        {
            // 切断イベント削除
            NetworkManager.OnDisconnected -= OnDisconnect;

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