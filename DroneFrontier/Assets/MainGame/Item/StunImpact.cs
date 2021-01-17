using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class StunImpact : NetworkBehaviour
{
    public GameObject Thrower { private get; set; } = null;
    [SerializeField] float stunTime = 9.0f;
    [SerializeField] float destroyTime = 0.5f;

    void Start()
    {
        Invoke(nameof(DestroyMe), destroyTime);

        //爆発した直後に当たり判定を消す
        Invoke(nameof(FalseEnabledCollider), 0.05f);
    }

    void Update() { }

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
        if (ReferenceEquals(other.gameObject, Thrower))    //投げたプレイヤーなら当たり判定から除外
        {
            return;
        }

        if (other.CompareTag(TagNameManager.PLAYER))
        {
            IPlayerStatus ps = other.GetComponent<Player>();
            ps.SetStun(stunTime);

            //必要なら距離によるスタンの時間を変える処理をいつか加える
            //
            //
        }
    }
}
