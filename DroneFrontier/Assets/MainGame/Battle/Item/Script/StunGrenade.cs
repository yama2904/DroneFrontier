﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class StunGrenade : NetworkBehaviour
{
    [SyncVar] GameObject thrower = null;
    [SerializeField] StunImpact stunImpact = null;
    [SerializeField ,Tooltip("投げる角度")] Transform throwRotate = null;

    [SerializeField, Tooltip("投げる速度")] float throwPower = 10.0f;  //投げる速度
    [SerializeField, Tooltip("着弾時間")] float impactTime = 1.0f;   //着弾時間

    public override void OnStartClient()
    {
        base.OnStartClient();

        GetComponent<Rigidbody>().isKinematic = true;
        ServerInit();
    }

    [ServerCallback]
    void Start()
    {
    }

    [ServerCallback]
    void ServerInit()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddForce(throwRotate.forward * throwPower, ForceMode.Impulse);
        Invoke(nameof(CreateImpact), impactTime);
    }

    public void ThrowGrenade(GameObject thrower)
    {
        Transform cacheTransform = transform;   //キャッシュ用
        this.thrower = thrower;

        //playerの座標と向きのコピー
        cacheTransform.position = thrower.transform.position;
        cacheTransform.rotation = thrower.transform.rotation;
    }

    //スタングレネードを爆破させる
    [ServerCallback]
    void CreateImpact()
    {
        StunImpact s = Instantiate(stunImpact, transform.position, Quaternion.identity).GetComponent<StunImpact>();
        s.thrower = thrower;
        NetworkServer.Spawn(s.gameObject, connectionToClient);

        NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (ReferenceEquals(other.gameObject, thrower)) return;  //投げたプレイヤーなら当たり判定から除外
        //特定のオブジェクトはすり抜け
        if (other.CompareTag(TagNameManager.ITEM)) return;
        if (other.CompareTag(TagNameManager.GIMMICK)) return;
        if (other.CompareTag(TagNameManager.JAMMING)) return;
        CreateImpact();
    }
}