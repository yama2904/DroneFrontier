using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MainGameManager : MonoBehaviour
{
    //マルチモードか
    public static bool IsMulti { get; set; } = false;

    //アイテムを出現させるか
    public static bool IsItem { get; set; } = true;

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

    //NonGameSceneからプレイヤー・CPUを追加する用
    public class PlayerData
    {
        public string name;
        public BaseWeapon.Weapon weapon;
        public bool isPlayer;
    }
    static List<PlayerData> playerDatas = new List<PlayerData>();

    //ゲーム上のプレイヤー・CPU情報
    [SerializeField] Player playerInspector = null;
    [SerializeField] CPUController cpuInspector = null;
    static Player player = null;
    static CPUController cpu = null;
    static List<BasePlayer> basePlayers = new List<BasePlayer>();


    //設定画面移動時のマスク用変数
    [SerializeField] Image screenMaskImageInspector = null;  //画面を暗くする画像を持っているオブジェクト
    static Image screenMaskImage = null;    //screenMaskImageをstaticに移す用

    //マスクする色
    const float MASK_COLOR_RED = 0;     //赤
    const float MASK_COLOR_GREEN = 0;   //緑
    const float MASK_COLOR_BLUE = 0;    //青
    const float MASK_COLOR_ALFA = 0.5f; //アルファ


    //デバッグ用
    public static bool IsCursorLock { get; private set; } = true;
    [SerializeField] bool offline = false;

    void Awake()
    {
        player = playerInspector;
        cpu = cpuInspector;
        screenMaskImage = screenMaskImageInspector;


        //デバッグ用
        IsMulti = !offline;
    }

    void Start()
    {
        IsMainGaming = true;
        BaseScreenManager.LoadScreen(BaseScreenManager.Screen.CONFIG);  //メインゲームを始めた時点で設定画面をロードする

        //設定画面に移動した際のマスクの暗さと色を設定
        screenMaskImage.color = new Color(MASK_COLOR_RED, MASK_COLOR_GREEN, MASK_COLOR_BLUE, MASK_COLOR_ALFA);
        screenMaskImage.enabled = false;


        ////プレイヤーとCPUを配置
        //for (int i = 0; i < playerDatas.Count; i++)
        //{
        //    BasePlayer p;
        //    if (playerDatas[i].isPlayer)
        //    {
        //        p = Instantiate(player);
        //    }
        //    else
        //    {
        //        CPUController c = Instantiate(cpu);
        //        c.SetSubWeapon(playerDatas[i].weapon);
        //        p = c;
        //    }
        //    p.transform.Translate(0, 0, i * 2.0f);
        //    p.name = playerDatas[i].name;

        //    basePlayers.Add(p);
        //}

        ////デバッグ用
        //if (playerDatas.Count == 0)
        //{
        //    GameObject[] p = GameObject.FindGameObjectsWithTag(TagNameManager.PLAYER);
        //    foreach (GameObject o in p)
        //    {
        //        basePlayers.Add(o.GetComponent<BasePlayer>());
        //    }
        //    GameObject[] c = GameObject.FindGameObjectsWithTag(TagNameManager.CPU);
        //    foreach (GameObject o in c)
        //    {
        //        basePlayers.Add(o.GetComponent<BasePlayer>());
        //    }
        //}

        //カーソルロック
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
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

        //破壊されたドローンがあるか調べる
        for (int i = basePlayers.Count - 1; i >= 0; i--)
        {
            //破壊されていたらランキング用リストに名前を入れてドローンをリストから削除
            if (basePlayers[i].IsDestroy)
            {
                ResultButtonsController.Rank rank = (ResultButtonsController.Rank)basePlayers.Count - 1;
                ResultButtonsController.SetRank(basePlayers[i].name, rank);
                basePlayers.RemoveAt(i);
            }
        }

        //ドローンが1機に残ったらリザルトに移動
        if (basePlayers.Count == 1)
        {
            ResultButtonsController.SetRank(basePlayers[0].name, ResultButtonsController.Rank.RANK_1ST);
            Invoke(nameof(MoveResult), 3.0f);
        }


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
    }

    //変数の初期化
    void Init()
    {
        IsMainGaming = false;
        IsMulti = false;
        IsConfig = false;
        Mode = GameMode.NONE;
        playerDatas.Clear();
        basePlayers.Clear();
        Cursor.lockState = CursorLockMode.None;
    }

    void MainGameToConfig()
    {
        screenMaskImage.enabled = true;     //設定画面の背景にマスクをつける
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.CONFIG);

        Cursor.lockState = CursorLockMode.None;
        IsConfig = true;
    }

    void MoveResult()
    {
        Init();
        NonGameManager.MainGameToResult();
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

    //プレイヤー又はCPUを追加する
    public static void SetPlayer(string name, BaseWeapon.Weapon weapon, bool isPlayer)
    {
        //不正な値なら弾く
        if (name == "" || weapon == BaseWeapon.Weapon.NONE)
        {
            return;
        }

        PlayerData pd = new PlayerData();
        pd.name = name;
        pd.weapon = weapon;
        pd.isPlayer = isPlayer;

        playerDatas.Add(pd);
    }
}