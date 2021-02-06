using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class DroneDamageAction : MonoBehaviour
    {
        const float MAX_HP = 30;
        public float HP { get; private set; } = MAX_HP;
        BaseDrone drone = null;
        DroneBarrierAction barrierAction = null;
        [SerializeField] float nonDamageTime = 4f;
        bool isNonDamage = false;

        float damageInterval = 1f / 15;
        float damageCountTime = 0;

        void Awake()
        {
            drone = GetComponent<BaseDrone>();
            barrierAction = GetComponent<DroneBarrierAction>();
        }

        void Start()
        {

        }

        void Update()
        {
            if(Time.time - drone.StartTime <= nonDamageTime)
            {
                if (!isNonDamage)
                {
                    SetNonDamage(true);
                }
            }
            else
            {
                if (isNonDamage)
                {
                    SetNonDamage(false);
                }
            }
        }

        private void FixedUpdate()
        {
            damageCountTime += Time.deltaTime;
            if(damageCountTime >= damageInterval)
            {
                damageCountTime = damageInterval;
            }
        }


        public void Damage(float power)
        {
            if (HP <= 0) return;
            if (isNonDamage) return;
            if (damageCountTime < damageInterval) return;

            DamageMe(power);
        }


        void SetNonDamage(bool flag)
        {
            isNonDamage = flag;
        }

        void DamageMe(float power)
        {
            //小数点第2以下切り捨て
            float p = Useful.DecimalPointTruncation(power, 1);

            if (barrierAction.HP > 0)
            {
                barrierAction.Damage(p);
            }
            else
            {
                HP -= p;
                if (HP <= 0)
                {
                    HP = 0;
                }

                //デバッグ用
                Debug.Log(name + "に" + power + "のダメージ\n残りHP: " + HP);
            }

            damageCountTime = 0;
        }

        private void OnTriggerStay(Collider other)
        {
            if (HP <= 0) return;
            if (isNonDamage) return;
            if (damageCountTime < damageInterval) return;

            GameObject o = other.gameObject;  //名前省略
            if (o.CompareTag(TagNameManager.BULLET))
            {
                IBullet b = o.GetComponent<IBullet>();  //名前省略
                if (b.PlayerID == drone.PlayerID) return;  //自分の弾なら当たり判定を行わない

                Destroy(o);
                DamageMe(b.Power);
            }
        }
    }
}