using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class Bulletaaa : MonoBehaviour
    {
        public float Power { get; protected set; } = 0;  //威力

        protected GameObject shooter = null;
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

        public virtual void Init(GameObject drone, float power, float trackingPower, float speed, float destroyTime, GameObject target = null)
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
            // 当たり判定を行わないオブジェクトは処理しない
            if (other.CompareTag(TagNameConst.BULLET)) return;
            if (other.CompareTag(TagNameConst.ITEM)) return;
            if (other.CompareTag(TagNameConst.GIMMICK)) return;
            if (other.CompareTag(TagNameConst.JAMMING)) return;
            if (other.CompareTag(TagNameConst.NOT_COLLISION)) return;

            // ダメージ可能インターフェースが実装されている場合はダメージを与える
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.Damage(shooter, Power);
            }

            Destroy(gameObject);
        }
    }
}