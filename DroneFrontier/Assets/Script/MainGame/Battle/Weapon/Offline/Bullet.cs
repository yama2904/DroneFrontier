using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class Bullet : MonoBehaviour
    {
        public float Power { get; protected set; } = 0;  //威力

        protected IBattleDrone shooter = null;
        protected GameObject target = null;   //誘導する対象
        protected float trackingPower = 0;    //追従力
        protected float speed = 0;            //1秒間に進む量
        protected float destroyTime = 0;      //発射してから消えるまでの時間(射程)

        //キャッシュ用
        Transform cacheTransform = null;


        protected virtual void Awake()
        {
            cacheTransform = GetComponent<Rigidbody>().transform;
        }

        void Start()
        {
            Destroy(gameObject, destroyTime);
        }

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

        public virtual void Init(IBattleDrone drone, float power, float trackingPower, float speed, float destroyTime, GameObject target = null)
        {
            shooter = drone;
            Power = power;
            this.trackingPower = trackingPower;
            this.speed = speed;
            this.destroyTime = destroyTime;
            this.target = target;
        }


        void OnTriggerEnter(Collider other)
        {
            //当たり判定を行わないオブジェクトは処理しない
            if (other.CompareTag(TagNameConst.BULLET)) return;
            if (other.CompareTag(TagNameConst.ITEM)) return;
            if (other.CompareTag(TagNameConst.GIMMICK)) return;
            if (other.CompareTag(TagNameConst.JAMMING)) return;
            if (other.CompareTag(TagNameConst.NOT_COLLISION)) return;

            //プレイヤーの当たり判定
            if (other.CompareTag(TagNameConst.PLAYER) || other.CompareTag(TagNameConst.CPU))
            {
                //撃った本人なら処理しない
                if (other.GetComponent<IBattleDrone>() == shooter) return;

                //ダメージ処理
                other.GetComponent<DroneDamageComponent>().Damage(shooter.GameObject, Power);

                // ToDo:CPU側で処理させる
                //if (other.CompareTag(TagNameManager.CPU))
                //{
                //    other.GetComponent<CPU.BattleDrone>().StartRotate(shooter.transform);
                //}
            }
            else if (other.CompareTag(TagNameConst.JAMMING_BOT))
            {
                //名前省略
                JammingBot jb = other.GetComponent<JammingBot>();

                //撃った人が放ったジャミングボットなら処理しない
                if (jb.creater == shooter) return;

                //ダメージ処理
                jb.Damage(Power);
            }
            Destroy(gameObject);
        }
    }
}