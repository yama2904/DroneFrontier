using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BattleManager : NetworkBehaviour
{
    //シングルトン
    static BattleManager singleton;
    public static BattleManager Singleton { get { return singleton; } }

    //プレイヤー情報
    public class PlayerData
    {
        public BattleDrone drone = null;
        public int ranking = 1;
        public bool isDestroy = false;
        public static int droneNum = 0;  //残っているドローンの数
    }
    static List<PlayerData> playerDatas = new List<PlayerData>();

    //ランキング
    string[] ranking;

    //ゲーム開始のカウントダウンが鳴ったらtrue
    bool startFlag = false;
    public bool StartFlag { get { return startFlag; } }

    public int disconnectionClientCount = 0;
    bool isFinished = false;


    void Awake()
    {
        //シングルトンの作成
        singleton = this;
    }

    void Start()
    {
        //ゲームが開始して3秒後にカウントダウンSEを鳴らす
        Invoke(nameof(PlayStartSE), 3.0f);
    }

    [ServerCallback]
    void Update()
    {
        if (!startFlag) return;

        //負けたプレイヤーの走査
        foreach (PlayerData pd in playerDatas)
        {
            if (pd.isDestroy) continue;
            if (pd.drone.IsGameOver)
            {
                pd.ranking = PlayerData.droneNum;   //ランキングの記録
                pd.isDestroy = true;
                PlayerData.droneNum--;  //残りドローンを減らす
            }
        }

        //最後のプレイヤーが残ったら終了処理
        if (PlayerData.droneNum <= 1)
        {
            if (!isFinished)
            {
                ranking = new string[playerDatas.Count];
                foreach (PlayerData pd in playerDatas)
                {
                    ranking[pd.ranking - 1] = pd.drone.name;
                }
                StartCoroutine(FinishGame(ranking));
                isFinished = true;
            }
        }

        //クライアントが全て切断されたらホストもリザルト移動
        if (disconnectionClientCount >= playerDatas.Count - 1)
        {
            NetworkManager.singleton.StopHost();    //ホストを停止
            MatchingManager.Singleton.Init();
            ResultButtonsController.SetRank(ranking);

            //リザルト画面に移動
            NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.RESULT);
        }
    }

    void PlayStartSE()
    {
        SoundManager.Play(SoundManager.SE.START_COUNT_DOWN_D, SoundManager.BaseSEVolume);
        Invoke(nameof(SetStartFlagTrue), 3.5f);
    }

    void SetStartFlagTrue()
    {
        startFlag = true;
    }


    IEnumerator FinishGame(string[] ranking)
    {
        yield return new WaitForSeconds(1.0f);
        RpcPlayFinishSE();
        yield return new WaitForSeconds(4.0f);
        RpcMoveResultScreen(ranking);
    }

    [ClientRpc]
    void RpcPlayFinishSE()
    {
        SoundManager.Play(SoundManager.SE.FINISH, SoundManager.BaseSEVolume);
    }

    [ClientRpc]
    void RpcMoveResultScreen(string[] ranking)
    {
        //サーバだけ実行しない
        if (isServer) return;

        NetworkManager.singleton.StopClient();  //クライアントを停止
        Mirror.Discovery.CustomNetworkDiscoveryHUD.Singleton.Init();
        ResultButtonsController.SetRank(ranking);

        //リザルト画面に移動
        NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.RESULT);
    }


    public static void AddPlayerData(BattleDrone drone)
    {
        playerDatas.Add(new PlayerData
        {
            drone = drone
        });
        PlayerData.droneNum++;
    }
}
