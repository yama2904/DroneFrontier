using Common;
using Drone.Battle;
using UnityEngine;

namespace Battle.Weapon.Bullet
{
    public class Bullet : MonoBehaviour, IBullet
    {
        public GameObject Shooter { get; private set; } = null;

        /// <summary>
        /// ダメージ量
        /// </summary>
        private float _damage = 0;

        /// <summary>
        /// 弾速
        /// </summary>
        private float _speed = 0;

        /// <summary>
        /// 追従力
        /// </summary>
        private float _trackingPower = 0;

        /// <summary>
        /// 追従対象
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

            // 発射元とは当たり判定を行わない
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
                // 弾丸から追従対象までのベクトル計算
                Vector3 diff = _targetTransform.position - _transform.position;

                // 正面に対象が存在する場合のみ追従を行う
                if (Vector3.Dot(diff, _transform.forward) > 0)
                {
                    // 弾丸のローカル空間でのターゲット方向
                    Vector3 localDiff = _transform.InverseTransformDirection(diff.normalized);

                    // ヨー（左右）とピッチ（上下）の角度調整
                    float yaw = Mathf.Atan2(localDiff.x, localDiff.z) * Mathf.Rad2Deg;
                    float pitch = -Mathf.Atan2(localDiff.y, localDiff.z) * Mathf.Rad2Deg;

                    // 追従力で制限
                    yaw = Mathf.Clamp(yaw, -_trackingPower, _trackingPower);
                    pitch = Mathf.Clamp(pitch, -_trackingPower, _trackingPower);

                    // ローカル軸で回転
                    _transform.Rotate(Vector3.up, yaw, Space.Self);      // 左右
                    _transform.Rotate(Vector3.right, pitch, Space.Self); // 上下
                }
            }

            // 移動
            _transform.position += _transform.forward * _speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 当たり判定を行わないオブジェクトは処理しない
            if (other.CompareTag(TagNameConst.BULLET)) return;
            if (other.CompareTag(TagNameConst.ITEM)) return;
            if (other.CompareTag(TagNameConst.GIMMICK)) return;
            if (other.CompareTag(TagNameConst.JAMMING_AREA)) return;
            if (other.CompareTag(TagNameConst.NOT_COLLISION)) return;

            // ダメージ可能インターフェースが実装されている場合はダメージを与える
            if (other.TryGetComponent(out IDamageable damageable))
            {
                if (damageable.Owner == Shooter) return;
                damageable.Damage(Shooter, _damage);
            }

            Destroy(gameObject);
        }
    }
}