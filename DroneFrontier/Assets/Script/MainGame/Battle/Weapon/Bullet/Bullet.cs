using Common;
using Drone.Battle;
using UnityEngine;

namespace Battle.Weapon.Bullet
{
    public class Bullet : MonoBehaviour, IBullet
    {
        public GameObject Shooter { get; private set; } = null;

        /// <summary>
        /// �_���[�W��
        /// </summary>
        private float _damage = 0;

        /// <summary>
        /// �e��
        /// </summary>
        private float _speed = 0;

        /// <summary>
        /// �Ǐ]��
        /// </summary>
        private float _trackingPower = 0;

        /// <summary>
        /// �Ǐ]�Ώ�
        /// </summary>
        private GameObject _target = null;

        private Transform _transform = null;
        private Transform _targetTransform = null;

        public void Shot(GameObject shooter, float damage, float speed, float trackingPower = 0, GameObject target = null)
        {
            _damage = damage;
            _speed = speed;
            _trackingPower = trackingPower;
            _target = target;
            _targetTransform = Useful.IsNullOrDestroyed(target) ? null : target.transform;
            Shooter = shooter;

            // ���ˌ��Ƃ͓����蔻����s��Ȃ�
            if (!Useful.IsNullOrDestroyed(shooter) && shooter.TryGetComponent(out Collider collider))
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), collider);
            }
        }

        private void Awake()
        {
            _transform = GetComponent<Rigidbody>().transform;
        }

        private void FixedUpdate()
        {
            if (_target != null)
            {
                // �e�ۂ���Ǐ]�Ώۂ܂ł̃x�N�g���v�Z
                Vector3 diff = _targetTransform.position - _transform.position;

                // ���ʂɑΏۂ����݂���ꍇ�̂ݒǏ]���s��
                if (Vector3.Dot(diff, _transform.forward) > 0)
                {
                    // �e�ۂ̃��[�J����Ԃł̃^�[�Q�b�g����
                    Vector3 localDiff = _transform.InverseTransformDirection(diff.normalized);

                    // ���[�i���E�j�ƃs�b�`�i�㉺�j�̊p�x����
                    float yaw = Mathf.Atan2(localDiff.x, localDiff.z) * Mathf.Rad2Deg;
                    float pitch = -Mathf.Atan2(localDiff.y, localDiff.z) * Mathf.Rad2Deg;

                    // �Ǐ]�͂Ő���
                    yaw = Mathf.Clamp(yaw, -_trackingPower, _trackingPower);
                    pitch = Mathf.Clamp(pitch, -_trackingPower, _trackingPower);

                    // ���[�J�����ŉ�]
                    _transform.Rotate(Vector3.up, yaw, Space.Self);      // ���E
                    _transform.Rotate(Vector3.right, pitch, Space.Self); // �㉺
                }
            }

            // �ړ�
            _transform.position += _transform.forward * _speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // �����蔻����s��Ȃ��I�u�W�F�N�g�͏������Ȃ�
            if (other.CompareTag(TagNameConst.BULLET)) return;
            if (other.CompareTag(TagNameConst.ITEM)) return;
            if (other.CompareTag(TagNameConst.GIMMICK)) return;
            if (other.CompareTag(TagNameConst.JAMMING_AREA)) return;
            if (other.CompareTag(TagNameConst.NOT_COLLISION)) return;

            // �_���[�W�\�C���^�[�t�F�[�X����������Ă���ꍇ�̓_���[�W��^����
            if (other.TryGetComponent(out IDamageable damageable))
            {
                if (damageable.Owner == Shooter) return;
                damageable.Damage(Shooter, _damage);
            }

            Destroy(gameObject);
        }
    }
}