using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Offline
{
    public class MissileBullet : MonoBehaviour, IBullet
    {
        public GameObject Shooter { get; private set; } = null;

        /// <summary>
        /// 着弾時間（秒）
        /// </summary>
        public float ExplosionSec { get; set; } = 2f;

        [SerializeField, Tooltip("爆発オブジェクト")]
        private Explosion _explosion = null;

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

        /// <summary>
        /// キャンセルトークン発行クラス
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        // コンポーネントキャッシュ用
        private Transform _transform = null;
        private Transform _targetTransform = null;
        private AudioSource _audioSource = null;

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

            // SE再生
            _audioSource.Play();

            // 爆発タイマー設定
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(ExplosionSec), cancellationToken: _cancel.Token, ignoreTimeScale: true);
                Explosion();
            });
        }

        private void Awake()
        {
            // コンポーネント取得
            _transform = GetComponent<Rigidbody>().transform;
            _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.MISSILE);
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
                    // 弾丸から追従対象までの角度
                    float angle = Vector3.Angle(_transform.forward, diff);
                    if (angle > _trackingPower)
                    {
                        // 追従力以上の角度がある場合は修正
                        angle = _trackingPower;
                    }

                    // 追従方向を計算
                    Vector3 axis = Vector3.Cross(_transform.forward, diff);
                    int dirX = axis.y >= 0 ? 1 : -1;
                    int dirY = axis.x >= 0 ? 1 : -1;

                    // 左右の回転
                    _transform.RotateAround(_transform.position, Vector3.up, angle * dirX);

                    // 上下の回転
                    _transform.RotateAround(_transform.position, Vector3.right, angle * dirY);
                }
            }

            // 移動
            _transform.position += _transform.forward * _speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            //当たり判定を行わないオブジェクトは処理しない
            if (other.CompareTag(TagNameConst.BULLET)) return;
            if (other.CompareTag(TagNameConst.ITEM)) return;
            if (other.CompareTag(TagNameConst.GIMMICK)) return;
            if (other.CompareTag(TagNameConst.JAMMING)) return;
            if (other.CompareTag(TagNameConst.NOT_COLLISION)) return;

            // ダメージ可能インターフェースが実装されている場合はダメージを与える
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.Damage(Shooter, _damage);
            }

            Explosion();
        }

        /// <summary>
        /// 爆発
        /// </summary>
        private void Explosion()
        {
            // 爆発オブジェクト生成
            Explosion e = Instantiate(_explosion, _transform.position, Quaternion.identity);
            e.Shooter = Shooter;

            // 爆発タイマー停止
            _cancel.Cancel();

            // ミサイル削除
            Destroy(gameObject);
        }
    }
}