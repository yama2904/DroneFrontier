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
        public static int droneNum = MatchingManager.PlayerNum;  //残っているドローンの数
    }
    static List<PlayerData> playerDatas = new List<PlayerData>();
    static BattleDrone localDrone = null;
    int useIndex = 0;

    //ゲーム終了処理を行ったらtrue
    bool isFinished = false;


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!MainGameManager.IsItem)
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag(TagNameManager.ITEM);
            foreach (GameObject item in items)
            {
                Destroy(item);
            }
        }
    }

    void Awake()
    {
        //シングルトンの作成
        singleton = this;
    }

    void Start() { }

    void Update()
    {
        if (!MainGameManager.Singleton.StartFlag) return;

        //ゲームオーバーになったら他のプレイヤーのカメラにスペースキーで切り替える
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (localDrone.IsGameOver)
            {
                //次のプレイヤーのカメラに切り替える
                int initIndex = useIndex;
                playerDatas[useIndex].drone.SetCameraDepth(0);
                do
                {
                    useIndex++;
                    if (useIndex >= playerDatas.Count || useIndex < 0)
                    {
                        useIndex = 0;
                    }

                    //破壊されていたらスキップ
                    BattleDrone drone = playerDatas[useIndex].drone;
                    if (drone.IsGameOver)
                    {
                        useIndex++;
                    }
                    //自分のドローンだったらスキップ
                    else if(drone.netId == localDrone.netId)
                    {
                        useIndex++;
                    }
                    else
                    {
                        drone.SetCameraDepth(5);
                    }
                } while (useIndex != initIndex);
            }
        }

        if (isServer)
        {
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

    public static void AddPlayerData(BattleDrone drone, bool isLocalPlayer)
    {
        //既にリストにあったら処理しない
        if (playerDatas.FindIndex(pd => pd.drone.netId == drone.netId) >= 0) return;

        if (isLocalPlayer)
        {
            localDrone = drone;
        }

        playerDatas.Add(new PlayerData
        {
            drone = drone
        });
    }
}
