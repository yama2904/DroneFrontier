using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class JammingBot : MonoBehaviour
    {
        float HP = 30.0f;
        [HideInInspector] public BaseDrone creater = null;


        private void Start()
        {
            Transform t = transform;  //キャッシュ

            //ボットの向きを変える
            Vector3 angle = t.localEulerAngles;
            angle.y += creater.transform.localEulerAngles.y;
            t.localEulerAngles = angle;

            //生成した自分のジャミングボットをプレイヤーがロックオン・照射しないように設定
            creater.GetComponent<DroneLockOnAction>().SetNotLockOnObject(gameObject);
            creater.GetComponent<DroneRadarAction>().SetNotRadarObject(gameObject);
        }

        private void OnDestroy()
        {
            if (creater == null) return;

            //SetNotLockOnObject、SetNotRadarObjectを解除
            if (creater.CompareTag(TagNameManager.PLAYER))
            {
                creater.GetComponent<DroneLockOnAction>().UnSetNotLockOnObject(gameObject);
                creater.GetComponent<DroneRadarAction>().UnSetNotRadarObject(gameObject);
            }

            //デバッグ用
            Debug.Log("ジャミングボット破壊");
        }

        public void Damage(float power)
        {
            float p = Useful.DecimalPointTruncation(power, 1);   //小数点第2以下切り捨て
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
                Destroy(gameObject);
            }

            //デバッグ用
            Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + HP);
        }

        private void OnCollisionEnter(Collision collision)
        {
            GameObject o = collision.gameObject;  //名前省略
            if (o.CompareTag(TagNameManager.BULLET))
            {
                IBullet b = o.GetComponent<IBullet>();
                if (b.PlayerID == creater.PlayerID) return;  //自分の弾なら当たり判定を行わない

                Destroy(o);
                Damage(b.Power);
            }
        }
    }
}