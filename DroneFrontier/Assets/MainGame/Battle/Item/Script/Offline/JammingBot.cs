using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class JammingBot : MonoBehaviour
    {
        float HP = 30.0f;
        [HideInInspector] public GameObject creater = null;


        private void Start()
        {
            Transform t = transform;  //キャッシュ

            //ボットの向きを変える
            Vector3 angle = t.localEulerAngles;
            angle.y += creater.transform.localEulerAngles.y;
            t.localEulerAngles = angle;

            //生成した自分のジャミングボットをプレイヤーがロックオン・照射しないように設定
            if (creater.CompareTag(TagNameManager.PLAYER))
            {
                creater.GetComponent<BattleDrone>().SetNotLockOnObject(gameObject);
                creater.GetComponent<BattleDrone>().SetNotRadarObject(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (creater == null) return;

            //SetNotLockOnObject、SetNotRadarObjectを解除
            if (creater.CompareTag(TagNameManager.PLAYER))
            {
                creater.GetComponent<BattleDrone>().UnSetNotLockOnObject(gameObject);
                creater.GetComponent<BattleDrone>().UnSetNotRadarObject(gameObject);
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
        }
    }
}