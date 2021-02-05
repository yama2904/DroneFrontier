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

        //キャッシュ用
        Transform cacheTransform = null;


        void Awake()
        {
            explosion.gameObject.SetActive(false);
        }

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

        private void OnDestroy()
        {
            explosion.gameObject.SetActive(true);
            explosion.PlayerID = PlayerID;
        }


        public override void Init(uint id, float power, float trackingPower, float speed, float destroyTime, GameObject target = null)
        {
            base.Init(id, power, trackingPower, speed, destroyTime, target);
        }

        public void Shot(GameObject target)
        {
            //親子付け解除
            transform.parent = null;

            //SE再生
            audioSource.volume = SoundManager.BaseSEVolume;
            audioSource.Play();

            Destroy(gameObject, destroyTime);
            this.target = target;
            isShot = true;
        }
    }
}