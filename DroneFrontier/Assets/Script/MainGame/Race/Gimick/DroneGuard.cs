using Common;
using UnityEngine;

namespace Race.Gimmick
{
    public class DroneGuard : MonoBehaviour
    {
        [Header("�ړ�����")]
        [SerializeField, Tooltip("X���Ɉړ�")] bool _dirX = false;
        [SerializeField, Tooltip("Y���Ɉړ�")] bool _dirY = false;
        [SerializeField, Tooltip("Z���Ɉړ�")] bool _dirZ = false;

        [Header("�ړ����x")]
        [SerializeField] float _speed = 60f;

        [Header("�ړ�����")]
        [SerializeField] float _range = 60f;

        [Header("������")]
        [SerializeField] float _power = 800f;

        /// <summary>
        /// �������W
        /// </summary>
        Vector3 _initPos;

        // �R���|�[�l���g�L���b�V��
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