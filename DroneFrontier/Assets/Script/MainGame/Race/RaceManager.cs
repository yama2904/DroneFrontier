﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class RaceManager : MainGameManager
    {
        public new static RaceManager Singleton { get; private set; }

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


        protected override void Awake()
        {
            base.Awake();
            Singleton = this;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            PlayerData.goalNum = 0;

            //3秒後にカウントダウンSE
            Invoke(nameof(RpcPlayStartCountDown), 3.0f);
        }

        protected override void Update()
        {
            base.Update();

            //カウントダウンが終わってから処理
            if (!StartFlag) return;

            //ゴールしたドローンを走査
            if (isServer)
            {
                //ゴールしていないプレイヤーが1人になったら終了処理
                if (PlayerData.goalNum >= MatchingManager.PlayerNum - 1)
                {
                    if (!isFinished)
                    {
                        foreach (PlayerData pd in playerDatas)
                        {
                            ranking[pd.ranking - 1] = pd.drone.name;
                        }
                        FinishGame();
                        isFinished = true;
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

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
        [Server]
        public static void DisconnectPlayer(NetworkConnection conn)
        {
            int index = playerDatas.FindIndex(pd => ReferenceEquals(pd.conn, conn));
            if (index < 0) return;

            //ランキングを修正
            int rank = playerDatas[index].ranking;
            foreach (PlayerData pd in playerDatas)
            {
                if (pd.ranking >= rank)
                {
                    pd.ranking--;
                }
            }

            //残りプレイヤーの修正
            if (playerDatas[index].isGoal)
            {
                PlayerData.goalNum--;
            }

            //切断されたプレイヤーをリストから削除
            playerDatas.RemoveAt(index);
        }
    }
}