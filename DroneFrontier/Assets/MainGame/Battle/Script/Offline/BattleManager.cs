using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
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
            public BattleDrone drone = null;
            public int ranking = 1;
            public bool isDestroy = false;
            public static int droneNum = 0;  //残っているドローンの数
        }
        static List<PlayerData> playerDatas = new List<PlayerData>();
        static BattleDrone localDrone = null;
        int useIndex = 0;

        //ゲーム終了処理を行ったらtrue
        bool isFinished = false;

        //ゲーム開始時に生成
        //[SerializeField] ItemSpawnManager itemSpawnManager = null;

        //残り時間
        [SerializeField] Text timeText = null;
        [SerializeField, Tooltip("制限時間(分)")] int maxTime = 5;
        Coroutine countCoroutine = null;

        //キャッシュ用
        AudioListener listener = null;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            PlayerData.droneNum = MainGameManager.playerNum;

            //フィールド上のアイテム処理
            if (!IsItemSpawn)
            {
                GameObject[] items = GameObject.FindGameObjectsWithTag(TagNameManager.ITEM_SPAWN);
                foreach (GameObject item in items)
                {
                    Destroy(item);
                }
            }

            //アイテムスポーン処理
            if (IsItemSpawn)
            {
                //itemSpawnManager = Instantiate(itemSpawnManager).gameObject;
            }
            countCoroutine = StartCoroutine(CountTime());

            //3秒後にカウントダウンSE
            Invoke(nameof(PlayStartCountDown), 3.0f);


            listener = GetComponent<AudioListener>();
            listener.enabled = false;
            timeText.enabled = false;
        }

        protected override void Update()
        {
            base.Update();

            //カウントダウンが終わったら処理
            if (!StartFlag) return;

            ////ゲームオーバーになったら他のプレイヤーのカメラにスペースキーで切り替える
            //if (Input.GetKeyDown(KeyCode.Space))
            //{
            //    if (localDrone.IsGameOver)
            //    {
            //        //次のプレイヤーのカメラとリスナーに切り替える
            //        int initIndex = useIndex;
            //        playerDatas[useIndex].drone.SetCameraDepth(0);
            //        playerDatas[useIndex].drone.SetAudioListener(false);
            //        listener.enabled = false;
            //        while (true)
            //        {
            //            useIndex++;

            //            //無限ループ防止
            //            if (useIndex == initIndex)
            //            {
            //                break;
            //            }

            //            //配列の範囲外なら修正
            //            if (useIndex >= playerDatas.Count || useIndex < 0)
            //            {
            //                useIndex = 0;
            //            }

            //            //破壊されていたらスキップ
            //            PlayerData pd = playerDatas[useIndex];
            //            Debug.Log("useIndex: " + useIndex);
            //            if (pd.isDestroy || pd.drone == null)
            //            {
            //                continue;
            //            }
            //            else
            //            {
            //                pd.drone.SetCameraDepth(5);
            //                pd.drone.SetAudioListener(true);
            //                break;
            //            }
            //        }
            //    }
            //}

            //最後のプレイヤーが残ったら終了処理
            if (PlayerData.droneNum <= 1)
            {
                if (isSolo) return;
                FinishGame();
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
        public static void AddPlayerData(BattleDrone drone, bool isLocalPlayer)
        {
            //既にリストにあったら処理しない
            if (playerDatas.FindIndex(pd => ReferenceEquals(pd.drone, drone)) >= 0) return;

            if (isLocalPlayer)
            {
                localDrone = drone;
            }

            playerDatas.Add(new PlayerData
            {
                drone = drone
            });
        }

        //ゲームオーバーになったプレイヤーを登録
        public void SetDestroyedDrone(BattleDrone drone)
        {
            int index = playerDatas.FindIndex(playerData => ReferenceEquals(playerData.drone, drone));
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
                timeText.enabled = true;
                timeText.text = "残 り " + i + " 分";

                yield return new WaitForSeconds(4f);
                timeText.enabled = false;
                yield return new WaitForSeconds(56f);
            }

            timeText.enabled = true;
            timeText.text = "残 り " + 1 + " 分";
            yield return new WaitForSeconds(1f);

            for (int i = 59; i >= 0; i--)
            {
                timeText.text = i + " 秒";
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
    }
}