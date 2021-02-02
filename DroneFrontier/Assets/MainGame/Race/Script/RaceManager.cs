using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RaceManager : NetworkBehaviour
{
    //シングルトン
    static RaceManager singleton;
    public static RaceManager Singleton { get { return singleton; } }

    class PlayerData
    {
        public NetworkConnection conn;
        public RaceDrone drone = null;
        public int ranking = MatchingManager.PlayerNum;
        public bool isGoal = false;
        public static int goalNum = 0;
    }
    static List<PlayerData> playerDatas = new List<PlayerData>();

    //ゲーム終了処理を行ったらtrue
    bool isFinished = false;


    void Awake()
    {
        //シングルトンの作成
        singleton = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        PlayerData.goalNum = 0;
    }

    void Update()
    {
        if (!MainGameManager.Singleton.StartFlag) return;

        //ゴールしたドローンを走査
        if (isServer)
        {
            //ゴールしていないプレイヤーが1人になったら終了処理
            if (PlayerData.goalNum >= MatchingManager.PlayerNum - 1)
            {
                if (!isFinished)
                {
                    string[] ranking = new string[playerDatas.Count];
                    foreach (PlayerData pd in playerDatas)
                    {
                        ranking[pd.ranking - 1] = pd.drone.name;
                    }
                    MainGameManager.Singleton.FinishGame(ranking);
                    isFinished = true;
                }
            }
        }
    }

    private void OnDestroy()
    {
        playerDatas.Clear();
        PlayerData.goalNum = 0;
    }

    //プレイヤー情報の登録
    public static void AddPlayerData(RaceDrone drone, NetworkConnection conn)
    {
        //既にリストにあったら処理しない
        if (playerDatas.FindIndex(pd => pd.drone.netId == drone.netId) >= 0) return;

        playerDatas.Add(new PlayerData
        {
            conn = conn,
            drone = drone
        });
    }

    //ゴールしたプレイヤーを登録
    public void SetGoalDrone(uint netId)
    {
        int index = playerDatas.FindIndex(playerData => playerData.drone.netId == netId);
        if (index == -1) return;  //対応するドローンがなかったら処理しない

        PlayerData pd = playerDatas[index];  //名前省略
        if (pd.isGoal) return;  //既にゴール処理を行っていたら処理しない

        //リスト情報の変更
        pd.ranking = PlayerData.goalNum + 1;
        pd.isGoal = true;
        PlayerData.goalNum++;
    }


    //切断されたプレイヤーの処理
    public static void DisconnectPlayer(NetworkConnection conn)
    {
        int index = playerDatas.FindIndex(pd => ReferenceEquals(pd.conn, conn));
        if (index < 0) return;

        //ランキングを修正
        int rank = playerDatas[index].ranking;
        foreach (PlayerData pd in playerDatas)
        {
            if (pd.ranking > rank)
            {
                pd.ranking--;
            }
        }

        //残りプレイヤーの修正
        if (!playerDatas[index].isGoal)
        {
            PlayerData.goalNum--;
        }

        //切断されたプレイヤーをリストから削除
        playerDatas.RemoveAt(index);
    }
}
