using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ItemSpawn : NetworkBehaviour
{
    [SerializeField, Tooltip("スポーンするアイテム")] Item spawnItem = null;
    [SerializeField, Tooltip("スポーン確率(0～1)")] float spawnPercent = 0.5f;
    Item spawnedItem = null;
    public float SpawnPercent { get { return spawnPercent; } }

    //キャッシュ用
    Transform cacheTransform = null;


    public override void OnStartClient()
    {
        base.OnStartClient();
        cacheTransform = transform;
        GetComponent<Renderer>().enabled = false;

        //scaleを1に戻す
        transform.localScale = new Vector3(1, 1, 1);
    }

    void Start()
    {
        
    }

    void LateUpdate()
    {
        if(transform.position.y < 57.0f)
        {
            transform.position = new Vector3(transform.position.x, 57.0f, transform.position.z);
        }
    }

    //スポーンしたらtrue
    [Server]
    public bool RandomSpawn()
    {
        //既にスポーンしていて取得されていなかったらスポーンしない
        if (spawnedItem != null) return false;

        if (Random.Range(0, 1.0f) <= spawnPercent)
        {
            spawnedItem = Instantiate(spawnItem);
            spawnedItem.parentNetId = netId;
            spawnedItem.SetRandomItemType();
            NetworkServer.Spawn(spawnedItem.gameObject, connectionToClient);

            return true;
        }
        return false;
    }
}
