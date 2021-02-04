using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class StunGrenade : MonoBehaviour
    {
        GameObject thrower = null;
        [SerializeField] StunImpact stunImpact = null;
        [SerializeField, Tooltip("投げる角度")] Transform throwRotate = null;

        [SerializeField, Tooltip("投げる速度")] float throwPower = 10.0f;  //投げる速度
        [SerializeField, Tooltip("着弾時間")] float impactTime = 1.0f;   //着弾時間
        [SerializeField, Tooltip("重力")] float gravity = 1f; //重力

        Rigidbody _rigidbody = null;


        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            _rigidbody.AddForce(new Vector3(0, gravity * -1, 0), ForceMode.Acceleration);
        }

        public void ThrowGrenade(GameObject thrower)
        {
            Transform cacheTransform = transform;   //キャッシュ用
            this.thrower = thrower;

            //playerの座標と向きのコピー
            cacheTransform.position = thrower.transform.position;
            cacheTransform.rotation = thrower.transform.rotation;

            //投てき処理
            _rigidbody.AddForce(throwRotate.forward * throwPower, ForceMode.Impulse);
            Invoke(nameof(CreateImpact), impactTime);
        }

        //スタングレネードを爆破させる
        void CreateImpact()
        {
            StunImpact s = Instantiate(stunImpact, transform.position, Quaternion.identity);
            s.thrower = thrower;

            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log(other.gameObject);
            //特定のオブジェクトはすり抜け
            if (ReferenceEquals(other.gameObject, thrower)) return;
            if (other.CompareTag(TagNameManager.ITEM)) return;
            if (other.CompareTag(TagNameManager.GIMMICK)) return;
            if (other.CompareTag(TagNameManager.JAMMING)) return;
            if (other.CompareTag(TagNameManager.BULLET)) return;
            CreateImpact();
        }
    }
}