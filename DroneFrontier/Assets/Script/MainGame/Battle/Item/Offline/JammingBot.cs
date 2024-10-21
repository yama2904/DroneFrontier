using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class JammingBot : MonoBehaviour, ILockableOn
    {
        /// <summary>
        /// ロックオン可能であるか
        /// </summary>
        public bool IsLockableOn { get; } = true;

        /// <summary>
        /// ロックオン不可にするオブジェクト
        /// </summary>
        public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

        float HP = 30.0f;
        [HideInInspector] public IBattleDrone creater = null;


        private void Start()
        {
            Transform t = transform;  //キャッシュ

            // ToDo:未定
            //ボットの向きを変える
            //Vector3 angle = t.localEulerAngles;
            //angle.y += creater.transform.localEulerAngles.y;
            //t.localEulerAngles = angle;

            // 生成した自分のジャミングボットをプレイヤーがロックオン・照射しないように設定
            NotLockableOnList.Add(creater.GameObject);
        }

        private void OnDestroy()
        {
            if (creater == null) return;

            //デバッグ用
            Debug.Log("ジャミングボット破壊");
        }

        public void Damage(float power)
        {
            float p = Useful.Floor(power, 1);   //小数点第2以下切り捨て
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
                Destroy(gameObject);
            }

            //デバッグ用
            Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + HP);
        }
    }
}