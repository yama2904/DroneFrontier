﻿using System.Collections;
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
        [SerializeField, Tooltip("1秒間に進む距離")] float speedPerSecond = 10.0f;
        [SerializeField, Tooltip("射程")] float destroyTime = 1.0f;
        [SerializeField, Tooltip("誘導力")] float trackingPower = 1.2f;
        [SerializeField, Tooltip("1秒間に発射する弾数")] float shotPerSecond = 5.0f;
        [SerializeField, Tooltip("威力")] float _power = 3f;


        public override void OnStartClient()
        {
            base.OnStartClient();
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.GATLING);
        }

        protected override void Start()
        {
            Recast = 0;
            ShotInterval = 1.0f / shotPerSecond;
            ShotTimeCount = ShotInterval;
            MaxBullets = 10;
            BulletsRemain = MaxBullets;
            BulletPower = _power;
        }

        protected override void Update()
        {
            //リキャストと発射間隔のカウント
            base.Update();

            //リキャスト時間経過したら弾数を1個補充
            if (RecastTimeCount >= Recast)
            {
                //残り弾数が最大弾数に達していなかったら補充
                if (BulletsRemain < MaxBullets)
                {
                    BulletsRemain++;        //弾数を回復
                    RecastTimeCount = 0;    //リキャストのカウントをリセット
                }
            }
        }

        public override void Init() { }
        public override void UpdateMe() { }
        public override void ResetWeapon() { }

        public override void Shot(GameObject target = null)
        {
            //前回発射して発射間隔分の時間が経過していなかったら撃たない
            if (ShotTimeCount < ShotInterval) return;

            //残り弾数が0だったら撃たない
            if (BulletsRemain <= 0) return;

            CmdCreateBullet(shotPos.position, transform.rotation, target);


            //残り弾丸がMAXで撃つと一瞬で弾丸が1個回復するので
            //残り弾丸がMAXで撃った場合のみリキャストを0にする
            if (BulletsRemain == MaxBullets)
            {
                RecastTimeCount = 0;
            }
            BulletsRemain--;    //残り弾数を減らす
            ShotTimeCount = 0;  //発射間隔のカウントをリセット
        }

        Bullet CreateBullet(Vector3 pos, Quaternion rotation, GameObject target)
        {
            Bullet b = Instantiate(bullet, pos, rotation);    //弾丸の複製

            //弾丸のパラメータ設定
            b.Shooter = shooter;    //撃ったプレイヤーを登録
            b.Target = target;      //ロックオン中の敵
            b.SpeedPerSecond = speedPerSecond;  //スピード
            b.DestroyTime = destroyTime;        //射程
            b.TrackingPower = trackingPower;    //誘導力
            b.Power = BulletPower;              //威力

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
            audioSource.volume = SoundManager.BaseSEVolume;
            audioSource.Play();
        }
    }
}