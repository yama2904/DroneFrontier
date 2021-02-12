using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class MissileBullet : Bullet
    {
        [SerializeField] Explosion explosion = null;
        [SyncVar, HideInInspector] public uint parentNetId = 0;
        [SyncVar] bool isShot = false;
        AudioSource audioSource = null;


        public override void OnStartClient()
        {
            base.OnStartClient();
            cacheTransform = GetComponent<Rigidbody>().transform;
            GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
            cacheTransform.SetParent(parent.transform);
            cacheTransform.localPosition = new Vector3(0, 0, 0);
            cacheTransform.localRotation = Quaternion.Euler(90, 0, 0);

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.MISSILE);
        }

        void Start() { }

        [ServerCallback]
        protected override void FixedUpdate()
        {
            if (!isShot) return;

            //90度傾けたままだと誘導がバグるので一旦直す
            cacheTransform.Rotate(new Vector3(-90, 0, 0));
            base.FixedUpdate();
            cacheTransform.Rotate(new Vector3(90, 0, 0));
        }


        public override void Init(uint shooterNetId, float power, float trackingPower, float speed, float destroyTime, GameObject target = null)
        {
            base.Init(shooterNetId, power, trackingPower, speed, destroyTime, target);
        }


        [ServerCallback]
        protected override void OnTriggerEnter(Collider other)
        {
            if (!isShot) return;

            //当たり判定を行わないオブジェクトだったら処理をしない
            if (other.CompareTag(TagNameManager.BULLET)) return;
            if (other.CompareTag(TagNameManager.ITEM)) return;
            if (other.CompareTag(TagNameManager.GIMMICK)) return;
            if (other.CompareTag(TagNameManager.JAMMING)) return;
            if (other.CompareTag(TagNameManager.TOWER)) return;

            if (other.CompareTag(TagNameManager.PLAYER))
            {
                //キャッシュ用
                DroneDamageAction player = other.GetComponent<DroneDamageAction>();
                if (player.netId == shooter) return;  //撃った本人なら処理しない
                player.CmdDamage(power);
            }
            else if (other.CompareTag(TagNameManager.JAMMING_BOT))
            {
                //キャッシュ用
                JammingBot jb = other.GetComponent<JammingBot>();

                //撃った人が放ったジャミングボットなら処理しない
                if (jb.creater.GetComponent<BattleDrone>().netId == shooter) return;

                //ジャミングボットにダメージ
                jb.CmdDamage(power);
            }
            DestroyMe();
        }

        [Server]
        void DestroyMe()
        {
            CreateExplosion();
            NetworkServer.Destroy(gameObject);
        }

        #region CreateExplosion


        [Server]
        void CreateExplosion()
        {
            Explosion e = Instantiate(explosion, cacheTransform.position, Quaternion.identity);
            e.Shooter = shooter;
            NetworkServer.Spawn(e.gameObject);
        }

        #endregion

        [Server]
        public void Shot(GameObject target)
        {
            RpcParentNull();
            RpcPlaySE();

            Invoke(nameof(DestroyMe), destroyTime);
            base.target = target;
            isShot = true;
        }

        [ClientRpc]
        void RpcPlaySE()
        {
            audioSource.volume = SoundManager.BaseSEVolume;
            audioSource.Play();
        }

        [ClientRpc]
        void RpcParentNull()
        {
            transform.parent = null;
        }
    }
}