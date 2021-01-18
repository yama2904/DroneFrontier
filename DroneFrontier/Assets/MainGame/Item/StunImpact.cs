using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class StunImpact : NetworkBehaviour
{
    [SyncVar, HideInInspector] public GameObject thrower = null;
    [SerializeField] float stunTime = 9.0f;
    [SerializeField] float destroyTime = 0.5f;

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

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (ReferenceEquals(other.gameObject, thrower))    //投げたプレイヤーなら当たり判定から除外
        {
            return;
        }

        if (other.CompareTag(TagNameManager.PLAYER))
        {
            other.GetComponent<Player>().TargetSetStun(other.GetComponent<NetworkIdentity>().connectionToClient, stunTime);

            //必要なら距離によるスタンの時間を変える処理をいつか加える
            //
            //
        }
    }
}
