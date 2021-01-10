using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    public const string BULLET_TAG = "Bullet";

    public GameObject Shooter { protected get; set; } = null;  //撃ったプレイヤー
    public GameObject Target { private get; set; } = null;     //誘導する対象
    public float TrackingPower { protected get; set; } = 0;    //追従力
    public float Power { protected get; set; } = 0;            //威力
    public float SpeedPerSecond { protected get; set; } = 0;   //1秒間に進む量
    public float DestroyTime { protected get; set; } = 0;      //発射してから消えるまでの時間(射程)

    protected Transform cacheTransform = null;


    protected virtual void Start()
    {
        cacheTransform = transform;
        Destroy(gameObject, DestroyTime);
    }

    protected virtual void Update()
    {
    }

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

    protected virtual void OnTriggerEnter(Collider other)
    {
        //撃ったプレイヤーなら当たり判定を行わない
        if (ReferenceEquals(other.gameObject, Shooter))
        {
            return;
        }

        //プレイヤーかCPUの当たり判定
        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {            
            BasePlayer bp = other.GetComponent<BasePlayer>();
            bp.Damage(Power);
            Destroy(gameObject);
        }
        else if (other.CompareTag(JammingBot.JAMMING_BOT_TAG))
        {
            JammingBot jb = other.GetComponent<JammingBot>();
            if (ReferenceEquals(jb.Creater, Shooter))
            {
                return;
            }
            jb.Damage(Power);
            Destroy(gameObject);
        }
    }
}
