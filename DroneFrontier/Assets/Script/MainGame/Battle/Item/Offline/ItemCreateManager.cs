using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Offline
{
    public class ItemCreateManager : MonoBehaviour
    {
        //シングルトン
        static ItemCreateManager singleton;
        public static ItemCreateManager Singleton { get { return singleton; } }

        [SerializeField, Tooltip("フィールド上に出現させるアイテムの上限")] int maxSpawnNum = 10;
        [SerializeField, Tooltip("アイテムが出現する間隔")] float spawnInterval = 10f;
        [SerializeField, Tooltip("定期的にスポーンするアイテムの数")] int spawnNum = 1;
        ItemCreate[] createItems;
        int useSpawnItemsIndex = 0;
        int spawningNum = 0;  //スポーン中のアイテムの数
        float spawnTimeCount = 0;  //時間計測


        void Awake()
        {
            //シングルトンの作成
            singleton = this;
        }

        void Start()
        {
            createItems = GameObject.FindGameObjectsWithTag(TagNameManager.ITEM_SPAWN)
                         .Select(o => o.GetComponent<ItemCreate>()).ToArray();

            //処理の無駄なのでアイテムがなかったらスキップ
            if (createItems.Length == 0) return;

            //アイテムのランダムスポーン
            ItemSpawn(maxSpawnNum);
        }

        void Update()
        {
            //既に最大数スポーンしていたら処理しない
            if (spawningNum >= maxSpawnNum) return;

            //フィールド上の全てのアイテムをスポーンしていたら処理しない
            if (spawningNum >= createItems.Length) return;

            spawnTimeCount += Time.deltaTime;
            if (spawnTimeCount >= spawnInterval)
            {
                ItemSpawn(spawnNum);
                spawnTimeCount = 0;
            }
        }

        void ItemSpawn(int spawnNum)
        {
            int spawnCount = 0;
            while (spawnCount < spawnNum)
            {
                useSpawnItemsIndex++;
                if (useSpawnItemsIndex >= createItems.Length)   //配列の末尾に到達したら0に戻す
                {
                    useSpawnItemsIndex = 0;
                }

                //上限までスポーンしていたら終了
                if (spawnCount >= spawnNum) break;
                if (spawningNum >= maxSpawnNum) break;
                if (spawningNum >= createItems.Length) break;


                //スポーンに成功したらカウント更新
                if (createItems[useSpawnItemsIndex].RandomSpawn())
                {
                    spawningNum++;
                    spawnCount++;
                }
            }
        }

        public void NewItemSpawn()
        {
            spawningNum--;
        }
    }
}