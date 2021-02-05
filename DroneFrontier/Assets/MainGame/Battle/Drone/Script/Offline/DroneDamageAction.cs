using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class DroneDamageAction : MonoBehaviour
    {
        const float MAX_HP = 30;
        public float HP { get; private set; } = MAX_HP;
        BattleDrone drone = null;
        DroneBarrierAction barrierAction = null;
        [SerializeField] float nonDamageTime = 4f;
        bool isNonDamage = false;

        void Awake()
        {
            drone = GetComponent<BattleDrone>();
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

        void SetNonDamage(bool flag)
        {
            isNonDamage = flag;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (HP <= 0) return;
            if (isNonDamage) return;

            bool isDamage = false;  //ダメージ処理を行う場合はtrue
            float power = 0;
            GameObject o = collision.gameObject;  //名前省略
            if (o.CompareTag(TagNameManager.BULLET))
            {
                power = o.GetComponent<Bullet>().Power;
                Destroy(o);
                isDamage = true;
            }
            if (o.CompareTag(TagNameManager.LASE_BULLET))
            {
                power = o.GetComponent<LaserBullet>().Power;
                isDamage = true;
            }

            if (isDamage)
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
                    if(HP <= 0)
                    {
                        HP = 0;
                    }

                    //デバッグ用
                    Debug.Log(name + "に" + power + "のダメージ\n残りHP: " + HP);
                }
            }
        }
    }
}