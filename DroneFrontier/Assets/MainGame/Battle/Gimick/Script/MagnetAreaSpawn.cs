using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MagnetAreaSpawn : NetworkBehaviour
{
    [SerializeField] MagnetArea spawnArea = null;
    [SerializeField, Tooltip("スポーン確率(0～1)")] float spawnPercent = 0.5f;
    [SerializeField, Tooltip("スポーン判定を行う間隔")] float interval = 30f;
    [SerializeField, Tooltip("発生し続ける時間")] float activeTime = 20f;
    [SerializeField, Tooltip("スポーンする最小サイズ")] float minSize = 400f;
    [SerializeField, Tooltip("スポーンする最大サイズ")] float maxSize = 800f;
    [SerializeField, Tooltip("速度低下率")] float setDownPercent = 0.7f;

    //同時に発生する上限
    const int MAX_ACTIVE = 3;
    static int activeNum = 0;

    [SyncVar] GameObject spawnedArea = null;
    float deltaTime = 0;
    bool isActive = false;


    public override void OnStartClient()
    {
        base.OnStartClient();
        GetComponent<Renderer>().enabled = false;
    }

    [ServerCallback]
    void Start()
    {
        //scaleを1に戻す
        transform.localScale = new Vector3(1, 1, 1);

        MagnetArea ma = Instantiate(spawnArea);
        ma.DownPercent = setDownPercent;
        ma.transform.position = transform.position;
        NetworkServer.Spawn(ma.gameObject);
        spawnedArea = ma.gameObject;
    }

    [ServerCallback]
    void Update()
    {
        if (!isActive)
        {
            //最大数発生していたら発生処理を行わない
            if (activeNum >= MAX_ACTIVE) return;

            if (deltaTime >= interval)
            {
                if (Random.Range(0, 1.0f) <= spawnPercent)
                {
                    RpcSetAreaFlag(true);
                    float size = Random.Range(minSize, maxSize);
                    spawnedArea.transform.localScale = new Vector3(size, size, size);
                    activeNum++;
                    isActive = true;
                }
                deltaTime = 0;
            }
        }
        else
        {
            if (deltaTime >= activeTime)
            {
                RpcSetAreaFlag(false);
                activeNum--;
                isActive = false;
                deltaTime = 0;
            }
        }
        deltaTime += Time.deltaTime;
    }

    [ClientRpc]
    void RpcSetAreaFlag(bool flag)
    {
        spawnedArea.GetComponent<MagnetArea>().SetArea(flag);
    }
}
