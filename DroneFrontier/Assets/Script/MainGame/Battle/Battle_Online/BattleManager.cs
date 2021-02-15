using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Online
{
    public class BattleManager : MainGameManager
    {
        //シングルトン
        public new static BattleManager Singleton { get; private set; }

        //アイテムを出現させるか
        public static bool IsItemSpawn { get; set; } = true;

        //ストック
        [SerializeField] int droneStock = 1;
        [SerializeField] Image stockIcon = null;
        [SerializeField] Text stockText = null;

        //プレイヤー情報
        public class ServerPlayerData
        {
            public NetworkConnection conn;
            public GameObject drone = null;
            public int weapon = -1;
            public string name = "";
            public int stock = 0;
            public int ranking = 1;
            public bool isDestroy = false;
            public static int droneNum = MatchingManager.PlayerNum;  //残っているドローンの数
        }
        List<ServerPlayerData> serverPlayerDatas = new List<ServerPlayerData>();
        List<BattleDrone> clientPlayers = new List<BattleDrone>();
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
                GameObject[] items = GameObject.FindGameObjectsWithTag(TagNameManager.ITEM_SPAWN);
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
                Invoke(nameof(RpcPlayStartCountDown), 3.0f);
            }

            stockText.text = droneStock.ToString();
            listener = GetComponent<AudioListener>();
            listener.enabled = false;
            timeText.enabled = false;
        }

        protected override void Awake()
        {
            base.Awake();

            //シングルトンの作成
            Singleton = this;
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.T))
            {
                foreach (BattleDrone o in clientPlayers)
                {
                    Debug.Log(o);
                }
            }

            //カウントダウンが終わったら処理
            if (!StartFlag) return;

            //ゲームオーバーになったら他のプレイヤーのカメラにスペースキーで切り替える
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isDestroy)
                {
                    //次のプレイヤーのカメラとリスナーに切り替える
                    int initIndex = useIndex;
                    if (clientPlayers[useIndex] != null)
                    {
                        clientPlayers[useIndex].SetCameraDepth(0);
                        clientPlayers[useIndex].SetAudioListener(false);
                    }
                    listener.enabled = false;
                    while (true)
                    {
                        useIndex++;

                        //無限ループ防止
                        if (useIndex == initIndex) break;

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
                            clientPlayers[useIndex].SetCameraDepth(5);
                            clientPlayers[useIndex].SetAudioListener(true);
                            break;
                        }
                    }
                }
            }

            if (isServer)
            {
                //破壊されたドローンを調べる
                for (int i = 0; i < serverPlayerDatas.Count; i++)
                {
                    ServerPlayerData pd = serverPlayerDatas[i];
                    if (pd.isDestroy) continue;
                    if (pd.drone == null)
                    {
                        //ストックが残っていたら復活
                        if (pd.stock > 0)
                        {
                            Transform pos = NetworkManager.singleton.GetStartPosition();
                            GameObject p = Instantiate(NetworkManager.singleton.playerPrefab, pos.position, pos.rotation);
                            p.GetComponent<BattleDrone>().syncSetSubWeapon = pd.weapon;
                            NetworkServer.AddPlayerForConnection(pd.conn, p);
                            pd.drone = p;
                            pd.stock--;

                            //リスポーンSEの再生
                            p.GetComponent<DroneSoundAction>().RpcPlayOneShotSEAllClient(SoundManager.SE.RESPAWN, SoundManager.BaseSEVolume);

                            //残機UIの変更
                            TargetSetStockText(pd.conn, pd.stock.ToString());
                        }
                        //ストックが残っていなかったらランキングに記録
                        else
                        {
                            TargetSetIsDestroy(pd.conn, true);
                            pd.isDestroy = true;

                            pd.ranking = ServerPlayerData.droneNum;
                            ServerPlayerData.droneNum--;

                            foreach(ServerPlayerData spd in serverPlayerDatas)
                            {
                                //TargetAddClientPlayers(pd.conn, pd.drone);
                            }

                            //残機UIの非表示
                            TargetSetStockEnabled(pd.conn, false);
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
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServerPlayerData.droneNum = 0;
        }


        //プレイヤーの情報を登録する
        [Server]
        public void AddServerPlayerData(BattleDrone drone, NetworkConnection conn)
        {
            //既に登録済みのクライアントなら処理しない
            if (serverPlayerDatas.FindIndex(spd => ReferenceEquals(spd.conn, conn)) >= 0) return;

            serverPlayerDatas.Add(new ServerPlayerData
            {
                conn = conn,
                drone = drone.gameObject,
                weapon = drone.syncSetSubWeapon,
                name = drone.name,
                stock = droneStock
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


        [TargetRpc]
        void TargetSetIsDestroy(NetworkConnection target, bool isDestroy)
        {
            this.isDestroy = isDestroy;
            listener.enabled = true;
        }

        //時間制限処理
        IEnumerator CountTime()
        {
            //スタートフラグが立つまで停止
            while (!StartFlag) yield return null;

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

            foreach (ServerPlayerData pd in serverPlayerDatas)
            {
                if (pd.isDestroy) continue;
                pd.ranking = ServerPlayerData.droneNum;
                ServerPlayerData.droneNum--;
            }

            FinishGame();
        }

        //ゲームの終了処理
        [Server]
        new void FinishGame()
        {
            if (!isFinished)
            {
                for (int i = 0; i < serverPlayerDatas.Count; i++)
                {
                    ServerPlayerData spd = serverPlayerDatas[i];  //名前省略
                    ranking[spd.ranking - 1] = spd.name;
                }
                base.FinishGame();
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