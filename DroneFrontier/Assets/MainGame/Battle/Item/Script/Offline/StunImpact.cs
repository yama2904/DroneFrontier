using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class StunImpact : MonoBehaviour
    {
        [HideInInspector] public GameObject thrower = null;
        [SerializeField, Tooltip("スタン状態の時間")] float stunTime = 9.0f;
        float destroyTime = 0.5f;


        void Start()
        {
            //爆発した直後に当たり判定を消す
            Invoke(nameof(FalseEnabledCollider), 0.05f);

            Destroy(gameObject, destroyTime);
        }

        void FalseEnabledCollider()
        {
            GetComponent<SphereCollider>().enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            //投げたプレイヤーなら当たり判定から除外
            if (ReferenceEquals(other.gameObject, thrower)) return;
            if (!other.CompareTag(TagNameManager.PLAYER)) return;   //プレイヤーのみ対象

            other.GetComponent<DroneStatusAction>().SetStun(stunTime);
        }
    }
}