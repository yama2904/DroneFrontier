using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunGrenade : MonoBehaviour
{
    GameObject thrower = null;
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
        if (deltaTime >= impactTime)
        {
            CreateImpact();
        }
        deltaTime += Time.deltaTime;
    }

    public void ThrowGrenade(GameObject thrower)
    {
        Transform cacheTransform = transform;   //キャッシュ用
        this.thrower = thrower;

        //playerの座標と向きのコピー
        cacheTransform.position = thrower.transform.position;
        cacheTransform.rotation = thrower.transform.rotation;

        //Vector3 force = Vector3.Scale(cacheTransform.forward, throwRotate.forward);
        GetComponent<Rigidbody>().AddForce(throwRotate.forward * throwPower, ForceMode.Impulse);
    }

    //スタングレネードを爆破させる
    void CreateImpact()
    {
        StunImpact s = Instantiate(stunImpact, transform.position, Quaternion.identity).GetComponent<StunImpact>();
        s.Thrower = thrower;
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        //投げたプレイヤーなら当たり判定から除外
        if (ReferenceEquals(other.gameObject, thrower))
        {
            return;
        }

        if (other.CompareTag(TagNameManager.PLAYER) || other.CompareTag(TagNameManager.CPU))
        {
            CreateImpact();
        }
    }
}
