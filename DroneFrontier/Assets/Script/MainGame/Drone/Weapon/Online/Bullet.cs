using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class Bullet : NetworkBehaviour
    {
        [SyncVar] protected uint shooter;               //撃ったプレイヤー
        [SyncVar] protected GameObject target = null;   //誘導する対象
        [SyncVar] protected float trackingPower = 0;    //追従力
        [SyncVar] protected float power = 0;            //威力
        [SyncVar] protected float speed = 0;            //1秒間に進む量
        [SyncVar] protected float destroyTime = 0;      //発射してから消えるまでの時間(射程)

        //キャッシュ用
        protected Transform cacheTransform = null;


        void Start()
        {
            cacheTransform = GetComponent<Rigidbody>().transform;
            Invoke(nameof(DestroyMe), destroyTime);
        }

        [ServerCallback]
        protected virtual void FixedUpdate()
        {
            if (target != null)
            {
                Vector3 diff = target.transform.position - cacheTransform.position;  //座標の差

                //視野内に敵がいる場合
                if (Vector3.Dot(cacheTransform.forward, diff) > 0)
                {
                    //自分の方向から見た敵の位置の角度
                    float angle = Vector3.Angle(cacheTransform.forward, diff);
                    if (angle > trackingPower)
                    {
                        //誘導力以上の角度がある場合は修正
                        angle = trackingPower;
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
            cacheTransform.position += cacheTransform.forward * speed * Time.deltaTime;
        }


        public virtual void Init(uint shooterNetId, float power, float trackingPower, float speed, float destroyTime, GameObject target = null)
        {
            shooter = shooterNetId;
            this.power = power;
            this.trackingPower = trackingPower;
            this.speed = speed;
            this.destroyTime = destroyTime;
            this.target = target;
        }


        void DestroyMe()
        {
            NetworkServer.Destroy(gameObject);
        }

        [ServerCallback]
        protected virtual void OnTriggerEnter(Collider other)
        {
            //当たり判定を行わないオブジェクトは処理しない
            if (other.CompareTag(TagNameConst.BULLET)) return;
            if (other.CompareTag(TagNameConst.ITEM)) return;
            if (other.CompareTag(TagNameConst.GIMMICK)) return;
            if (other.CompareTag(TagNameConst.JAMMING_AREA)) return;
            if (other.CompareTag(TagNameConst.TOWER)) return;

            //プレイヤーの当たり判定
            if (other.CompareTag(TagNameConst.PLAYER))
            {
                DroneDamageAction player = other.GetComponent<DroneDamageAction>();
                if (player.netId == shooter) return;   //撃った本人なら処理しない
                player.CmdDamage(power);
            }
            else if (other.CompareTag(TagNameConst.JAMMING_BOT))
            {
                JammingBot jb = other.GetComponent<JammingBot>();
                if (ReferenceEquals(jb.creater, shooter))
                {
                    return;
                }
                jb.CmdDamage(power);
            }
            DestroyMe();
        }
    }
}