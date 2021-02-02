using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class BattleManager : NetworkBehaviour
{
    //シングルトン
    static BattleManager singleton;
    public static BattleManager Singleton { get { return singleton; } }

    //プレイヤー情報
    public class PlayerData
    {
        public NetworkConnection conn;
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

    //ゲーム開始時に生成
    [SerializeField] ItemSpawnManager itemSpawnManager = null;

    //残り時間
    [SerializeField] Text timeText = null;
    [SerializeField, Tooltip("制限時間(分)")] int maxTime = 5;
    Coroutine countCoroutine = null;


    public override void OnStartClient()
    {
        base.OnStartClient();

        //フィールド上のアイテム処理
        if (!MainGameManager.IsItemSpawn)
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag(TagNameManager.ITEM_SPAWN);
            foreach (GameObject item in items)
            {
                Destroy(item);
            }
        }
        PlayerData.droneNum = MatchingManager.PlayerNum;

        if (isServer)
        {
            if (MainGameManager.IsItemSpawn)
            {
                GameObject manager = Instantiate(itemSpawnManager).gameObject;
                NetworkServer.Spawn(manager, connectionToClient);
            }
            countCoroutine = StartCoroutine(CountTime());
        }

        timeText.enabled = false;
    }

    void Awake()
    {
        //シングルトンの作成
        singleton = this;
    }

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
                playerDatas[useIndex].drone.SetAudioListener(false);
                do
                {
                    useIndex++;
                    if (useIndex >= playerDatas.Count || useIndex < 0)
                    {
                        useIndex = 0;
                    }

                    //破壊されていたらスキップ
                    PlayerData pd = playerDatas[useIndex];
                    if (pd.isDestroy || pd.drone == null)
                    {
                        useIndex++;
                    }
                    else
                    {
                        pd.drone.SetCameraDepth(5);
                        pd.drone.SetAudioListener(true);
                        break;
                    }
                } while (useIndex != initIndex);
            }
        }

        if (isServer)
        {
            //最後のプレイヤーが残ったら終了処理
            if (PlayerData.droneNum <= 1)
            {
                FinishGame();
            }
        }
    }

    IEnumerator CountTime()
    {
        //スタートフラグが立つまで停止
        while (!MainGameManager.Singleton.StartFlag) yield return null;

        if (maxTime > 1)
        {
            yield return new WaitForSeconds(60f);
        }

        for (int i = maxTime - 1; i > 1; i--)
        {
            RpcSetTextEnabled(true);
            RpcSetText("残 り " + i + " 分");

            yield return new WaitForSeconds(4f);
            RpcSetTextEnabled(false);
            yield return new WaitForSeconds(56f);
        }

        RpcSetTextEnabled(true);
        RpcSetText("残 り " + 1 + " 分");
        yield return new WaitForSeconds(1f);

        for (int i = 59; i >= 0; i--)
        {
            RpcSetText(i + " 秒");
            yield return new WaitForSeconds(1f);
        }

        int rank = 1;
        foreach (PlayerData pd in playerDatas)
        {
            if (pd.isDestroy) continue;
            pd.ranking = rank;
            rank++;
        }

        FinishGame();
    }

    [ClientRpc]
    void RpcSetText(string text)
    {
        timeText.text = text;
    }

    [ClientRpc]
    void RpcSetTextEnabled(bool flag)
    {
        timeText.enabled = flag;
    }

    //ゲームの終了処理
    [Server]
    void FinishGame()
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

            StopCoroutine(countCoroutine);
        }
    }

    public static void Init()
    {
        playerDatas.Clear();
        PlayerData.droneNum = 0;
        localDrone = null;
    }

    //プレイヤーの情報を登録する
    public static void AddPlayerData(BattleDrone drone, bool isLocalPlayer, NetworkConnection conn)
    {
        //既にリストにあったら処理しない
        if (playerDatas.FindIndex(pd => pd.drone.netId == drone.netId) >= 0) return;

        if (isLocalPlayer)
        {
            localDrone = drone;
        }

        playerDatas.Add(new PlayerData
        {
            conn = conn,
            drone = drone
        });
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

        //残りドローン数の修正
        if (!playerDatas[index].isDestroy)
        {
            PlayerData.droneNum--;
        }

        //切断されたプレイヤーをリストから削除
        playerDatas.RemoveAt(index);
    }

    //ゲームオーバーになったプレイヤーを登録
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
