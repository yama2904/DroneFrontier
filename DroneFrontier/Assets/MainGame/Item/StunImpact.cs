using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class StunImpact : NetworkBehaviour
{
    [SyncVar, HideInInspector] public GameObject thrower = null;
    [SerializeField, Tooltip("スタン状態の時間")] float stunTime = 9.0f;
    float destroyTime = 0.5f;

    public override void OnStartClient()
    {
        base.OnStartClient();
        //爆発した直後に当たり判定を消す
        Invoke(nameof(FalseEnabledCollider), 0.05f);
    }

    [ServerCallback]
    void Start()
    {
        Invoke(nameof(DestroyMe), destroyTime);
    }

    void FalseEnabledCollider()
    {
        GetComponent<SphereCollider>().enabled = false;
    }

    void DestroyMe()
    {
        NetworkServer.Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        //投げたプレイヤーなら当たり判定から除外
        if (ReferenceEquals(other.gameObject, thrower)) return;
        if (!other.CompareTag(TagNameManager.PLAYER)) return;   //プレイヤーのみ対象

        BattlePlayer p = other.GetComponent<BattlePlayer>();
        if (!p.isLocalPlayer) return;   //ローカルプレイヤーのみ処理
        p.SetStun(stunTime);

        //必要なら距離によるスタンの時間を変える処理をいつか加える
        //
        //

    }
}
