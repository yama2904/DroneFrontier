using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class Gatling : BaseWeapon
    {
        [SerializeField] Bullet bullet = null; //弾のオブジェクト
        AudioSource audioSource = null;

        //弾丸のパラメータ
        [SerializeField, Tooltip("1秒間に進む距離")] float speed = 500f;
        [SerializeField, Tooltip("射程")] float destroyTime = 1.0f;
        [SerializeField, Tooltip("誘導力")] float trackingPower = 3f;
        [SerializeField, Tooltip("1秒間に発射する弾数")] float shotPerSecond = 10f;
        [SerializeField, Tooltip("威力")] float power = 1f;
        float shotInterval = 0;  //発射間隔
        float shotTimeCount = 0; //時間計測用


        public override void OnStartClient()
        {
            base.OnStartClient();
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.GATLING);
        }

        void Start()
        {
            //パラメータの初期化
            shotInterval = 1.0f / shotPerSecond;
            shotTimeCount = shotInterval;
        }

        void Update()
        {
            shotTimeCount += Time.deltaTime;
            if (shotTimeCount > shotInterval)
            {
                shotTimeCount = shotInterval;
            }
        }

        public override void Init() { }
        public override void UpdateMe() { }

        public override void Shot(GameObject target = null)
        {
            //前回発射して発射間隔分の時間が経過していなかったら撃たない
            if (shotTimeCount < shotInterval) return;

            //弾丸生成
            CmdCreateBullet(shotPos.position, transform.rotation, target);

            shotTimeCount = 0;  //発射間隔のカウントをリセット
        }

        Bullet CreateBullet(Vector3 pos, Quaternion rotation, GameObject target)
        {
            Bullet b = Instantiate(bullet, pos, rotation);    //弾丸の複製

            //弾丸のパラメータ設定
            b.Init(shooter.netId, power, trackingPower, speed, destroyTime, target);

            return b;
        }

        [Command]
        void CmdCreateBullet(Vector3 pos, Quaternion rotation, GameObject target)
        {
            Bullet b = CreateBullet(pos, rotation, target);
            NetworkServer.Spawn(b.gameObject, connectionToClient);
            RpcPlaySE();
        }

        [ClientRpc]
        void RpcPlaySE()
        {
            audioSource.volume = SoundManager.SEVolume;
            audioSource.Play();
        }
    }
}