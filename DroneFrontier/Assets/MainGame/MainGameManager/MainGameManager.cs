﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;


public class MainGameManager : NetworkBehaviour
{
    //シングルトン
    static MainGameManager singleton;
    public static MainGameManager Singleton { get { return singleton; } }

    [SerializeField] BattleManager battleManager = null;
    [SerializeField] RaceManager raceManager = null;
    [SerializeField] Animator finishAnimator = null;
    
    //マルチモードか
    public static bool IsMulti { get; set; } = false;

    //アイテムを出現させるか
    public static bool IsItemSpawn { get; set; } = true;

    //メインゲーム中か
    public static bool IsMainGaming { get; private set; } = false;

    //設定画面を開いているか
    public static bool IsConfig { get; private set; } = false;

    //ゲームモード
    public enum GameMode
    {
        BATTLE,   //バトルモード
        RACE,     //レースモード

        NONE
    }
    public static GameMode Mode { get; set; } = GameMode.NONE;  //選んでいるゲームモード


    //ゲーム開始のカウントダウンが鳴ったらtrue
    bool startFlag = false;
    public bool StartFlag { get { return startFlag; } }

    string[] ranking = new string[MatchingManager.PlayerNum];


    //設定画面移動時のマスク用変数
    [SerializeField] Image screenMaskImageInspector = null;  //画面を暗くする画像を持っているオブジェクト
    static Image screenMaskImage = null;    //screenMaskImageをstaticに移す用

    //マスクする色
    Color maskColor = new Color(0, 0, 0.5f);


    //デバッグ用
    public static bool IsCursorLock { get; private set; } = true;
    [Header("デバッグ用")]
    [SerializeField] GameMode debugGameMode = GameMode.NONE;
    [SerializeField] bool solo = false;


    //未使用
    //NonGameSceneからプレイヤー・CPUを追加する用
    public class PlayerData
    {
        public string name;
        public BaseWeapon.Weapon weapon;
        public bool isPlayer;
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        //乱数のシード値の設定
        Random.InitState(System.DateTime.Now.Millisecond);
    }

    void Awake()
    {
        //シングルトンの作成
        singleton = this;

        screenMaskImage = screenMaskImageInspector;


        //デバッグ用
        IsMulti = true;
        if (solo)
        {
            startFlag = true;
        }
    }

    void Start()
    {
        if (isServer)
        {
            if (Mode == GameMode.BATTLE)
            {
                BattleManager bm = Instantiate(battleManager);
                NetworkServer.Spawn(bm.gameObject);
            }
            else if (Mode == GameMode.RACE)
            {
                RaceManager rm = Instantiate(raceManager);
                NetworkServer.Spawn(rm.gameObject);
            }
            else
            {
                //デバッグ用
                if (debugGameMode == GameMode.BATTLE)
                {
                    BattleManager bm = Instantiate(battleManager);
                    NetworkServer.Spawn(bm.gameObject);
                }
                else if (debugGameMode == GameMode.RACE)
                {
                    RaceManager rm = Instantiate(raceManager);
                    NetworkServer.Spawn(rm.gameObject);
                }
                else
                {
                    //エラー
                    Application.Quit();
                }
            }

            //3秒後にカウントダウンSE
            Invoke(nameof(RpcPlayStartSE), 3.0f);
        }


        IsMainGaming = true;
        BaseScreenManager.LoadScreen(BaseScreenManager.Screen.CONFIG);  //メインゲームを始めた時点で設定画面をロードする

        //設定画面に移動した際のマスクの暗さと色を設定
        screenMaskImage.color = maskColor;
        screenMaskImage.enabled = false;

        //カーソルロック
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //デバッグ用
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
                ResultButtonsController.SetRank(ranking);

                //リザルト画面に移動
                NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.RESULT);
            }
        }
    }

    //変数の初期化
    private void OnDestroy()
    {
        IsMainGaming = false;
        IsMulti = false;
        IsConfig = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    //ゲームの終了処理
    [Server]
    public void FinishGame(string[] ranking)
    {
        //デバッグ用
        if (solo) return;


        int index = 0;
        for (; index < MatchingManager.PlayerNum; index++)
        {
            if (index < 0 || index >= ranking.Length) break;  //配列の範囲外ならやめる
            this.ranking[index] = ranking[index];
        }

        //引数の配列の要素が足りなかったら空白文字で補う
        for (; index < MatchingManager.PlayerNum; index++)
        {
            this.ranking[index] = "";
        }

        RpcStopBGM();
        StartCoroutine(FinishGameCoroutine(this.ranking));
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
        ResultButtonsController.SetRank(ranking);

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
    void RpcPlayStartSE()
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
    public static void ConfigToMainGame()
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
    


    //未使用
    public static void SetPlayer(string name, BaseWeapon.Weapon weapon, bool isPlayer)
    {
    }
}