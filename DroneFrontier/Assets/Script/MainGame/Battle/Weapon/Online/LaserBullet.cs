﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

namespace Online
{
    public class LaserBullet : NetworkBehaviour
    {
        //パラメータ
        const float LINE_RADIUS = 0.2f;
        BattleDrone shooter = null;  //発射したプレイヤー
        float chargeTime = 0;        //チャージ時間
        float power = 0;             //威力
        float lineRange = 0;         //レーザーの半径
        float shotInterval = 0;      //発射間隔
        float shotTimeCount = 0;     //時間計測用
        bool isLocal = false;

        public bool IsShotBeam { get; private set; } = false;
        bool isStartCharge = false;
        AudioSource audioSource = null;


        //親子付け用
        [SyncVar, HideInInspector] public uint parentNetId = 0;
        [SyncVar, HideInInspector] public Vector3 localPos = new Vector3();
        [SyncVar, HideInInspector] public Quaternion localRot = Quaternion.identity;


        //Charge用変数
        const int MAX_RATE_OVER_TIME = 128;   //チャージのパーティクルのrateOverTime最大値
        [SerializeField] ParticleSystem charge = null;
        ParticleSystem.EmissionModule chargeEmission;
        ParticleSystem.MinMaxCurve chargeMinmaxcurve;
        float rateovertimeAddAmout;    //割り算は重いので先に計算させる用
        bool isCharged;     //チャージし終わったらtrue

        //Start用変数
        [SerializeField] Transform startObjcectTransform = null;
        ParticleSystem[] startChilds;

        //Line用変数
        [SerializeField] ParticleSystem lineParticle = null;
        Transform lineTransform = null;

        //End用変数
        [SerializeField] Transform endObjectTransform = null;
        List<ParticleSystem> endChilds = new List<ParticleSystem>();


        public override void OnStartClient()
        {
            base.OnStartClient();
            GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
            transform.SetParent(parent.transform);
            transform.localPosition = localPos;
            transform.localRotation = localRot;

            //処理の軽量化用キャッシュ
            Transform cacheTransform = transform;
            lineTransform = lineParticle.transform;
        }

        void Update()
        {
            //発射間隔の管理
            shotTimeCount += Time.deltaTime;
            if (shotTimeCount > shotInterval)
            {
                shotTimeCount = shotInterval;
            }
        }


        [ClientRpc]
        public void RpcInit(BattleDrone drone, float power, float size, float chargeTime, float lineRange, float hitPerSecond)
        {
            shooter = drone;
            this.chargeTime = chargeTime;
            this.power = power;
            this.lineRange = lineRange;

            //長さをスケールに合わせる
            this.lineRange *= size;

            shotInterval = 1f / hitPerSecond;
            shotTimeCount = shotInterval;

            //Charge用処理//
            Vector3 cLocalScale = charge.transform.localScale;
            charge.transform.localScale = new Vector3(size * cLocalScale.x, size * cLocalScale.y, size * cLocalScale.z);
            chargeEmission = charge.emission;
            chargeMinmaxcurve = chargeEmission.rateOverTime;
            rateovertimeAddAmout = MAX_RATE_OVER_TIME / chargeTime;  //1秒間で増加するRateOverTime量


            //Start用処理//
            startChilds = new ParticleSystem[startObjcectTransform.childCount];
            for (int i = 0; i < startObjcectTransform.childCount; i++)
            {
                startChilds[i] = startObjcectTransform.GetChild(i).GetComponent<ParticleSystem>();
                Vector3 sLocalScale = startChilds[i].transform.localScale;
                startChilds[i].transform.localScale = new Vector3(size * sLocalScale.x, size * sLocalScale.y, size * sLocalScale.z);
            }
            startObjcectTransform.localRotation = lineTransform.localRotation;  //Midwayと同じ向き


            //End用処理//
            for (int i = 0; i < endObjectTransform.childCount; i++)
            {
                ParticleSystem ps = endObjectTransform.GetChild(i).GetComponent<ParticleSystem>();
                Vector3 eLocalScale = ps.transform.localScale;
                ps.transform.localScale = new Vector3(size * eLocalScale.x, size * eLocalScale.y, size * eLocalScale.z);
                endChilds.Add(ps);
            }
            //初期座標の保存
            endObjectTransform.localRotation = lineTransform.localRotation;   //Midwayと同じ向き

            audioSource = GetComponent<AudioSource>();
            ModifyLaserLength(lineRange);
            StopShot();
        }

        [TargetRpc]
        public void TargetSetIsLocalTrue(NetworkConnection target)
        {
            isLocal = true;
        }

        public void Shot(GameObject target)
        {
            #region Charge

            //チャージ処理
            if (!isCharged)
            {
                //攻撃開始時
                if (!isStartCharge)
                {
                    CmdCallRpcChargePlay(true);
                    isStartCharge = true;
                }

                //徐々にチャージのエフェクトを増す
                CmdCallRpcAddChargeParticle(rateovertimeAddAmout * Time.deltaTime);

                //MAX_RATE_OVER_TIME経ったら発射
                if (chargeEmission.rateOverTime.constant > MAX_RATE_OVER_TIME)
                {
                    //チャージを止める
                    CmdCallRpcChargePlay(false);

                    //レーザーの発射
                    CmdCallRpcLaserPlay();

                    isCharged = true;
                }
            }

            #endregion

            #region Laser

            else
            {
                IsShotBeam = true;

                //前回ヒットして発射間隔分の時間が経過していなかったら当たり判定を行わない
                if (shotTimeCount < shotInterval) return;


                //Y軸の誘導
                if (isServer)
                {
                    RpcRotateBullet(target);
                }
                else
                {
                    CmdCallRpcRotateBullet(target);
                }


                //レーザーの射線上にヒットした全てのオブジェクトを調べる
                var hits = Physics.SphereCastAll(
                            lineTransform.position,    //レーザーの発射座標
                            LINE_RADIUS,               //レーザーの半径
                            lineTransform.forward,     //レーザーの正面
                            lineRange)                 //射程
                            .ToList();  //リスト化  

                hits = FilterTargetRaycast(hits);
                float lineLength = lineRange;   //レーザーの長さ

                //ヒット処理
                if (hits.Count > 0)
                {
                    GetNearestObject(out RaycastHit hit, hits);
                    GameObject o = hit.transform.gameObject;    //名前省略

                    if (o.CompareTag(TagNameManager.PLAYER))
                    {
                        o.GetComponent<DroneDamageAction>().CmdDamage(power);
                    }
                    else if (o.CompareTag(TagNameManager.JAMMING_BOT))
                    {
                        o.GetComponent<JammingBot>().CmdDamage(power);
                    }

                    //ヒットしたオブジェクトの距離とレーザーの長さを合わせる
                    lineLength = hit.distance;

                    //ヒットした場所にEndオブジェクトを移動させる
                    endObjectTransform.position = hit.point;

                    shotTimeCount = 0;  //発射間隔のカウントをリセット
                }
                else
                {
                    //レーザーの末端にEndオブジェクトを移動
                    endObjectTransform.position = lineTransform.position + (lineTransform.forward * lineRange);
                }
                //レーザーの長さに応じてオブジェクトの座標やサイズを変える
                CmdCallModifyLaserLength(lineLength);
            }

            #endregion
        }


        //リストから必要な要素だけ抜き取る
        List<RaycastHit> FilterTargetRaycast(List<RaycastHit> hits)
        {
            //不要な要素を除外する
            return hits.Where(h => !h.transform.CompareTag(TagNameManager.ITEM))    //アイテム除外
                       .Where(h => !h.transform.CompareTag(TagNameManager.BULLET))  //弾丸除外
                       .Where(h => !h.transform.CompareTag(TagNameManager.GIMMICK)) //ギミック除外
                       .Where(h => !h.transform.CompareTag(TagNameManager.JAMMING)) //ジャミング除外
                       .Where(h => !h.transform.CompareTag(TagNameManager.TOWER))   //タワー除外
                       .Where(h =>
                       {
                           //撃った本人は当たり判定から除外
                           if (h.transform.CompareTag(TagNameManager.PLAYER))
                           {
                               return h.transform.GetComponent<BattleDrone>().netId != shooter.netId;
                           }

                           //ジャミングボットを生成したプレイヤーと撃ったプレイヤーが同じなら除外
                           if (h.transform.CompareTag(TagNameManager.JAMMING_BOT))
                           {
                               return !ReferenceEquals(h.transform.GetComponent<JammingBot>().creater, shooter.gameObject);
                           }
                           return true;
                       })
                       .ToList();  //リスト化 
        }

        //リスト内で最も距離が近いRaycastHitを返す
        void GetNearestObject(out RaycastHit hit, List<RaycastHit> hits)
        {
            hit = hits[0];
            float minTargetDistance = float.MaxValue;   //初期化
            foreach (RaycastHit h in hits)
            {
                //距離が最小だったら更新
                if (h.distance < minTargetDistance)
                {
                    minTargetDistance = h.distance;
                    hit = h;
                }
            }
        }


        void ModifyLaserLength(float length)
        {
            //Lineオブジェクト
            Vector3 lineScale = lineTransform.localScale;

            //そのままレーザーを太くすると他プレイヤーから見ると異常に太く見えるので
            //ローカルプレイヤーのみ太くする
            float localScaleY = length;
            if (isLocal)
            {
                localScaleY *= 40;
            }
            lineTransform.localScale = new Vector3(length, localScaleY, 1.0f);
        }

        [Command]
        void CmdCallModifyLaserLength(float length)
        {
            RpcModifyLaserLength(length);
        }

        [ClientRpc]
        void RpcModifyLaserLength(float length)
        {
            ModifyLaserLength(length);
        }


        #region StopShot

        //チャージとレーザーを止める
        public void StopShot()
        {
            CmdCallRpcStopAllParticle();

            //フラグの初期化
            isCharged = false;
            IsShotBeam = false;
            isStartCharge = false;
        }

        [Command(ignoreAuthority = true)]
        void CmdCallRpcStopAllParticle()
        {
            RpcStopAllParticle();
        }

        [ClientRpc]
        void RpcStopAllParticle()
        {
            //角度を戻す
            lineTransform.localRotation = Quaternion.identity;

            charge.Stop();  //Chargeを止める

            //Chargeのパーティクルの発生量の初期化
            chargeMinmaxcurve.constant = 0;
            chargeEmission.rateOverTime = chargeMinmaxcurve;


            //Startを止める
            foreach (ParticleSystem p in startChilds)
            {
                p.Stop();
            }

            //Midwayを止める
            lineParticle.Stop();

            //Endを止める
            foreach (ParticleSystem p in endChilds)
            {
                p.Stop();
            }

            //サウンドを止める
            audioSource.Stop();
            audioSource.loop = false;
        }

        #endregion

        [Command(ignoreAuthority = true)]
        void CmdCallRpcRotateBullet(GameObject target)
        {
            RpcRotateBullet(target);
        }

        [ClientRpc]
        void RpcRotateBullet(GameObject target)
        {
            if (target != null)
            {
                Vector3 diff = target.transform.position - lineTransform.position;
                Quaternion rotation = Quaternion.LookRotation(diff);  //敵の方向


                //カメラの角度からtrackingSpeed(0～1)の速度でロックオンしたオブジェクトの角度に向く
                lineTransform.rotation = Quaternion.Slerp(lineTransform.rotation, rotation, 0.2f);
            }
            else
            {
                lineTransform.localRotation = Quaternion.identity;
            }
        }

        #region ChargePlay

        [Command(ignoreAuthority = true)]
        void CmdCallRpcChargePlay(bool flag)
        {
            RpcChargePlay(flag);
        }

        [ClientRpc]
        void RpcChargePlay(bool flag)
        {
            if (flag)
            {
                charge.Play();
                audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.BEAM_CAHRGE);
                audioSource.time = 0.2f;
                audioSource.volume = SoundManager.BaseSEVolume * 0.15f;
                audioSource.Play();
            }
            else
            {
                charge.Stop();
                audioSource.Stop();
            }
        }

        #endregion

        #region AddChargeParticle

        [Command(ignoreAuthority = true)]
        void CmdCallRpcAddChargeParticle(float add)
        {
            RpcAddChargeParticle(add);
        }

        [ClientRpc]
        void RpcAddChargeParticle(float add)
        {
            chargeMinmaxcurve.constant += add;
            chargeEmission.rateOverTime = chargeMinmaxcurve;
        }

        #endregion

        #region LaserPlay

        [Command(ignoreAuthority = true)]
        void CmdCallRpcLaserPlay()
        {
            RpcLaserPlay();
        }

        [ClientRpc]
        void RpcLaserPlay()
        {
            //Startの再生
            foreach (ParticleSystem p in startChilds)
            {
                p.Play();
            }

            //Midwayの再生
            lineParticle.Play();

            //Endの再生
            foreach (ParticleSystem p in endChilds)
            {
                p.Play();
            }

            //レーザー音の再生
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.BEAM);
            audioSource.volume = SoundManager.BaseSEVolume * 0.05f;
            audioSource.loop = true;
            audioSource.Play();
        }

        #endregion
    }
}