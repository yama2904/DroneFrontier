using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MagnetAreaSpawn : NetworkBehaviour
{
    //コンポーネント群
    Transform cacheTransform = null;
    MagnetArea magnetArea = null;
    [SerializeField] ParticleSystem particle1 = null;
    [SerializeField] ParticleSystem particle2 = null;

    [Header("パラメータ調整")]
    [SerializeField, Tooltip("スポーン確率(0～1)")] float spawnPercent = 0.5f;
    [SerializeField, Tooltip("スポーン判定を行う間隔")] float interval = 30f;
    [SerializeField, Tooltip("発生し続ける時間")] float activeTime = 20f;
    [SerializeField, Tooltip("スポーンする最小サイズ")] float minSize = 1f;
    [SerializeField, Tooltip("スポーンする最大サイズ")] float maxSize = 3f;

    //同時に発生する上限
    const int MAX_ACTIVE = 3;
    static int activeNum = 0;

    float deltaTime = 0;
    bool isActive = false;


    public override void OnStartClient()
    {
        base.OnStartClient();
        cacheTransform = transform;
        magnetArea = GetComponent<MagnetArea>();
        magnetArea.SetArea(false);
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
                    magnetArea.RpcSetArea(true);
                    float size = Random.Range(minSize, maxSize);
                    RpcSetSize(size);
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
                magnetArea.RpcSetArea(false);
                activeNum--;
                isActive = false;
                deltaTime = 0;
            }
        }
        deltaTime += Time.deltaTime;
    }

    [ClientRpc]
    void RpcSetSize(float size)
    {
        cacheTransform.localScale = new Vector3(size, size, size);
        particle1.transform.localScale = new Vector3(size * 5, size * 5, size * 5);
        particle2.transform.localScale = new Vector3(size * 5, size * 5, size * 5);
    }
}
