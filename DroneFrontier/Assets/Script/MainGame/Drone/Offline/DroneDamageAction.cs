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

        //1フレームに8ヒットまで
        int damageCount = 0;
        const int MAX_COUNT_ONE_FRAME = 8;


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

        private void LateUpdate()
        {
            damageCount = 0;
        }


        public void Damage(float power)
        {
            if (HP <= 0) return;
            if (isNonDamage) return;

            DamageMe(power);
        }


        void SetNonDamage(bool flag)
        {
            isNonDamage = flag;
        }

        void DamageMe(float power)
        {
            if (damageCount > MAX_COUNT_ONE_FRAME) return;

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
                Debug.Log(name + "の残りHP: " + HP);
            }

            damageCount++;
        }
    }
}