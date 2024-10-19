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
            if (other.CompareTag(TagNameManager.BULLET)) return;
            if (other.CompareTag(TagNameManager.ITEM)) return;
            if (other.CompareTag(TagNameManager.GIMMICK)) return;
            if (other.CompareTag(TagNameManager.JAMMING)) return;

            //プレイヤーの当たり判定
            if (other.CompareTag(TagNameManager.PLAYER) || other.CompareTag(TagNameManager.CPU))
            {
                //撃った本人なら処理しない
                if (other.GetComponent<IBattleDrone>() == shooter) return;

                //ダメージ処理
                other.GetComponent<DroneDamageComponent>().Damage(shooter.GameObject, Power);

                // ToDo:CPU側に処理させる
                //if (other.CompareTag(TagNameManager.CPU))
                //{
                //    other.GetComponent<CPU.BattleDrone>().StartRotate(shooter.transform);
                //}
            }
            else if (other.CompareTag(TagNameManager.JAMMING_BOT))
            {
                //キャッシュ用
                JammingBot jb = other.GetComponent<JammingBot>();

                //撃った人が放ったジャミングボットなら処理しない
                if (jb.creater == shooter) return;

                //ダメージ処理
                jb.Damage(Power);
            }
            DestroyMe();
        }
    }
}