using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class DroneBarrierAction : MonoBehaviour
    {
        [SerializeField] GameObject barrierObject = null;
        public GameObject BarrierObject { get { return barrierObject; } }
        BattleDrone drone = null;

        const float MAX_HP = 100;
        public float HP { get; private set; } = MAX_HP;
        Material material = null;
        const float TRANS_COLOR = 0.5f;

        public bool IsStrength { get; private set; } = false;
        public bool IsWeak { get; private set; } = false;

        //バリアの回復用変数
        [SerializeField] float regeneStartTime = 8.0f;   //バリアが回復しだす時間
        [SerializeField] float regeneInterval = 1.0f;    //回復する間隔
        [SerializeField] float regeneValue = 5.0f;       //バリアが回復する量
        [SerializeField] float resurrectBarrierTime = 15.0f;   //バリアが破壊されてから修復される時間
        [SerializeField] float resurrectBarrierHP = 10.0f;     //バリアが復活した際のHP
        float regeneCountTime;    //計測用
        bool isRegene;    //回復中か

        float damagePercent;    //ダメージ倍率


        void Start()
        {
            drone = GetComponent<BattleDrone>();
            material = barrierObject.GetComponent<Renderer>().material;
            Init();
        }
        
        void Update()
        {
            //バリア弱体化中は回復処理を行わない
            if (IsWeak) return;

            //ドローンが破壊されていたら回復処理を行わない
            if (drone.IsDestroy) return;

            //バリアが破壊されていたら修復処理
            if (HP <= 0)
            {
                if (regeneCountTime >= resurrectBarrierTime)
                {
                    ResurrectBarrier(resurrectBarrierHP);
                    regeneCountTime = 0;
                }
            }
            //バリアが回復を始めるまで待つ
            else if (!isRegene)
            {
                if (regeneCountTime >= regeneStartTime)
                {
                    isRegene = true;
                    regeneCountTime = 0;
                }
            }
            //バリアの回復処理
            else
            {
                if (regeneCountTime >= regeneInterval)
                {
                    if (HP < MAX_HP)
                    {
                        Regene(regeneValue);
                    }
                    regeneCountTime = 0;
                }
            }
            regeneCountTime += Time.deltaTime;
        }

        public void Init()
        {
            HP = MAX_HP;
            regeneCountTime = 0;
            damagePercent = 1;
            isRegene = true;
            IsStrength = false;
            IsWeak = false;
            barrierObject.SetActive(true);
            SetBarrierColor(1, false);
        }

        //HPを回復する
        void Regene(float regeneValue)
        {
            HP += regeneValue;
            if (HP >= MAX_HP)
            {
                HP = MAX_HP;
                Debug.Log("バリアHPMAX: " + HP);
            }
            //デバッグ用
            else
            {
                Debug.Log("リジェネ後バリアHP: " + HP);
            }

            //バリアの色変え
            float value = HP / MAX_HP;
            SetBarrierColor(value, IsStrength);
        }

        //バリアを復活させる
        void ResurrectBarrier(float resurrectHP)
        {
            if (HP > 0) return;

            //修復したら回復処理に移る
            HP = resurrectHP;
            isRegene = true;

            //バリア復活
            barrierObject.SetActive(true);
            float value = HP / MAX_HP;
            SetBarrierColor(value, IsStrength);

            //デバッグ用
            Debug.Log("バリア修復");
        }

        #region Damage

        //バリアに引数分のダメージを与える
        public void Damage(float power)
        {
            float p = Useful.DecimalPointTruncation(power * damagePercent, 1);  //小数点第2以下切り捨て
            HP -= p;

            //バリアHPが0になったらバリアを非表示
            if (HP <= 0)
            {
                HP = 0;
                barrierObject.SetActive(false);
                drone.PlayOneShotSE(SoundManager.SE.DESTROY_BARRIER, SoundManager.BaseSEVolume);
            }
            regeneCountTime = 0;
            isRegene = false;
            drone.PlayOneShotSE(SoundManager.SE.BARRIER_DAMAGE, SoundManager.BaseSEVolume * 0.7f);

            //バリアの色変え
            float value = HP / MAX_HP;
            SetBarrierColor(value, IsStrength);

            Debug.Log("バリアに" + p + "のダメージ\n残りHP: " + HP);
        }

        #endregion

        #region BarrierStrength

        /*
         * バリアの受けるダメージを軽減する
         * 引数1: 軽減する割合(0～1)
         * 引数2: 軽減する時間(秒数)
         */
        public void BarrierStrength(float strengthPrercent, float time)
        {
            damagePercent = 1 - strengthPrercent;
            Invoke(nameof(EndStrength), time);
            IsStrength = true;

            //バリアの色変え
            float value = HP / MAX_HP;
            SetBarrierColor(value, IsStrength);


            //デバッグ用
            Debug.Log("バリア強化");
        }

        //バリア強化を終了させる
        void EndStrength()
        {
            if (IsWeak)
            {
                return;
            }
            damagePercent = 1;
            IsStrength = false;

            //バリアの色変え
            float value = HP / MAX_HP;
            SetBarrierColor(value, IsStrength);


            //デバッグ用
            Debug.Log("バリア強化解除");
        }

        #endregion

        #region BarrierWeak

        //バリア弱体化
        public void BarrierWeak()
        {
            //デバッグ用
            Debug.Log("バリア弱体化");


            if (IsStrength)
            {
                damagePercent = 1;
                IsStrength = false;

                //デバッグ用
                Debug.Log("バリア強化解除");
            }
            else
            {
                HP = Useful.DecimalPointTruncation((HP *= 0.5f), 1);


                //デバッグ用
                Debug.Log("バリアHP: " + HP);
            }

            //バリアの色変え
            float value = HP / MAX_HP;
            SetBarrierColor(value, IsStrength);

            isRegene = false;
            regeneCountTime = 0;

            IsWeak = true;
        }

        //バリア弱体化解除
        public void StopBarrierWeak()
        {
            if (HP <= 0)
            {
                ResurrectBarrier(resurrectBarrierHP);
            }
            IsWeak = false;

            //デバッグ用
            Debug.Log("バリア弱体化解除");
        }

        #endregion

        void SetBarrierColor(float value, bool isStrength)
        {
            if (!isStrength)
            {
                material.color = new Color(1 - value, value, 0, value * TRANS_COLOR);
            }
            else
            {
                material.color = new Color(1 - value, 0, value, value * TRANS_COLOR);
            }
        }
    }
}