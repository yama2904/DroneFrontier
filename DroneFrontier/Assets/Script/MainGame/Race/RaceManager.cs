using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class RaceManager : NetworkBehaviour
    {
        public new static RaceManager Singleton { get; private set; }

        class ServerPlayerData
        {
            public NetworkConnection conn;
            public RaceDrone drone = null;
            //public int ranking = MatchingManager.PlayerNum;
            public bool isGoal = false;
            public static int goalNum = 0;
        }
        static List<ServerPlayerData> serverPlayerDatas = new List<ServerPlayerData>();

        //ゲーム終了処理を行ったらtrue
        bool isFinished = false;


        void Awake()
        {
            Singleton = this;
        }

        [ServerCallback]
        public override void OnStartClient()
        {
            base.OnStartClient();
            ServerPlayerData.goalNum = 0;

            //3秒後にカウントダウンSE
            Invoke(nameof(CallRpcPlayStartCountDown), 3.0f);
        }

        void Update()
        {
            //カウントダウンが終わってから処理
            //if (!MainGameManager.Singleton.StartFlag) return;

            //ゴールしたドローンを走査
            if (isServer)
            {
                //ゴールしていないプレイヤーが1人になったら終了処理
                //if (ServerPlayerData.goalNum >= MatchingManager.PlayerNum - 1)
                //{
                //    if (!isFinished)
                //    {
                //        string[] ranking = new string[serverPlayerDatas.Count];
                //        foreach (ServerPlayerData pd in serverPlayerDatas)
                //        {
                //            ranking[pd.ranking - 1] = pd.drone.name;
                //        }
                //        MainGameManager.Singleton.FinishGame(ranking);
                //        isFinished = true;
                //    }
                //}
            }
        }

        void OnDestroy()
        {
            serverPlayerDatas.Clear();
            ServerPlayerData.goalNum = 0;
        }

        //プレイヤー情報の登録
        public static void AddPlayerData(RaceDrone drone, NetworkConnection conn)
        {
            //既にリストにあったら処理しない
            //if (serverPlayerDatas.FindIndex(pd => pd.drone.netId == drone.netId) >= 0) return;

            serverPlayerDatas.Add(new ServerPlayerData
            {
                conn = conn,
                drone = drone
            });
        }

        //ゴールしたプレイヤーを登録
        public void SetGoalDrone(uint netId)
        {
            //int index = serverPlayerDatas.FindIndex(playerData => playerData.drone.netId == netId);
            //if (index == -1) return;  //対応するドローンがなかったら処理しない

            //ServerPlayerData pd = serverPlayerDatas[index];  //名前省略
            //if (pd.isGoal) return;  //既にゴール処理を行っていたら処理しない

            //リスト情報の変更
            //pd.ranking = ServerPlayerData.goalNum + 1;
            //pd.isGoal = true;
            //ServerPlayerData.goalNum++;
        }


        //切断されたプレイヤーの処理
        [Server]
        public static void DisconnectPlayer(NetworkConnection conn)
        {
            int index = serverPlayerDatas.FindIndex(pd => ReferenceEquals(pd.conn, conn));
            if (index < 0) return;

            //ランキングを修正
            //int rank = serverPlayerDatas[index].ranking;
            foreach (ServerPlayerData pd in serverPlayerDatas)
            {
                //if (pd.ranking >= rank)
                //{
                //    pd.ranking--;
                //}
            }

            //残りプレイヤーの修正
            if (serverPlayerDatas[index].isGoal)
            {
                ServerPlayerData.goalNum--;
            }

            //切断されたプレイヤーをリストから削除
            serverPlayerDatas.RemoveAt(index);
        }


        [Server]
        void CallRpcPlayStartCountDown()
        {
            //MainGameManager.Singleton.RpcPlayStartCountDown();
        }
    }
}