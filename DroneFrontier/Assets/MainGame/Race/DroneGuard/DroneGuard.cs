using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class DroneGuard : NetworkBehaviour
    {
        [Header("移動方向")]
        [SerializeField, Tooltip("X軸に移動")] bool dirX = false;
        [SerializeField, Tooltip("Y軸に移動")] bool dirY = false;
        [SerializeField, Tooltip("Z軸に移動")] bool dirZ = false;

        [Header("移動速度")]
        [SerializeField] float speed = 10f;

        [Header("移動距離")]
        [SerializeField] float range = 7.5f;

        [Header("反発力")]
        [SerializeField] float power = 200;

        Transform cacheTransform = null;
        Vector3 initPos;

        void Start()
        {
            cacheTransform = transform;
            initPos = cacheTransform.position;
        }

        void Update()
        {
            if (dirX)
            {
                cacheTransform.position = new Vector3(initPos.x + Mathf.PingPong(Time.time * speed, range), initPos.y, initPos.z);
            }
            if (dirY)
            {
                cacheTransform.position = new Vector3(initPos.x, initPos.y + Mathf.PingPong(Time.time * speed, range), initPos.z);
            }
            if (dirZ)
            {
                cacheTransform.position = new Vector3(initPos.x, initPos.y, initPos.z + Mathf.PingPong(Time.time * speed, range));
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag(TagNameManager.PLAYER))
            {
                RaceDrone p = collision.gameObject.GetComponent<RaceDrone>();
                if (!p.isLocalPlayer) return;
                p.GetComponent<Rigidbody>().AddForce(p.transform.forward * power * -1, ForceMode.Impulse);
            }
        }
    }
}