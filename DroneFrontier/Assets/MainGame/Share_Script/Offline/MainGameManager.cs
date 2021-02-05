using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    public class MainGameManager : MonoBehaviour
    {
        public static MainGameManager Singleton { get; private set; }


        //メインゲーム中か
        public static bool IsMainGaming { get; private set; } = false;

        //設定画面を開いているか
        public static bool IsConfig { get; private set; } = false;

        //マウスロック中か
        public static bool IsCursorLock { get; private set; } = true;

        //ゲーム開始のカウントダウンが鳴ったらtrue
        protected bool startFlag = false;
        public bool StartFlag { get { return startFlag; } }

        //プレイヤー人数
        public static int playerNum = 0;


        //ランキング用配列
        protected string[] ranking = new string[playerNum];


        //ゲーム終了アニメーター
        [SerializeField] Animator finishAnimator = null;

        //設定画面移動時のマスク用変数
        [SerializeField] protected Image screenMaskImage = null;

        //マスクする色
        protected Color maskColor = new Color(0, 0, 0.5f);


        [Header("デバッグ用")]
        [SerializeField] protected bool isSolo = false;
        

        protected virtual void Awake()
        {
            //シングルトンの作成
            Singleton = this;

            //ランキング配列の初期化
            ranking = new string[playerNum];
        }

        protected virtual void Start()
        {
            //カーソルロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (isSolo)
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
        }

        //変数の初期化
        protected virtual void OnDestroy()
        {
            IsMainGaming = false;
            IsConfig = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        //ゲーム開始のカウントダウンを開始する
        public virtual void PlayStartCountDown()
        {
            SoundManager.Play(SoundManager.SE.START_COUNT_DOWN_D, SoundManager.BaseSEVolume);
            Invoke(nameof(SetStartFlagTrue), 4.5f);
        }

        //ゲームの終了処理
        public virtual void FinishGame(string[] ranking)
        {
            int index = 0;
            for (; index < playerNum; index++)
            {
                if (index < 0 || index >= ranking.Length) break;  //配列の範囲外ならやめる
                this.ranking[index] = ranking[index];
            }

            //引数の配列の要素が足りなかったら空白文字で補う
            for (; index < playerNum; index++)
            {
                this.ranking[index] = "";
            }

            SoundManager.StopBGM();  //BGM停止
            StartCoroutine(FinishGameCoroutine(this.ranking));
        }

        IEnumerator FinishGameCoroutine(string[] ranking)
        {
            SetAnimatorPlay();
            yield return new WaitForSeconds(2.0f);
            SoundManager.Play(SoundManager.SE.FINISH, SoundManager.BaseSEVolume);

            yield return new WaitForSeconds(3.0f);
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


        protected virtual void PlayFinishSE()
        {
            SoundManager.Play(SoundManager.SE.FINISH, SoundManager.BaseSEVolume);
        }

        //アニメーターの再生
        protected virtual void SetAnimatorPlay()
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