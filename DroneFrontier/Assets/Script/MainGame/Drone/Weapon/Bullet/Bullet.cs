using UnityEngine;

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
                // �e�ۂ���Ǐ]�Ώۂ܂ł̊p�x
                float angle = Vector3.Angle(_transform.forward, diff);
                if (angle > _trackingPower)
                {
                    // �Ǐ]�͈ȏ�̊p�x������ꍇ�͏C��
                    angle = _trackingPower;
                }

                // �Ǐ]�������v�Z
                Vector3 axis = Vector3.Cross(_transform.forward, diff);
                int dirX = axis.y >= 0 ? 1 : -1;
                int dirY = axis.x >= 0 ? 1 : -1;

                // ���E�̉�]
                _transform.RotateAround(_transform.position, Vector3.up, angle * dirX);

                // �㉺�̉�]
                _transform.RotateAround(_transform.position, Vector3.right, angle * dirY);
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
        if (other.CompareTag(TagNameConst.JAMMING)) return;
        if (other.CompareTag(TagNameConst.NOT_COLLISION)) return;

        // �_���[�W�\�C���^�[�t�F�[�X����������Ă���ꍇ�̓_���[�W��^����
        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.Damage(Shooter, _damage);
        }

        Destroy(gameObject);
    }
}
