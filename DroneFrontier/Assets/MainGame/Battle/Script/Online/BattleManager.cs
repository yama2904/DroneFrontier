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
        static BattleManager singleton;
        public new static BattleManager Singleton { get { return singleton; } }

        //アイテムを出現させるか
        public static bool IsItemSpawn { get; set; } = true;

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

            listener = GetComponent<AudioListener>();
            listener.enabled = false;
            timeText.enabled = false;
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();

            //カウントダウンが終わったら処理
            if (!StartFlag) return;

            //ゲームオーバーになったら他のプレイヤーのカメラにスペースキーで切り替える
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (localDrone.IsGameOver)
                {
                    //次のプレイヤーのカメラとリスナーに切り替える
                    int initIndex = useIndex;
                    playerDatas[useIndex].drone.SetCameraDepth(0);
                    playerDatas[useIndex].drone.SetAudioListener(false);
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
                //最後のプレイヤーが残ったら終了処理
                if (PlayerData.droneNum <= 1)
                {
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
            if (ReferenceEquals(localDrone, pd.drone) ||
                (localDrone.IsGameOver && index == useIndex))
            {
                listener.enabled = true;
            }
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

            int rank = 1;
            foreach (PlayerData pd in playerDatas)
            {
                if (pd.isDestroy) continue;
                pd.ranking = rank;
                rank++;
            }

            FinishGame();
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