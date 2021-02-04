using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class MissileBullet : Bullet
    {
        [SerializeField] Explosion explosion = null;
        bool isShot = false;
        AudioSource audioSource = null;


        void Start()
        {
            cacheTransform = GetComponent<Rigidbody>().transform;
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
        protected override void OnTriggerEnter(Collider other)
        {
            if (!isShot) return;

            //当たり判定を行わないオブジェクトだったら処理をしない
            if (ReferenceEquals(other.gameObject, Shooter)) return;
            if (other.CompareTag(TagNameManager.BULLET)) return;
            if (other.CompareTag(TagNameManager.ITEM)) return;
            if (other.CompareTag(TagNameManager.GIMMICK)) return;
            if (other.CompareTag(TagNameManager.JAMMING)) return;

            if (other.CompareTag(TagNameManager.PLAYER))
            {
                other.GetComponent<BattleDrone>().Damage(Power);
            }
            else if (other.CompareTag(TagNameManager.JAMMING_BOT))
            {
                JammingBot jb = other.GetComponent<JammingBot>();
                if (jb.creater == Shooter)
                {
                    return;
                }
                jb.Damage(Power);
            }
            DestroyMe();
        }

        void DestroyMe()
        {
            CreateExplosion();
            Destroy(gameObject);
        }

        #region CreateExplosion

        private Explosion CreateExplosion()
        {
            Explosion e = Instantiate(explosion, cacheTransform.position, Quaternion.identity);
            e.Shooter = Shooter;
            return e;
        }

        #endregion

        public void Shot(GameObject target)
        {
            //親子付け解除
            transform.parent = null;

            //SE再生
            audioSource.volume = SoundManager.BaseSEVolume;
            audioSource.Play();

            Invoke(nameof(DestroyMe), DestroyTime);
            Target = target;
            isShot = true;
        }
    }
}