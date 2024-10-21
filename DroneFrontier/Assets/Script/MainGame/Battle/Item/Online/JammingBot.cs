using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class JammingBot : NetworkBehaviour, ILockableOn
    {
        /// <summary>
        /// ロックオン可能であるか
        /// </summary>
        public bool IsLockableOn { get; } = true;

        /// <summary>
        /// ロックオン不可にするオブジェクト
        /// </summary>
        public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

        [SyncVar] float HP = 30.0f;
        [SyncVar, HideInInspector] public GameObject creater = null;


        public override void OnStartClient()
        {
            base.OnStartClient();
            Transform t = transform;  //キャッシュ

            //ボットの向きを変える
            Vector3 angle = t.localEulerAngles;
            angle.y += creater.transform.localEulerAngles.y;
            t.localEulerAngles = angle;

            // 生成した自分のジャミングボットをプレイヤーがロックオン・照射しないように設定
            if (creater.CompareTag(TagNameConst.PLAYER))
            {
                NotLockableOnList.Add(creater);
            }
        }

        private void OnDestroy()
        {
            if (creater == null) return;

            //デバッグ用
            Debug.Log("ジャミングボット破壊");
        }

        [Command(ignoreAuthority = true)]
        public void CmdDamage(float power)
        {
            float p = Useful.Floor(power, 1);   //小数点第2以下切り捨て
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}