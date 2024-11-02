using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class MissileBullet : Bullet
    {
        public GameObject Source { get; private set; } = null;

        [SerializeField] Explosion explosion = null;
        bool isShot = false;
        AudioSource audioSource = null;

        //キャッシュ用
        Transform cacheTransform = null;


        protected override void Awake()
        {
            base.Awake();
            cacheTransform = GetComponent<Rigidbody>().transform;
        }

        void Start()
        {
            cacheTransform.localRotation = Quaternion.Euler(90, 0, 0);

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.MISSILE);
        }

        protected override void FixedUpdate()
        {
            if (!isShot) return;

            //90度傾けたままだと誘導がバグるので一旦直す
            cacheTransform.Rotate(new Vector3(-90, 0, 0));
            base.FixedUpdate();
            cacheTransform.Rotate(new Vector3(90, 0, 0));
        }


        public override void Init(IBattleDrone drone, float power, float trackingPower, float speed, float destroyTime, GameObject target = null)
        {
            base.Init(drone, power, trackingPower, speed, destroyTime, target);
        }

        public void Shot(GameObject target)
        {
            //親子付け解除
            transform.parent = null;

            //SE再生
            audioSource.volume = SoundManager.SEVolume;
            audioSource.Play();

            Invoke(nameof(DestroyMe), destroyTime);
            this.target = target;
            isShot = true;
        }


        void DestroyMe()
        {
            Explosion e = Instantiate(explosion, cacheTransform.position, Quaternion.identity);
            e.shooter = shooter;
            Destroy(gameObject);
        }


        void OnTriggerEnter(Collider other)
        {
            if (!isShot) return;

            //当たり判定を行わないオブジェクトは処理しない
            if (other.CompareTag(TagNameConst.BULLET)) return;
            if (other.CompareTag(TagNameConst.ITEM)) return;
            if (other.CompareTag(TagNameConst.GIMMICK)) return;
            if (other.CompareTag(TagNameConst.JAMMING)) return;
            if (other.CompareTag(TagNameConst.NOT_COLLISION)) return;

            // ダメージ可能インターフェースが実装されている場合はダメージを与える
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.Damage(shooter.GameObject, Power);
            }

            DestroyMe();
        }
    }
}