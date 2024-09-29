using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Online
{
    public class MainGameManager : NetworkBehaviour
    {
        public static MainGameManager Singleton { get; private set; }

        //プレハブ
        [SerializeField] BattleManager battleManagerPrefab = null;
        [SerializeField] RaceManager raceManagerPrefab = null;

        //ゲーム終了アニメーター
        [SerializeField] Animator finishAnimator = null;

        //メインゲーム中か
        public static bool IsMainGaming { get; private set; } = false;

        //設定画面を開いているか
        public static bool IsConfig { get; private set; } = false;

        //カウントダウンが終わったらtrue
        public bool StartFlag { get; private set; } = false;

        //ランキング用配列
        string[] ranking = new string[MatchingManager.PlayerNum];


        //設定画面移動時のマスク用変数
        [SerializeField] protected Image screenMaskImage = null;

        //マスクする色
        Color maskColor = new Color(0, 0, 0.5f);


        //デバッグ用
        public static bool IsCursorLock { get; private set; } = true;
        [Header("デバッグ用")]
        [SerializeField] protected GameModeSelectManager.GameMode debugGameMode = GameModeSelectManager.GameMode.NONE;
        [SerializeField] protected bool solo = false;


        public override void OnStartClient()
        {
            base.OnStartClient();

            if (isServer)
            {
                //Managerの生成
                if (GameModeSelectManager.Mode == GameModeSelectManager.GameMode.BATTLE)
                {
                    BattleManager manager = Instantiate(battleManagerPrefab);
                    NetworkServer.Spawn(manager.gameObject);
                }
                else if (GameModeSelectManager.Mode == GameModeSelectManager.GameMode.RACE)
                {
                    RaceManager manager = Instantiate(raceManagerPrefab);
                    NetworkServer.Spawn(manager.gameObject);
                }
                else
                {
                    //エラー
                    Application.Quit();
                }
            }

            //乱数のシード値の設定
            Random.InitState(System.DateTime.Now.Millisecond);

            IsMainGaming = true;  //メインゲーム中フラグ
            BaseScreenManager.LoadScreen(BaseScreenManager.Screen.CONFIG);  //メインゲームを始めた時点で設定画面をロードする

            //設定画面に移動した際のマスクの暗さと色を設定
            screenMaskImage.color = maskColor;
            screenMaskImage.enabled = false;
        }

        void Awake()
        {
            //シングルトンの作成
            Singleton = this;

            //カーソルロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;


            //デバッグ用
            if (solo)
            {
                StartFlag = true;
            }
        }

        void Update()
        {
            //カメラロック切り替え
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                IsCursorLock = !IsCursorLock;
                if (IsCursorLock)
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

            if (!StartFlag) return;

            //設定画面を開く
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (IsConfig)
                {
                    ConfigToMainGame();
                }
                else
                {
                    MainGameToConfig();
                }
            }


            //クライアントが全て切断されたらホストもリザルト移動
            if (isServer)
            {
                if (solo) return;   //デバッグ用
                if (MatchingManager.PlayerNum <= 1)
                {
                    NetworkManager.singleton.StopHost();    //ホストを停止
                    MatchingManager.Singleton.Init();
                    ResultSceneManager.SetRank(ranking);

                    //リザルト画面に移動
                    HomeSceneManager.LoadHomeScene(BaseScreenManager.Screen.RESULT);
                }
            }
        }

        //変数の初期化
        void OnDestroy()
        {
            IsMainGaming = false;
            IsConfig = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        //カウントダウンの開始
        [ClientRpc]
        public void RpcPlayStartCountDown()
        {
            SoundManager.Play(SoundManager.SE.START_COUNT_DOWN_D, SoundManager.SEVolume);
            Invoke(nameof(SetStartFlagTrue), 4.5f);
        }

        //ゲームの終了処理
        [Server]
        public void FinishGame(string[] ranking)
        {
            //デバッグ用
            if (solo) return;


            RpcStopBGM();
            this.ranking = ranking;
            StartCoroutine(FinishGameCoroutine());
        }

        IEnumerator FinishGameCoroutine()
        {
            SetAnimatorPlay();
            yield return new WaitForSeconds(2.0f);
            RpcPlayFinishSE();

            yield return new WaitForSeconds(3.0f);
            RpcMoveResultScreen(ranking);
        }

        [ClientRpc]
        void RpcMoveResultScreen(string[] ranking)
        {
            //サーバだけ実行しない
            if (isServer) return;

            NetworkManager.singleton.StopClient();  //クライアントを停止
            MatchingManager.Singleton.Init();  //MatchingManagerの初期化
            Mirror.Discovery.CustomNetworkDiscoveryHUD.Singleton.Init();
            ResultSceneManager.SetRank(ranking);

            //リザルト画面に移動
            HomeSceneManager.LoadHomeScene(BaseScreenManager.Screen.RESULT);
        }


        //スタートフラグを立てる
        void SetStartFlagTrue()
        {
            StartFlag = true;
            SoundManager.Play(SoundManager.BGM.LOOP, SoundManager.BGMVolume * 0.4f);
        }

        [ClientRpc]
        void RpcStopBGM()
        {
            SoundManager.StopBGM();
        }

        [ClientRpc]
        void RpcPlayFinishSE()
        {
            SoundManager.Play(SoundManager.SE.FINISH, SoundManager.SEVolume);
        }

        //アニメーターの再生
        [ClientRpc]
        void SetAnimatorPlay()
        {
            finishAnimator.SetBool("SetFinish", true);
        }


        //設定画面からメインゲームに移動する
        public virtual void ConfigToMainGame()
        {
            screenMaskImage.enabled = false;
            BaseScreenManager.HideScreen();

            if (IsCursorLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
            IsConfig = false;
        }

        void MainGameToConfig()
        {
            screenMaskImage.enabled = true;     //設定画面の背景にマスクをつける
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.CONFIG);

            Cursor.lockState = CursorLockMode.None;
            IsConfig = true;
        }
    }
}