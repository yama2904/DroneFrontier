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

    //アイテム
    [SerializeField, Tooltip("スポーンするアイテム")] Item spawnItem = null;
    [SerializeField, Tooltip("フィールド上に出現させるアイテムの上限")] int itemLimitNum = 10;
    [SerializeField, Tooltip("アイテムが出現する間隔")] float itemSpawnInterval = 10f;


    public override void OnStartClient()
    {
        base.OnStartClient();

        //フィールド上のアイテム処理
        GameObject[] items = GameObject.FindGameObjectsWithTag(TagNameManager.ITEM_SPAWN);
        if (!MainGameManager.IsItem)
        {
            foreach (GameObject item in items)
            {
                Destroy(item);
            }
        }
        else
        {
            if (isServer)
            {
                //アイテムのランダムスポーン
                int itemCount = 0;
                int index = 0;
                bool[] useIndex = new bool[items.Length];
                float quitCount = 0;  //無限ループ防止用
                while (itemCount < itemLimitNum)
                {
                    quitCount++;
                    if (quitCount > 1000)
                    {
                        Application.Quit();
                        break;
                    }

                    //フィールド上の全てのアイテムをスポーンしていたら終了
                    if (itemCount >= items.Length) break;

                    //既にスポーン済みならスキップ
                    if (useIndex[index]) continue;


                    //ランダムでスポーン
                    if (Random.Range(0, 2) == 0)
                    {
                        Item item = Instantiate(spawnItem, items[index].transform);
                        item.InitItemType();
                        NetworkServer.Spawn(item.gameObject, connectionToClient);

                        itemCount++;  //カウントの更新
                        useIndex[index] = true; //使用した配列要素をメモ
                    }

                    index++;
                    if (index >= items.Length)   //配列の末尾に到達したら0に戻す
                    {
                        index = 0;
                    }
                }
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
                    PlayerData pd = playerDatas[useIndex];
                    if (pd.isDestroy)
                    {
                        useIndex++;
                    }
                    else
                    {
                        pd.drone.SetCameraDepth(5);
                    }
                } while (useIndex != initIndex);
            }
        }

        if (isServer)
        {
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

    public void SetDestroyedDrone(uint netId)
    {
        int index = playerDatas.FindIndex(playerData => playerData.drone.netId == netId);
        if (index == -1) return;  //対応するドローンがなかったら処理しない

        PlayerData pd = playerDatas[index];  //名前省略
        if (pd.isDestroy) return;  //既に死亡処理を行っていたら処理しない

        //リスト情報の変更
        pd.ranking = PlayerData.droneNum;   //ランキングの記録
        pd.isDestroy = true;
        PlayerData.droneNum--;  //残りドローンを減らす

        //カメラ切り替え
        pd.drone.SetCameraDepth(-1);
    }
}
