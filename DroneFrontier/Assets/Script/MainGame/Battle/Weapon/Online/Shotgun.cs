﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Online
{
    public class Shotgun : BaseWeapon
    {
        //ショットガンのパラメータ
        [SerializeField] Bullet bullet = null;
        [SerializeField, Tooltip("拡散力")] float angle = 10.0f;
        [SerializeField, Tooltip("拡散力のランダム値")] float angleDiff = 3.0f;
        AudioSource audioSource = null;

        //弾丸のパラメータ
        [SerializeField, Tooltip("1秒間に進む距離")] float speedPerSecond = 10.0f;
        [SerializeField, Tooltip("射程")] float destroyTime = 0.3f;
        float trackingPower = 0;
        [SerializeField, Tooltip("1秒間に発射する弾数")] float shotPerSecond = 2.0f;

        [SerializeField, Tooltip("リキャスト時間")] float _recast = 2f;
        [SerializeField, Tooltip("ストック可能な弾数")] int _maxBullets = 5;
        [SerializeField, Tooltip("威力")] float _power = 8f;

        //所持弾数のUI用
        const float UI_POS_DIFF_X = 1.2f;
        const float UI_POS_Y = 175f;
        [SerializeField] Canvas UIParentCanvas = null;
        [SerializeField] Image bulletUIBack = null;
        [SerializeField] Image bulletUIFront = null;
        Image[] UIs;


        public override void OnStartClient()
        {
            base.OnStartClient();
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.SHOTGUN);
            audioSource.volume = SoundManager.BaseSEVolume;
        }

        protected override void Start()
        {
            Recast = _recast;
            ShotInterval = 1.0f / shotPerSecond;
            ShotTimeCount = ShotInterval;
            MaxBullets = _maxBullets;
            BulletsRemain = MaxBullets;
            BulletPower = _power;
        }

        protected override void Update()
        {
            //リキャストと発射間隔のカウント
            base.Update();
        }

        public override void Init()
        {
            //所持弾数のUI作成
            UIs = new Image[_maxBullets];
            for (int i = 0; i < _maxBullets; i++)
            {
                //bulletUIBackの生成
                RectTransform back = Instantiate(bulletUIBack).GetComponent<RectTransform>();
                back.SetParent(UIParentCanvas.transform);
                back.anchoredPosition3D = new Vector3((back.sizeDelta.x * i * UI_POS_DIFF_X) + back.sizeDelta.x, UI_POS_Y, 0);
                back.localRotation = Quaternion.identity;

                //bulletUIFrontの生成
                RectTransform front = Instantiate(bulletUIFront).GetComponent<RectTransform>();
                front.SetParent(UIParentCanvas.transform);
                front.anchoredPosition3D = new Vector3((front.sizeDelta.x * i * UI_POS_DIFF_X) + front.sizeDelta.x, UI_POS_Y, 0);
                front.localRotation = Quaternion.identity;

                //配列に追加
                UIs[i] = front.GetComponent<Image>();
                UIs[i].fillAmount = 1f;
            }
        }

        public override void UpdateMe()
        {
            //最大弾数持っているなら処理しない
            if (BulletsRemain >= MaxBullets) return;

            //リキャスト時間経過したら弾数を1個補充
            if (RecastTimeCount >= Recast)
            {
                UIs[BulletsRemain].fillAmount = 1f;
                BulletsRemain++;        //弾数を回復
                RecastTimeCount = 0;    //リキャストのカウントをリセット


                //デバッグ用
                Debug.Log("ショットガンの弾丸が1回分補充されました");
            }
            else
            {
                UIs[BulletsRemain].fillAmount = RecastTimeCount / Recast;
            }
        }

        public override void ResetWeapon()
        {
            RecastTimeCount = 0;
            ShotTimeCount = ShotInterval;
            BulletsRemain = MaxBullets;

            //弾数UIのリセット
            for (int i = 0; i < UIs.Length; i++)
            {
                UIs[i].fillAmount = 1f;
            }
        }

        public override void Shot(GameObject target = null)
        {
            //前回発射して発射間隔分の時間が経過していなかったら撃たない
            if (ShotTimeCount < ShotInterval) return;

            //残り弾数が0だったら撃たない
            if (BulletsRemain <= 0) return;

            //弾を散らす
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    CmdCreateBullet(shotPos.position, shotPos.rotation, angle * i, angle * j, target);
                }
            }

            //SE再生
            CmdCallPlaySE();

            //所持弾丸のUIを灰色に変える
            for (int i = BulletsRemain - 1; i < MaxBullets; i++)
            {
                UIs[i].fillAmount = 0;
            }

            //残り弾丸がMAXで撃つと一瞬で弾丸が1個回復するので
            //残り弾丸がMAXで撃った場合のみリキャストを0にする
            if (BulletsRemain == MaxBullets)
            {
                RecastTimeCount = 0;
            }
            BulletsRemain--;    //残り弾数を減らす
            ShotTimeCount = 0;  //発射間隔のカウントをリセット


            //デバッグ用
            Debug.Log("残り弾数: " + BulletsRemain);
        }

        Bullet CreateBullet(Vector3 pos, Quaternion rotation, float angleX, float angleY, GameObject target)
        {
            Bullet b = Instantiate(bullet, pos, rotation);    //弾丸の複製

            //弾丸のパラメータ設定
            b.Shooter = shooter;    //撃ったプレイヤーを登録
            b.Target = target;      //ロックオン中の敵
            b.SpeedPerSecond = speedPerSecond;  //スピード
            b.DestroyTime = destroyTime;        //射程
            b.TrackingPower = trackingPower;    //誘導力
            b.Power = BulletPower;              //威力

            //弾丸の進む方向を変えて散らす処理
            Transform t = b.transform;  //キャッシュ
            float rotateX = angleX + Random.Range(angleDiff * -1, angleDiff);  //左右の角度
            float rotateY = angleY + Random.Range(angleDiff * -1, angleDiff);   //上下の角度
            t.RotateAround(t.position, t.right, rotateY);
            t.RotateAround(t.position, t.up, rotateX);

            return b;
        }

        [Command]
        void CmdCreateBullet(Vector3 pos, Quaternion rotation, float angleX, float angleY, GameObject target)
        {
            Bullet b = CreateBullet(pos, rotation, angleX, angleY, target);
            NetworkServer.Spawn(b.gameObject, connectionToClient);
        }

        [Command]
        void CmdCallPlaySE()
        {
            RpcPlaySE();
        }

        [ClientRpc]
        void RpcPlaySE()
        {
            audioSource.Play();
        }
    }
}