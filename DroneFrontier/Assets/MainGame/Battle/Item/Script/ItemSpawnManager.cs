using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class ItemSpawnManager : NetworkBehaviour
{
    //シングルトン
    static ItemSpawnManager singleton;
    public static ItemSpawnManager Singleton { get { return singleton; } }

    [SerializeField, Tooltip("フィールド上に出現させるアイテムの上限")] int maxSpawnNum = 10;
    [SerializeField, Tooltip("アイテムが出現する間隔")] float spawnInterval = 10f;
    [SerializeField, Tooltip("定期的にスポーンするアイテムの数")] int spawnNum = 1;
    ItemSpawn[] spawnItems;
    int useSpawnItemsIndex = 0;
    int spawningNum = 0;  //スポーン中のアイテムの数
    float spawnCountTime = 0;


    public override void OnStartClient()
    {
        base.OnStartClient();

        //シングルトンの作成
        singleton = this;
    }

    [ServerCallback]
    void Start()
    {
        spawnItems = GameObject.FindGameObjectsWithTag(TagNameManager.ITEM_SPAWN)
                     .Select(o => o.GetComponent<ItemSpawn>()).ToArray();

        //処理の無駄なのでアイテムがなかったらスキップ
        if (spawnItems.Length == 0) return;

        //アイテムのランダムスポーン
        ItemSpawn(maxSpawnNum);
    }

    [ServerCallback]
    void Update()
    {
        //既に最大数スポーンしていたら処理しない
        if (spawningNum >= maxSpawnNum) return;

        //フィールド上の全てのアイテムをスポーンしていたら処理しない
        if (spawningNum >= spawnItems.Length) return;

        spawnCountTime += Time.deltaTime;
        if (spawnCountTime >= spawnInterval)
        {
            ItemSpawn(spawnNum);
            spawnCountTime = 0;
        }
    }

    [Server]
    void ItemSpawn(int spawnNum)
    {
        int spawnCount = 0;
        while (spawnCount < spawnNum)
        {
            useSpawnItemsIndex++;
            if (useSpawnItemsIndex >= spawnItems.Length)   //配列の末尾に到達したら0に戻す
            {
                useSpawnItemsIndex = 0;
            }

            //上限までスポーンしていたら終了
            if (spawnCount >= spawnNum) break;
            if (spawningNum >= maxSpawnNum) break;
            if (spawningNum >= spawnItems.Length) break;


            //スポーンに成功したらカウント更新
            if (spawnItems[useSpawnItemsIndex].RandomSpawn())
            {
                spawningNum++;
                spawnCount++;
            }
        }
    }

    [Server]
    public void NewItemSpawn()
    {
        spawningNum--;
    }
}
