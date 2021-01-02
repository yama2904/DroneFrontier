using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunImpact : MonoBehaviour
{
    public GameObject Thrower { private get; set; } = null;
    [SerializeField] float stunTime = 9.0f;
    [SerializeField] float destroyTime = 0.5f;

    IEnumerator Start()
    {
        Destroy(gameObject, destroyTime);

        //爆発した直後に当たり判定を消す
        yield return new WaitForSeconds(0.05f);
        GetComponent<SphereCollider>().enabled = false;
    }

    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (ReferenceEquals(other.gameObject, Thrower))    //投げたプレイヤーなら当たり判定から除外
        {
            return;
        }

        if (other.CompareTag(Player.PLAYER_TAG))
        {
            IPlayerStatus ps = other.GetComponent<BasePlayer>();
            ps.SetStun(stunTime);

            //必要なら距離によるスタンの時間を変える処理をいつか加える
            //
            //
        }
    }
}
