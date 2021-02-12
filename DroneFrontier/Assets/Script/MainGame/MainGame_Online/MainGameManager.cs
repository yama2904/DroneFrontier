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

        //ゲーム終了アニメーター
        [SerializeField] Animator finishAnimator = null;

        //メインゲーム中か
        public static bool IsMainGaming { get; private set; } = false;

        //設定画面を開いているか
        public static bool IsConfig { get; private set; } = false;


        //ゲーム開始のカウントダウンが鳴ったらtrue
        protected bool startFlag = false;
        public bool StartFlag { get { return startFlag; } }

        //ランキング用配列
        protected string[] ranking = new string[MatchingManager.PlayerNum];


        //設定画面移動時のマスク用変数
        [SerializeField] protected Image screenMaskImage = null;

        //マスクする色
        protected Color maskColor = new Color(0, 0, 0.5f);


        //デバッグ用
        public static bool IsCursorLock { get; private set; } = true;
        [Header("デバッグ用")]
        [SerializeField] protected GameModeSelectScreenManager.GameMode debugGameMode = GameModeSelectScreenManager.GameMode.NONE;
        [SerializeField] protected bool solo = false;


        public override void OnStartClient()
        {
            base.OnStartClient();

            //乱数のシード値の設定
            Random.InitState(System.DateTime.Now.Millisecond);

            IsMainGaming = true;  //メインゲーム中フラグ
            BaseScreenManager.LoadScreen(BaseScreenManager.Screen.CONFIG);  //メインゲームを始めた時点で設定画面をロードする

            //設定画面に移動した際のマスクの暗さと色を設定
            screenMaskImage.color = maskColor;
            screenMaskImage.enabled = false;
        }

        protected virtual void Awake()
        {
            //シングルトンの作成
            Singleton = this;

            //カーソルロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;


            //デバッグ用
            if (solo)
            {
                startFlag = true;
            }
        }

        protected virtual void Update()
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

            if (!startFlag) return;

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
                    ResultScreenManager.SetRank(ranking);

                    //リザルト画面に移動
                    NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.RESULT);
                }
            }
        }

        //変数の初期化
        protected virtual void OnDestroy()
        {
            IsMainGaming = false;
            IsConfig = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        //ゲームの終了処理
        [Server]
        public void FinishGame()
        {
            //デバッグ用
            if (solo) return;


            RpcStopBGM();
            StartCoroutine(FinishGameCoroutine(ranking));
        }

        IEnumerator FinishGameCoroutine(string[] ranking)
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
            ResultScreenManager.SetRank(ranking);

            //リザルト画面に移動
            NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.RESULT);
        }


        //スタートフラグを立てる
        void SetStartFlagTrue()
        {
            startFlag = true;
            SoundManager.Play(SoundManager.BGM.LOOP, SoundManager.BaseBGMVolume * 0.4f);
        }

        [ClientRpc]
        void RpcStopBGM()
        {
            SoundManager.StopBGM();
        }

        [ClientRpc]
        protected void RpcPlayStartCountDown()
        {
            SoundManager.Play(SoundManager.SE.START_COUNT_DOWN_D, SoundManager.BaseSEVolume);
            Invoke(nameof(SetStartFlagTrue), 4.5f);
        }

        [ClientRpc]
        void RpcPlayFinishSE()
        {
            SoundManager.Play(SoundManager.SE.FINISH, SoundManager.BaseSEVolume);
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

        protected virtual void MainGameToConfig()
        {
            screenMaskImage.enabled = true;     //設定画面の背景にマスクをつける
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.CONFIG);

            Cursor.lockState = CursorLockMode.None;
            IsConfig = true;
        }
    }
}