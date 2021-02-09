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
        public class PlayerData
        {
            public NetworkConnection conn;
            public BattleDrone drone = null;
            public int weapon = -1;
            public string name = "";
            public int stock = 0;
            public int ranking = 1;
            public bool isDestroy = false;
            public static int droneNum = MatchingManager.PlayerNum;  //残っているドローンの数
        }
        List<PlayerData> playerDatas = new List<PlayerData>();
        int isLocalIndex = -1;
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
            PlayerData.droneNum = MatchingManager.PlayerNum;

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

            //カウントダウンが終わったら処理
            if (!StartFlag) return;

            //ゲームオーバーになったら他のプレイヤーのカメラにスペースキーで切り替える
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isLocalIndex == -1) return;
                if (playerDatas[isLocalIndex].isDestroy)
                {
                    //次のプレイヤーのカメラとリスナーに切り替える
                    int initIndex = useIndex;
                    if (playerDatas[useIndex].drone != null)
                    {
                        playerDatas[useIndex].drone.SetCameraDepth(0);
                        playerDatas[useIndex].drone.SetAudioListener(false);
                    }
                    listener.enabled = false;
                    while (true)
                    {
                        useIndex++;

                        //無限ループ防止
                        if (useIndex == initIndex)
                        {
                            break;
                        }

                        //配列の範囲外なら修正
                        if (useIndex >= playerDatas.Count || useIndex < 0)
                        {
                            useIndex = 0;
                        }

                        //破壊されていたらスキップ
                        PlayerData pd = playerDatas[useIndex];
                        Debug.Log("useIndex: " + useIndex);
                        if (pd.isDestroy || pd.drone == null)
                        {
                            continue;
                        }
                        else
                        {
                            pd.drone.SetCameraDepth(5);
                            pd.drone.SetAudioListener(true);
                            break;
                        }
                    }
                }
            }

            if (isServer)
            {
                //破壊されたドローンを調べる
                foreach (PlayerData pd in playerDatas)
                {
                    if (pd.isDestroy) continue;
                    if (pd.drone == null)
                    {
                        //ストックが残っていたら復活
                        if (pd.stock > 0)
                        {
                            Transform pos = NetworkManager.singleton.GetStartPosition();
                            GameObject p = Instantiate(NewNetworkRoomManager.singleton.playerPrefab, pos.position, pos.rotation);
                            p.GetComponent<BattleDrone>().syncSetSubWeapon = pd.weapon;
                            NetworkServer.AddPlayerForConnection(pd.conn, p);
                            pd.stock--;
                            pd.drone = p.GetComponent<BattleDrone>();

                            //リスポーンSEの再生
                            pd.drone.GetComponent<DroneSoundAction>().RpcPlayOneShotSEAllClient(SoundManager.SE.RESPAWN, SoundManager.BaseSEVolume);

                            //残機UIの変更
                            TargetSetStockText(pd.conn, pd.stock.ToString());
                        }
                        //ストックが残っていなかったらランキングに記録
                        else
                        {
                            pd.isDestroy = true;
                            ranking[PlayerData.droneNum - 1] = pd.name;
                            PlayerData.droneNum--;

                            //残機UIの非表示
                            TargetSetStockEnabled(pd.conn, false);
                        }
                    }
                }

                //最後のプレイヤーが残ったら終了処理
                if (PlayerData.droneNum <= 1)
                {
                    foreach (PlayerData pd in playerDatas)
                    {
                        if (pd.isDestroy) continue;
                        ranking[PlayerData.droneNum - 1] = pd.name;
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

            playerDatas.Clear();
            PlayerData.droneNum = 0;
            isLocalIndex = -1;
        }


        //プレイヤーの情報を登録する
        public void AddPlayerData(BattleDrone drone, bool isLocalPlayer, NetworkConnection conn)
        {
            //既にリストにあったら処理しない
            if (playerDatas.FindIndex(pd => ReferenceEquals(pd.conn, conn)) >= 0) return;

            playerDatas.Add(new PlayerData
            {
                conn = conn,
                drone = drone,
                weapon = drone.syncSetSubWeapon,
                name = drone.name,
                stock = droneStock
            });

            if (isLocalPlayer)
            {
                isLocalIndex = playerDatas.Count - 1;
            }
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

            //オーディオリスナーを担当していたドローンなら一時的に自分のリスナーをオンにする
            if (ReferenceEquals(playerDatas[isLocalIndex].drone, pd.drone) ||
                (playerDatas[isLocalIndex].isDestroy && index == useIndex))
            {
                listener.enabled = true;
            }
        }


        //切断されたプレイヤーの処理
        public void DisconnectPlayer(NetworkConnection conn)
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

            foreach (PlayerData pd in playerDatas)
            {
                if (pd.isDestroy) continue;
                ranking[PlayerData.droneNum - 1] = pd.name;
                PlayerData.droneNum--;
            }

            FinishGame();
        }

        //ゲームの終了処理
        [Server]
        void FinishGame()
        {
            if (!isFinished)
            {
                FinishGame(ranking);
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