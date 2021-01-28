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
        public RaceDrone drone = null;
        public int ranking = MatchingManager.PlayerNum;
        public bool isGoal = false;
        public static int droneNum = MatchingManager.PlayerNum;
    }
    static List<PlayerData> playerDatas = new List<PlayerData>();

    //ゲーム終了処理を行ったらtrue
    bool isFinished = false;


    void Awake()
    {
        //シングルトンの作成
        singleton = this;
    }

    void Start()
    {

    }

    void Update()
    {
        if (!MainGameManager.Singleton.StartFlag) return;

        //ゴールしたドローンを走査
        if (isServer)
        {
            foreach (PlayerData pd in playerDatas)
            {
                if (pd.isGoal) continue;
                if (pd.drone.IsGoal)
                {
                    pd.ranking = MatchingManager.PlayerNum - (PlayerData.droneNum - 1);
                    pd.isGoal = true;
                    PlayerData.droneNum--;
                }
            }

            //ゴールしていないプレイヤーが1人になったら終了処理
            if (PlayerData.droneNum <= 1)
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

    public static void AddPlayerData(RaceDrone drone)
    {
        //既にリストにあったら処理しない
        if (playerDatas.FindIndex(pd => pd.drone.netId == drone.netId) >= 0) return;

        playerDatas.Add(new PlayerData
        {
            drone = drone
        });
    }
}
