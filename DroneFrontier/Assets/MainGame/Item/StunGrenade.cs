using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunGrenade : MonoBehaviour
{
    BasePlayer throwPlayer = null;
    [SerializeField] StunImpact stunImpact = null;
    [SerializeField] Transform throwRotate = null;

    [SerializeField] float throwPower = 10.0f;  //投げる速度
    [SerializeField] float impactTime = 1.0f;   //着弾時間
    float deltaTime;    //計測用

    void Start()
    {
    }

    void Update()
    {
        if(deltaTime >= impactTime)
        {
            CreateImpact();
        }
        deltaTime += Time.deltaTime;
    }

    public void ThrowGrenade(BasePlayer player)
    {
        Transform cacheTransform = transform;   //キャッシュ用
        throwPlayer = player;

        //playerの座標と向きのコピー
        cacheTransform.position = player.transform.position;
        cacheTransform.rotation = player.transform.rotation;

        //Vector3 force = Vector3.Scale(cacheTransform.forward, throwRotate.forward);
        GetComponent<Rigidbody>().AddForce(throwRotate.forward * throwPower, ForceMode.Impulse);
    }

    //スタングレネードを爆破させる
    void CreateImpact()
    {
        StunImpact s = Instantiate(stunImpact, transform.position, Quaternion.Euler(0, 0, 0)).GetComponent<StunImpact>();
        s.ThrowPlayer = throwPlayer;
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            if(ReferenceEquals(bp, throwPlayer))    //投げたプレイヤーなら当たり判定から除外
            {
                return;
            }
            CreateImpact();
        }
    }
}
