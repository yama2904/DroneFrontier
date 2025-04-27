using Common;
using UnityEngine;

namespace Race.Gimmick
{
    public class DroneGuard : MonoBehaviour
    {
        [Header("移動方向")]
        [SerializeField, Tooltip("X軸に移動")] bool _dirX = false;
        [SerializeField, Tooltip("Y軸に移動")] bool _dirY = false;
        [SerializeField, Tooltip("Z軸に移動")] bool _dirZ = false;

        [Header("移動速度")]
        [SerializeField] float _speed = 60f;

        [Header("移動距離")]
        [SerializeField] float _range = 60f;

        [Header("反発力")]
        [SerializeField] float _power = 800f;

        /// <summary>
        /// 初期座標
        /// </summary>
        Vector3 _initPos;

        // コンポーネントキャッシュ
        private Transform _transform = null;

        private void Awake()
        {
            _transform = transform;
            _initPos = _transform.position;
        }

        private void FixedUpdate()
        {
            if (_dirX)
            {
                _transform.position = new Vector3(_initPos.x + Mathf.PingPong(Time.time * _speed, _range), _initPos.y, _initPos.z);
            }
            if (_dirY)
            {
                _transform.position = new Vector3(_initPos.x, _initPos.y + Mathf.PingPong(Time.time * _speed, _range), _initPos.z);
            }
            if (_dirZ)
            {
                _transform.position = new Vector3(_initPos.x, _initPos.y, _initPos.z + Mathf.PingPong(Time.time * _speed, _range));
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag(TagNameConst.PLAYER))
            {
                collision.gameObject.GetComponent<Rigidbody>().AddForce(collision.transform.forward * _power * -1, ForceMode.Impulse);
            }
        }
    }
}