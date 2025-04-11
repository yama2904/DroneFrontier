using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Online
{
    public class BattleManager : NetworkBehaviour
    {
        //シングルトン
        public static BattleManager Singleton { get; private set; }

        //アイテムを出現させるか
        public static bool IsItemSpawn { get; set; } = true;

        //ストック
        const int MAX_STOCK = 1;
        [SerializeField] Image stockIcon = null;
        [SerializeField] Text stockText = null;

        //プレイヤー情報
        public class ServerPlayerData
        {
            public NetworkConnection conn;
            public GameObject drone = null;
            public int weapon = -1;
            public string name = "";
            public int stock = MAX_STOCK;
            public int ranking = 1;
            public float destroyTime = -1;
            public bool isDestroy = false;
            public static int droneNum = MatchingManager.PlayerNum;  //残っているドローンの数
        }
        static List<ServerPlayerData> serverPlayerDatas = new List<ServerPlayerData>();
        SyncList<GameObject> clientPlayers = new SyncList<GameObject>();
        bool initClientPlayers = false;
        bool isDestroy = false;
        int useIndex = 0;

        //ゲーム終了処理を行ったらtrue
        bool isFinished = false;

        //ゲーム開始時に生成
        [SerializeField] ItemSpawnManager itemSpawnManager = null;

        //残り時間
        [SerializeField] Text timeText = null;
        [SerializeField, Tooltip("制限時間(分)")] int maxTime = 5;
        Coroutine countCoroutine = null;

        //キャッシュ用
        AudioListener listener = null;


        public override void OnStartClient()
        {
            base.OnStartClient();

            //フィールド上のアイテム処理
            if (!IsItemSpawn)
            {
                GameObject[] items = GameObject.FindGameObjectsWithTag(TagNameConst.ITEM_SPAWN);
                foreach (GameObject item in items)
                {
                    Destroy(item);
                }
            }
            ServerPlayerData.droneNum = MatchingManager.PlayerNum;

            if (isServer)
            {
                //アイテムスポーン処理
                if (IsItemSpawn)
                {
                    GameObject manager = Instantiate(itemSpawnManager).gameObject;
                    NetworkServer.Spawn(manager, connectionToClient);
                }
                countCoroutine = StartCoroutine(CountTime());

                //3秒後にカウントダウンSE
                Invoke(nameof(CallRpcPlayStartCountDown), 3.0f);
            }

            stockText.text = MAX_STOCK.ToString();
            listener = GetComponent<AudioListener>();
            listener.enabled = false;
            timeText.enabled = false;
        }

        void Awake()
        {
            //シングルトンの作成
            Singleton = this;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                foreach (GameObject o in clientPlayers)
                {
                    Debug.Log(o);
                }
            }

            if (!initClientPlayers)
            {
                if (isServer)
                {
                    if (serverPlayerDatas.Count == MatchingManager.PlayerNum)
                    {
                        foreach (ServerPlayerData spd in serverPlayerDatas)
                        {
                            clientPlayers.Add(spd.drone);
                        }
                        initClientPlayers = true;
                    }
                }
            }

            //カウントダウンが終わったら処理
            if (!MainGameManager.Singleton.StartFlag) return;

            if (isDestroy)
            {
                //観戦中のプレイヤーが死亡したらカメラとリスナーを切り替える
                if (clientPlayers[useIndex] == null)
                {
                    SwitchingWatch();
                }

                //ゲームオーバーになったら他のプレイヤーのカメラにスペースキーで切り替える
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    //次のプレイヤーのカメラとリスナーに切り替える
                    listener.enabled = false;
                    SwitchingWatch();
                }
            }


            //リスポーン・ランキング処理
            if (isServer)
            {
                //破壊されたドローンを調べる
                for (int i = 0; i < serverPlayerDatas.Count; i++)
                {
                    if (serverPlayerDatas[i].isDestroy) continue;

                    ServerPlayerData destroyDrone = serverPlayerDatas[i];  //名前省略
                    if (clientPlayers[i] == null)
                    {
                        //ストックが残っていたら復活
                        if (destroyDrone.stock > 0)
                        {
                            Transform pos = NetworkManager.singleton.GetStartPosition();
                            GameObject p = Instantiate(NetworkManager.singleton.playerPrefab, pos.position, pos.rotation);
                            p.GetComponent<BattleDrone>().syncSetSubWeapon = destroyDrone.weapon;
                            NetworkServer.AddPlayerForConnection(destroyDrone.conn, p);
                            clientPlayers[i] = p;
                            destroyDrone.stock--;
                            destroyDrone.destroyTime = Time.time;

                            //リスポーンSEの再生
                            p.GetComponent<DroneSoundAction>().RpcPlayOneShotSEAllClient(SoundManager.SE.Respawn, SoundManager.MasterSEVolume);

                            //残機UIの変更
                            TargetSetStockText(destroyDrone.conn, destroyDrone.stock.ToString());
                        }
                        //ストックが残っていなかったらランキングに記録
                        else
                        {
                            TargetSetIsDestroy(destroyDrone.conn, true);
                            destroyDrone.isDestroy = true;

                            destroyDrone.ranking = ServerPlayerData.droneNum;
                            ServerPlayerData.droneNum--;

                            //残機UIの非表示
                            TargetSetStockEnabled(destroyDrone.conn, false);
                        }
                    }
                }

                //最後のプレイヤーが残ったら終了処理
                if (ServerPlayerData.droneNum <= 1)
                {
                    foreach (ServerPlayerData pd in serverPlayerDatas)
                    {
                        if (pd.isDestroy) continue;
                        pd.ranking = ServerPlayerData.droneNum;
                        break;
                    }
                    FinishGame();
                }
            }
        }

        //変数の初期化
        void OnDestroy()
        {
            serverPlayerDatas.Clear();
            ServerPlayerData.droneNum = 0;
        }


        //プレイヤーの情報を登録する
        [Server]
        public static void AddServerPlayerData(BattleDrone drone, NetworkConnection conn)
        {
            //既に登録済みのクライアントなら処理しない
            if (serverPlayerDatas.FindIndex(spd => ReferenceEquals(spd.conn, conn)) >= 0) return;

            serverPlayerDatas.Add(new ServerPlayerData
            {
                conn = conn,
                drone = drone.gameObject,
                weapon = drone.syncSetSubWeapon,
                name = drone.name,
            });

            Debug.Log("AddServerPlayerData");
        }


        //切断されたプレイヤーの処理
        [Server]
        public void DisconnectPlayer(NetworkConnection conn)
        {
            int index = serverPlayerDatas.FindIndex(pd => ReferenceEquals(pd.conn, conn));
            if (index < 0) return;  //リストにない場合は処理しない

            //ランキングを修正
            int rank = serverPlayerDatas[index].ranking;
            foreach (ServerPlayerData pd in serverPlayerDatas)
            {
                if (pd.ranking >= rank)
                {
                    pd.ranking--;
                }
            }

            //残りドローン数の修正
            if (!serverPlayerDatas[index].isDestroy)
            {
                ServerPlayerData.droneNum--;
            }

            //切断されたプレイヤーをリストから削除
            serverPlayerDatas.RemoveAt(index);
        }


        [Server]
        void CallRpcPlayStartCountDown()
        {
            MainGameManager.Singleton.RpcPlayStartCountDown();
        }

        [TargetRpc]
        void TargetSetIsDestroy(NetworkConnection target, bool isDestroy)
        {
            this.isDestroy = isDestroy;

            //観戦モードに切り替え
            useIndex = 0;
            SwitchingWatch();
        }

        //観戦プレイヤー切り替え
        void SwitchingWatch()
        {
            //観戦中のプレイヤーのカメラをリスナーをオフ
            if (clientPlayers[useIndex] != null)
            {
                //名前省略
                BattleDrone drone = clientPlayers[useIndex].GetComponent<BattleDrone>();

                drone.SetCameraDepth(0);
                drone.SetAudioListener(false);
            }

            int initIndex = useIndex;
            while (true)
            {
                useIndex++;

                //全てのプレイヤーが死亡している場合はBattleManagerに付けているリスナーをオンにする
                if (useIndex == initIndex)
                {
                    useIndex = 0;
                    listener.enabled = true;
                    return;
                }

                //配列の範囲外なら修正
                if (useIndex >= clientPlayers.Count || useIndex < 0)
                {
                    useIndex = 0;
                }

                //破壊されていたらスキップ
                Debug.Log("useIndex: " + useIndex);
                if (clientPlayers[useIndex] == null)
                {
                    continue;
                }
                else
                {
                    //名前省略
                    BattleDrone bd = clientPlayers[useIndex].GetComponent<BattleDrone>();

                    bd.SetCameraDepth(5);
                    bd.SetAudioListener(true);
                    return;
                }
            }
        }

        //時間制限処理
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
                RpcSetTimeText("残 り " + i + " 分");

                yield return new WaitForSeconds(4f);
                RpcSetTextEnabled(false);
                yield return new WaitForSeconds(56f);
            }

            RpcSetTextEnabled(true);
            RpcSetTimeText("残 り " + 1 + " 分");
            yield return new WaitForSeconds(1f);

            for (int i = 59; i >= 0; i--)
            {
                RpcSetTimeText(i + " 秒");
                yield return new WaitForSeconds(1f);
            }

            //残りストック数と死亡した時間に応じてランキング設定
            int stock = 0;
            while (ServerPlayerData.droneNum > 0)
            {
                List<int> destroyTimeIndex = new List<int>();
                for (int i = 0; i < serverPlayerDatas.Count; i++)
                {
                    if (serverPlayerDatas[i].isDestroy) continue;
                    if (serverPlayerDatas[i].stock == stock)
                    {
                        destroyTimeIndex.Add(i);
                    }
                }

                //死亡した時間に応じてランキング順位変動
                while(destroyTimeIndex.Count > 0)
                {
                    int minIndex = 0;
                    for(int i = 0; i < destroyTimeIndex.Count; i++)
                    {
                        //死亡した時間が早いほどランキングが低い
                        if(serverPlayerDatas[destroyTimeIndex[i]].destroyTime < serverPlayerDatas[destroyTimeIndex[minIndex]].destroyTime)
                        {
                            minIndex = i;
                        }
                    }

                    //順位決定
                    serverPlayerDatas[destroyTimeIndex[minIndex]].ranking = ServerPlayerData.droneNum;
                    serverPlayerDatas[destroyTimeIndex[minIndex]].isDestroy = true;
                    ServerPlayerData.droneNum--;

                    //使用したインデックスは削除
                    destroyTimeIndex.RemoveAt(minIndex);
                }

                stock++;
            }

            FinishGame();
        }

        //ゲームの終了処理
        [Server]
        void FinishGame()
        {
            if (!isFinished)
            {
                string[] ranking = new string[serverPlayerDatas.Count];
                for (int i = 0; i < serverPlayerDatas.Count; i++)
                {
                    ServerPlayerData spd = serverPlayerDatas[i];  //名前省略
                    ranking[spd.ranking - 1] = spd.name;
                }
                MainGameManager.Singleton.FinishGame(ranking);
                isFinished = true;

                RpcSetTextEnabled(false);
                StopCoroutine(countCoroutine);
            }
        }


        //残機のテキスト変更
        [TargetRpc]
        void TargetSetStockText(NetworkConnection target, string text)
        {
            stockText.text = text;
        }

        //残機UI表示変更
        [TargetRpc]
        void TargetSetStockEnabled(NetworkConnection target, bool flag)
        {
            stockIcon.enabled = flag;
            stockText.enabled = flag;
        }


        //残り時間のテキスト変更
        [ClientRpc]
        void RpcSetTimeText(string text)
        {
            timeText.text = text;
        }

        //残り時間のテキスト表示
        [ClientRpc]
        void RpcSetTextEnabled(bool flag)
        {
            timeText.enabled = flag;
        }
    }
}