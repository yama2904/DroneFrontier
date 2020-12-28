using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunImpact : MonoBehaviour
{
    public BasePlayer ThrowPlayer { private get; set; } = null;
    [SerializeField] StunScreenMask stunScreenMask = null;
    [SerializeField] float destroyTime = 0.5f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Player.PLAYER_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            if (ReferenceEquals(bp, ThrowPlayer))    //投げたプレイヤーなら当たり判定から除外
            {
                return;
            }
            StunScreenMask s = Instantiate(stunScreenMask).GetComponent<StunScreenMask>();

            //必要なら距離によるスタンの時間を変える処理をいつか加える
            //
            //
        }
    }
}
