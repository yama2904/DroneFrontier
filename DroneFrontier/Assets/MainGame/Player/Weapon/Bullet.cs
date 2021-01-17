using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    [SyncVar, HideInInspector] public GameObject Shooter = null;  //撃ったプレイヤー
    [SyncVar, HideInInspector] public GameObject Target = null;   //誘導する対象
    [SyncVar, HideInInspector] public float TrackingPower = 0;    //追従力
    [SyncVar, HideInInspector] public float Power = 0;            //威力
    [SyncVar, HideInInspector] public float SpeedPerSecond = 0;   //1秒間に進む量
    [SyncVar, HideInInspector] public float DestroyTime = 0;      //発射してから消えるまでの時間(射程)

    protected Transform cacheTransform = null;


    void Start()
    {
        cacheTransform = transform;
        Invoke(nameof(DestroyMe), DestroyTime);
    }

    void Update()
    {
    }

    [ServerCallback]
    protected virtual void FixedUpdate()
    {
        if (Target != null)
        {
            Vector3 diff = Target.transform.position - cacheTransform.position;  //座標の差
            //視野内に敵がいる場合
            if (Vector3.Dot(cacheTransform.forward, diff) > 0)
            {
                //自分の方向から見た敵の位置の角度
                float angle = Vector3.Angle(cacheTransform.forward, diff);
                if (angle > TrackingPower)
                {
                    //誘導力以上の角度がある場合は修正
                    angle = TrackingPower;
                }

                //+値と-値のどちらに回転するか上下と左右ごとに判断する
                Vector3 axis = Vector3.Cross(cacheTransform.forward, diff);
                //左右の回転
                float x = angle * (axis.y < 0 ? -1 : 1);
                cacheTransform.RotateAround(cacheTransform.position, Vector3.up, x);

                //上下の回転
                float y = angle * (axis.x < 0 ? -1 : 1);
                cacheTransform.RotateAround(cacheTransform.position, Vector3.right, y);
            }
        }
        //移動
        cacheTransform.position += cacheTransform.forward * SpeedPerSecond * Time.deltaTime;
    }

    void DestroyMe()
    {
        NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    protected virtual void OnTriggerEnter(Collider other)
    {
        //撃ったプレイヤーなら当たり判定を行わない
        if (ReferenceEquals(other.gameObject, Shooter))
        {
            return;
        }

        //プレイヤーかCPUの当たり判定
        if (other.CompareTag(TagNameManager.PLAYER) || other.CompareTag(TagNameManager.CPU))
        {
            Player bp = other.GetComponent<Player>();
            bp.CmdDamage(Power);
            DestroyMe();
        }
        else if (other.CompareTag(TagNameManager.JAMMING_BOT))
        {
            JammingBot jb = other.GetComponent<JammingBot>();
            if (ReferenceEquals(jb.Creater, Shooter))
            {
                return;
            }
            jb.CmdDamage(Power);
            DestroyMe();
        }
    }
}
