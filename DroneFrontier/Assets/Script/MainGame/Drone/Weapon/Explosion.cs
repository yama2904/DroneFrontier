using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class Explosion : MonoBehaviour
    {
        /// <summary>
        /// 爆発元オブジェクト
        /// </summary>
        public GameObject Shooter { get; set; } = null;

        /// <summary>
        /// 威力
        /// </summary>
        public float Damage
        {
            get { return _damage; }
            set { _damage = value; }
        }

        /// <summary>
        /// 爆発範囲
        /// </summary>
        public float ExplosionRadius
        {
            get { return _explosionRadius; }
            set { _explosionRadius = value; }
        }

        /// <summary>
        /// 消滅時間
        /// </summary>
        private const float DESTROY_TIME = 3f;

        [SerializeField, Tooltip("威力")]
        private float _damage = 20;

        [SerializeField, Tooltip("爆発範囲を半径で指定")]
        private float _explosionRadius = 200;

        [SerializeField, Tooltip("爆発ダメージの最大減衰率")]
        private float _maxPowerDownRate = 0.8f;

        [SerializeField, Tooltip("威力が減衰し始める範囲（中心から見た半径で指定）")]
        private float _damageDownRadius = 50f;

        List<GameObject> _hitedList = new List<GameObject>();    //ダメージを与えたオブジェクトを全て格納する

        // コンポーネントキャッシュ
        private Transform _transform = null;

        private void Start()
        {
            // コンポーネントキャッシュ
            _transform = transform;

            // 爆発範囲を各オブジェクトのサイズに適用
            SetChildrenScale(_transform, _explosionRadius);

            // 爆発範囲を当たり判定に適用
            GetComponent<SphereCollider>().radius = _explosionRadius;

            // 爆発した直後に当たり判定を消す
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(200), ignoreTimeScale: true);
                GetComponent<SphereCollider>().enabled = false;
            });

            // 爆発元とは当たり判定を行わない
            if (!Useful.IsNullOrDestroyed(Shooter) && Shooter.TryGetComponent(out Collider collider))
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), collider);
            }

            // 爆発SE再生
            AudioSource audio = GetComponent<AudioSource>();
            audio.clip = SoundManager.GetAudioClip(SoundManager.SE.EXPLOSION_MISSILE);
            audio.volume = SoundManager.SEVolume;
            audio.time = 0.2f;
            audio.Play();

            // 一定時間後に消滅
            Destroy(gameObject, DESTROY_TIME);
        }

        private void OnTriggerEnter(Collider other)
        {
            // ダメージ可能インターフェースが実装されていない場合は除外
            IDamageable damageable;
            if (!other.TryGetComponent(out damageable))
            {
                return;
            }

            // 既にヒット済のオブジェクトはスルー
            foreach (GameObject o in _hitedList)
            {
                if (other.gameObject == o) return;
            }

            damageable.Damage(Shooter, CalcDamage(other.transform.position));
            _hitedList.Add(other.gameObject);
        }

        /// <summary>
        /// 指定したTransformの全ての子孫に対してlocalScaleを設定
        /// </summary>
        /// <param name="parent">localScaleを設定する親</param>
        /// <param name="scale">localScale値</param>
        private void SetChildrenScale(Transform parent, float scale)
        {
            foreach (Transform child in parent)
            {
                child.localScale = new Vector3(scale, scale, scale);

                // 孫オブジェクトにも適用
                SetChildrenScale(child, scale);
            }
        }

        /// <summary>
        /// 相手との距離を基に与えるダメージを計算
        /// </summary>
        /// <param name="hitPos">ダメージを与える相手の座標</param>
        /// <returns>与えるダメージ量</returns>
        private float CalcDamage(Vector3 hitPos)
        {
            // 爆発の中心から相手までの距離を計算
            float distance = Vector3.Distance(_transform.position, hitPos);

            // 威力が減衰し始める範囲から相手までの距離へ補正
            distance -= _damageDownRadius;

            // 減衰範囲内にない場合はダメージをそのまま返す
            if (distance <= 0)
            {
                return _damage;
            }

            // 距離に応じた減衰率を適用する
            float downRate = 1 - distance / (_explosionRadius - _damageDownRadius) * _maxPowerDownRate;
            return _damage * downRate;
        }
    }
}