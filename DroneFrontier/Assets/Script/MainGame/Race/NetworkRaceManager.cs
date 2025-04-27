using Common;
using Cysharp.Threading.Tasks;
using Drone.Network;
using Drone.Race.Network;
using Network;
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
            NetworkManager.Singleton.OnDisconnect += OnDisconnect;
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
            if (NetworkManager.Singleton.IsHost)
            {
                foreach (string name in NetworkManager.Singleton.PlayerNames)
                {
                    NetworkRaceDrone spawnDrone = _droneSpawnManager.SpawnDrone(name);
                    drones.Add(spawnDrone);
                    NetworkObjectSpawner.Spawn(spawnDrone);
                }
            }
            else
            {
                while (true)
                {
                    // ドローン検索
                    drones = GameObject.FindGameObjectsWithTag(TagNameConst.PLAYER).Select(x => x.GetComponent<NetworkRaceDrone>()).ToList();

                    // 全プレイヤー分生成されていない場合は待機
                    if (drones.Count < NetworkManager.Singleton.PlayerCount)
                    {
                        await UniTask.Delay(100);
                        continue;
                    }
                    break;
                }
            }

            // 同期してランダムシード値も共有
            object value = await new SyncHandler().SyncValueAsync(seed);
            if (NetworkManager.Singleton.IsClient)
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
            NetworkManager.Singleton.OnDisconnect -= OnDisconnect;

            // 切断
            NetworkManager.Singleton.Disconnect();
        }

        /// <summary>
        /// プレイヤー切断イベント
        /// </summary>
        /// <param name="name">切断したプレイヤー名</param>
        /// <param name="isHost">切断したプレイヤーがホストであるか</param>
        private async void OnDisconnect(string name, bool isHost)
        {
            // ホストから切断、又はプレイヤーが自分のみの場合はエラーメッセージ表示
            if (isHost || NetworkManager.Singleton.PlayerCount == 1)
            {
                _errMsgCanvas.enabled = true;

                await UniTask.Delay(1000, ignoreTimeScale: true);
                _isError = true;
                return;
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
            RaceGoalTrigger trigger = sender as RaceGoalTrigger;

            lock (_goalLock)
            {
                if (_isFinished) return;

                // 最後の1人が残ったら終了
                if (trigger.GoalPlayers.Count == NetworkManager.Singleton.PlayerCount - 1)
                {
                    if (NetworkManager.Singleton.IsHost)
                    {
                        // 最後のプレイヤー取得
                        string lastPlayer = NetworkManager.Singleton.PlayerNames.Where(x => !trigger.GoalPlayers.Contains(x)).First();

                        // ゴール済みプレイヤーの最後に未ゴールプレイヤーを追加してランキング設定
                        string[] ranking = trigger.GoalPlayers.Concat(new string[] { lastPlayer }).ToArray();

                        // ゲーム終了
                        SendMethod(() => FinishGame(ranking));
                        _isFinished = true;
                    }
                }
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
        /// ゲーム終了処理
        /// </summary>
        private async void FinishGame(string[] ranking)
        {
            // 切断イベント削除
            NetworkManager.Singleton.OnDisconnect -= OnDisconnect;

            // ゲーム終了アニメーション再生
            _finishAnimator.SetBool("SetFinish", true);

            // ゲーム終了SE再生
            SoundManager.Play(SoundManager.SE.Finish);

            // 3秒後リザルト画面に移動
            await UniTask.Delay(TimeSpan.FromSeconds(3));

            // 通信切断
            NetworkManager.Singleton.Disconnect();

            // リザルト画面へ移動
            ResultSceneManager.SetRank(ranking);
            SceneManager.LoadScene("ResultScene");
        }
    }
}