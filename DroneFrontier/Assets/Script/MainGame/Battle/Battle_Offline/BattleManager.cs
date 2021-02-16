using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    public class BattleManager : MainGameManager
    {
        [SerializeField] GameObject playerPrefab = null;
        [SerializeField] GameObject cpuPrefab = null;

        //シングルトン
        public new static BattleManager Singleton { get; private set; }

        //アイテムを出現させるか
        public static bool IsItemSpawn { get; set; } = true;

        //ストック
        const int MAX_STOCK = 1;
        [SerializeField] Image stockIcon = null;
        [SerializeField] Text stockText = null;

        //プレイヤー情報
        public class PlayerData
        {
            public GameObject drone = null;
            public BaseWeapon.Weapon weapon = BaseWeapon.Weapon.NONE;
            public string name = "";
            public int stock = MAX_STOCK;
            public float destroyTime = 0;
            public bool isDestroy = false;
            public bool isPlayer = false;
            public static int droneNum = 0;  //残っているドローンの数
        }
        List<PlayerData> playerDatas = new List<PlayerData>();
        int useIndex = 0;
        bool isPlayerDestroy = false;

        //ゲーム終了処理を行ったらtrue
        bool isFinished = false;

        //ゲーム開始時に生成
        [SerializeField] ItemCreateManager itemCreateManager = null;

        //残り時間
        [SerializeField] Text timeText = null;
        [SerializeField, Tooltip("制限時間(分)")] int maxTime = 5;
        Coroutine countCoroutine = null;

        //キャッシュ用
        AudioListener listener = null;


        protected override void Awake()
        {
            base.Awake();
            Singleton = this;
        }

        protected override void Start()
        {
            base.Start();

            if (!isSolo)
            {
                //ドローンの生成
                foreach (CPUSelectScreenManager.CPUData cd in CPUSelectScreenManager.CPUDatas)
                {
                    //CPUの生成
                    playerDatas.Add(new PlayerData
                    {
                        drone = CreateDrone(cd.weapon, false),
                        weapon = cd.weapon,
                        name = cd.name,
                        isPlayer = false
                    });
                }
                //プレイヤーの生成
                playerDatas.Add(new PlayerData
                {
                    drone = CreateDrone(WeaponSelectScreenManager.weapon, true),
                    weapon = WeaponSelectScreenManager.weapon,
                    name = "Player",
                    isPlayer = true
                });
            }

            //残りプレイヤー数の初期化
            PlayerData.droneNum = playerNum;

            //残機UIの表示
            stockIcon.enabled = true;
            stockText.enabled = true;
            stockText.text = MAX_STOCK.ToString();


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
                itemCreateManager = Instantiate(itemCreateManager);
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

            //プレイヤーがゲームオーバーになったらスペースキーで各CPUのカメラに切り替える
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isPlayerDestroy)
                {
                    //次のプレイヤーのカメラとリスナーに切り替える
                    int initIndex = useIndex;
                    if (playerDatas[useIndex] != null)
                    {
                        if (playerDatas[useIndex].drone.CompareTag(TagNameManager.CPU))
                        {
                            //名前省略
                            CPU.BattleDrone drone = playerDatas[useIndex].drone.GetComponent<CPU.BattleDrone>();

                            drone.SetCameraDepth(0);
                            drone.SetAudioListener(false);
                        }
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

                        Debug.Log("useIndex: " + useIndex);
                        //バグ防止
                        if (playerDatas[useIndex] == null)
                        {
                            continue;
                        }
                        //破壊されていたらスキップ
                        if (playerDatas[useIndex].isDestroy)
                        {
                            continue;
                        }
                        //CPUのみ処理
                        if (!playerDatas[useIndex].drone.CompareTag(TagNameManager.CPU))
                        {
                            continue;
                        }
                        else
                        {
                            //名前省略
                            CPU.BattleDrone bd = playerDatas[useIndex].drone.GetComponent<CPU.BattleDrone>();

                            bd.SetCameraDepth(5);
                            bd.SetAudioListener(true);
                            break;
                        }
                    }
                }
            }

            //破壊されたドローンを調べる
            foreach (PlayerData pd in playerDatas)
            {
                if (pd.isDestroy) continue;
                if (pd.drone == null)
                {
                    //ストックが残っていたら復活
                    if (pd.stock > 0)
                    {
                        pd.stock--;
                        pd.destroyTime = Time.time;
                        pd.drone = CreateDrone(pd.weapon, pd.isPlayer);

                        //リスポーンSEの再生
                        pd.drone.GetComponent<DroneSoundAction>().PlayOneShot(SoundManager.SE.RESPAWN, SoundManager.BaseSEVolume);

                        //残機UIの変更
                        if (pd.isPlayer)
                        {
                            stockText.text = pd.stock.ToString();
                        }
                    }
                    //ストックが残っていなかったらランキングに記録
                    else
                    {
                        pd.isDestroy = true;
                        ranking[PlayerData.droneNum - 1] = pd.name;
                        PlayerData.droneNum--;

                        //残機UIの非表示
                        if (pd.isPlayer)
                        {
                            stockIcon.enabled = false;
                            stockText.enabled = false;

                            listener.enabled = true;
                            isPlayerDestroy = true;
                        }
                    }
                }
            }

            //最後のプレイヤーが残ったら終了処理
            if (PlayerData.droneNum <= 1)
            {
                if (isSolo) return;
                foreach (PlayerData pd in playerDatas)
                {
                    if (pd.isDestroy) continue;
                    ranking[PlayerData.droneNum - 1] = pd.name;
                    break;
                }
                FinishGame();
            }
        }

        //変数の初期化
        protected override void OnDestroy()
        {
            base.OnDestroy();

            playerDatas.Clear();
            PlayerData.droneNum = 0;
        }


        GameObject CreateDrone(BaseWeapon.Weapon weapon, bool isPlayer)
        {
            Transform pos = StartPosition.Singleton.GetPos();
            GameObject o = null;

            //プレイヤーの生成
            if (isPlayer)
            {
                o = Instantiate(playerPrefab, pos.position, pos.rotation);
                o.GetComponent<Player.BattleDrone>().setSubWeapon = weapon;
                return o;
            }
            //CPUの生成
            o = Instantiate(cpuPrefab, pos.position, pos.rotation);
            o.GetComponent<CPU.BattleDrone>().setSubWeapon = weapon;
            return o;
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

            //残りストック数と死亡した時間に応じてランキング設定
            int stock = 0;
            while (PlayerData.droneNum > 0)
            {
                List<int> destroyTimeIndex = new List<int>();
                for (int i = 0; i < playerDatas.Count; i++)
                {
                    if (playerDatas[i].isDestroy) continue;
                    if (playerDatas[i].stock == stock)
                    {
                        destroyTimeIndex.Add(i);
                    }
                }

                //死亡した時間に応じてランキング順位変動
                while (destroyTimeIndex.Count > 0)
                {
                    int minIndex = 0;
                    for (int i = 0; i < destroyTimeIndex.Count; i++)
                    {
                        //死亡した時間が早いほどランキングが低い
                        if (playerDatas[destroyTimeIndex[i]].destroyTime < playerDatas[destroyTimeIndex[minIndex]].destroyTime)
                        {
                            minIndex = i;
                        }
                    }

                    //順位決定
                   ranking[PlayerData.droneNum - 1] = playerDatas[destroyTimeIndex[minIndex]].name;
                    playerDatas[destroyTimeIndex[minIndex]].isDestroy = true;
                    PlayerData.droneNum--;

                    //使用したインデックスは削除
                    destroyTimeIndex.RemoveAt(minIndex);
                }

                stock++;
            }

            FinishGame();
        }

        //ゲームの終了処理
        void FinishGame()
        {
            if (!isFinished)
            {
                FinishGame(ranking);
                isFinished = true;

                StopCoroutine(countCoroutine);

                timeText.enabled = false;
            }
        }
    }
}