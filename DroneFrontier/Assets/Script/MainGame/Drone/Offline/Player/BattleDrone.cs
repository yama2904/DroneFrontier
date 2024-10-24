﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Offline
{
    namespace Player
    {
        public class BattleDrone : BaseDrone
        {
            //コンポーネント用
            Transform cacheTransform = null;
            Rigidbody _rigidbody = null;
            Animator animator = null;
            DroneBaseAction baseAction = null;
            DroneDamageAction damageAction = null;
            DroneSoundAction soundAction = null;
            DroneLockOnAction lockOnAction = null;
            DroneRadarAction radarAction = null;
            DroneBarrierAction barrierAction = null;
            DroneItemAction itemAction = null;
            DroneStatusAction statusAction = null;

            //ドローンが移動した際にオブジェクトが傾く処理用
            float moveRotateSpeed = 2f;
            Quaternion frontMoveRotate = Quaternion.Euler(50, 0, 0);
            Quaternion leftMoveRotate = Quaternion.Euler(0, 0, 60);
            Quaternion rightMoveRotate = Quaternion.Euler(0, 0, -60);
            Quaternion backMoveRotate = Quaternion.Euler(-70, 0, 0);

            //ロックオン
            [SerializeField, Tooltip("ロックオンした際に敵に向く速度")] float lockOnTrackingSpeed = 0.2f;

            //ブースト用
            const float BOOST_POSSIBLE_MIN = 0.2f;  //ブースト可能な最低ゲージ量
            [SerializeField] Image boostGaugeImage = null;   //ブーストのゲージ画像
            [SerializeField, Tooltip("ブーストの加速度")] float boostAccele = 2.1f;  //ブーストの加速度
            [SerializeField, Tooltip("ブースト時間")] float maxBoostTime = 6.0f;     //ブーストできる最大の時間
            [SerializeField, Tooltip("ブーストのリキャスト時間")] float boostRecastTime = 8.0f;  //ブーストのリキャスト時間
            int boostSoundId = -1;
            bool isBoost = false;


            //武器
            protected enum Weapon
            {
                MAIN,   //メイン武器
                SUB,    //サブ武器

                NONE
            }
            BaseWeapon mainWeapon = null;
            BaseWeapon subWeapon = null;
            public BaseWeapon.Weapon setSubWeapon = BaseWeapon.Weapon.SHOTGUN;
            bool[] usingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
            [SerializeField, Tooltip("攻撃中の移動速度の低下率")] float atackingDownSpeed = 0.5f;   //攻撃中の移動速度の低下率

            //死亡処理用
            [SerializeField] GameObject explosion = null;
            [SerializeField] Transform droneObject = null;
            Quaternion deathRotate = Quaternion.Euler(28, -28, -28);
            float deathRotateSpeed = 2f;
            float gravityAccele = 1f;  //落下加速用
            float fallTime = 2.5f;     //死亡後の落下時間
            bool isDestroyFall = false;
            bool isDestroy = false;


            //アイテム枠
            enum ItemNum
            {
                ITEM_1,   //アイテム枠1
                ITEM_2,   //アイテム枠2

                NONE
            }


            protected override void Awake()
            {
                base.Awake();

                //コンポーネントの取得
                _rigidbody = GetComponent<Rigidbody>();
                cacheTransform = _rigidbody.transform;
                animator = GetComponent<Animator>();
                baseAction = GetComponent<DroneBaseAction>();
                damageAction = GetComponent<DroneDamageAction>();
                soundAction = GetComponent<DroneSoundAction>();
                lockOnAction = GetComponent<DroneLockOnAction>();
                radarAction = GetComponent<DroneRadarAction>();
                barrierAction = GetComponent<DroneBarrierAction>();
                itemAction = GetComponent<DroneItemAction>();
                statusAction = GetComponent<DroneStatusAction>();
            }

            protected override void Start()
            {
                base.Start();

                //コンポーネントの初期化
                lockOnAction.Init();
                itemAction.Init((int)ItemNum.NONE);


                //武器初期化
                mainWeapon = BaseWeapon.CreateWeapon(this, BaseWeapon.Weapon.GATLING, true);
                mainWeapon.SetParent(transform);
                subWeapon = BaseWeapon.CreateWeapon(this, setSubWeapon, true);
                subWeapon.SetParent(transform);

                //ブースト初期化
                boostGaugeImage.enabled = true;
                boostGaugeImage.fillAmount = 1;

                //ショットガンの場合はブーストを多少強化する
                if (setSubWeapon == BaseWeapon.Weapon.SHOTGUN)
                {
                    boostAccele *= 1.2f;
                    maxBoostTime *= 1.2f;
                    boostRecastTime *= 0.8f;
                }


                //プロペラは最初から流す
                soundAction.PlayLoopSE(SoundManager.SE.PROPELLER, SoundManager.BaseSEVolume);
            }

            void Update()
            {
                if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない

                //死亡処理中は操作不可
                if (isDestroyFall || isDestroy) return;

                if (damageAction.HP <= 0)
                {
                    DestroyMe();
                    return;
                }

                #region Move

                //移動処理
                //前進
                if (Input.GetKey(KeyCode.W))
                {
                    baseAction.Move(cacheTransform.forward);
                    baseAction.RotateDroneObject(frontMoveRotate, moveRotateSpeed * Time.deltaTime);
                }
                else
                {
                    baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
                }

                //左移動
                if (Input.GetKey(KeyCode.A))
                {
                    Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
                    Vector3 left = leftAngle.normalized * cacheTransform.forward;
                    baseAction.Move(left);
                    baseAction.RotateDroneObject(leftMoveRotate, moveRotateSpeed * Time.deltaTime);
                }
                else
                {
                    baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
                }

                //後退
                if (Input.GetKey(KeyCode.S))
                {
                    Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
                    Vector3 backward = backwardAngle.normalized * cacheTransform.forward;
                    baseAction.Move(backward);
                    baseAction.RotateDroneObject(backMoveRotate, moveRotateSpeed * Time.deltaTime);
                }
                else
                {
                    baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
                }

                //右移動
                if (Input.GetKey(KeyCode.D))
                {
                    Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
                    Vector3 right = rightAngle.normalized * cacheTransform.forward;
                    baseAction.Move(right);
                    baseAction.RotateDroneObject(rightMoveRotate, moveRotateSpeed * Time.deltaTime);
                }
                else
                {
                    baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
                }

                //上下移動
                if (Input.mouseScrollDelta.y != 0)
                {
                    Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
                    Vector3 upward = upAngle.normalized * Vector3.forward;
                    baseAction.Move(upward * Input.mouseScrollDelta.y);
                }
                if (Input.GetKey(KeyCode.R))
                {
                    Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
                    Vector3 upward = upAngle.normalized * Vector3.forward;
                    baseAction.Move(upward);
                }
                if (Input.GetKey(KeyCode.F))
                {
                    Quaternion downAngle = Quaternion.Euler(90, 0, 0);
                    Vector3 down = downAngle.normalized * Vector3.forward;
                    baseAction.Move(down);
                }

                #endregion


                //
                //設定画面中はここより下の処理は行わない
                if (MainGameManager.IsConfig)
                {
                    return;
                }
                //
                //


                #region LockOn

                //ロックオン使用
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (!statusAction.GetIsStatus(DroneStatusAction.Status.JAMMING))
                    {
                        lockOnAction.UseLockOn(lockOnTrackingSpeed);
                    }
                }
                //ロックオン解除
                if (Input.GetKeyUp(KeyCode.LeftShift))
                {
                    lockOnAction.StopLockOn();
                }

                #endregion

                #region Radar

                //ジャミング中は処理しない
                if (!statusAction.GetIsStatus(DroneStatusAction.Status.JAMMING))
                {
                    //レーダー音の再生
                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        soundAction.PlayOneShot(SoundManager.SE.RADAR, SoundManager.BaseSEVolume);
                    }
                    //レーダー使用
                    if (Input.GetKey(KeyCode.Q))
                    {
                        radarAction.UseRadar();
                    }
                }
                //レーダー終了
                if (Input.GetKeyUp(KeyCode.Q))
                {
                    radarAction.StopRadar();
                }

                #endregion


                //回転処理
                if (MainGameManager.IsCursorLock)
                {
                    Vector3 angle = Vector3.zero;
                    angle.x = Input.GetAxis("Mouse X");
                    angle.y = Input.GetAxis("Mouse Y");
                    baseAction.Rotate(angle * CameraManager.CameraSpeed);
                }


                #region Weapon

                //メイン武器攻撃
                if (Input.GetMouseButtonDown(0))
                {
                    //サブ武器を使用していたら撃てない
                    //バグ防止用にメイン武器フラグも調べる
                    if (!usingWeapons[(int)Weapon.SUB] && !usingWeapons[(int)Weapon.MAIN])
                    {
                        //攻撃中は速度低下
                        baseAction.ModifySpeed(atackingDownSpeed);
                        usingWeapons[(int)Weapon.MAIN] = true;
                    }
                }
                if (Input.GetMouseButton(0))
                {
                    if (usingWeapons[(int)Weapon.MAIN])
                    {
                        UseWeapon(Weapon.MAIN);     //メインウェポン攻撃
                    }
                }
                if (Input.GetMouseButtonUp(0))
                {
                    //攻撃を止めたら速度を戻す
                    if (usingWeapons[(int)Weapon.MAIN])
                    {
                        baseAction.ModifySpeed(1 / atackingDownSpeed);
                        usingWeapons[(int)Weapon.MAIN] = false;
                    }
                }

                //サブ武器攻撃
                if (Input.GetMouseButtonDown(1))
                {
                    //サブ武器を使用していたら撃てない
                    //バグ防止用にサブ武器フラグも調べる
                    if (!usingWeapons[(int)Weapon.MAIN] && !usingWeapons[(int)Weapon.SUB])
                    {
                        if (setSubWeapon == BaseWeapon.Weapon.MISSILE)
                        {
                            //攻撃中は速度低下
                            baseAction.ModifySpeed(atackingDownSpeed);
                        }
                        //レーザーの場合は低下率増加
                        if (setSubWeapon == BaseWeapon.Weapon.LASER)
                        {
                            baseAction.ModifySpeed(atackingDownSpeed * 0.75f);
                        }
                        usingWeapons[(int)Weapon.SUB] = true;
                    }
                }
                if (Input.GetMouseButton(1))
                {
                    if (usingWeapons[(int)Weapon.SUB])
                    {
                        UseWeapon(Weapon.SUB);      //サブウェポン攻撃
                    }
                }
                if (Input.GetMouseButtonUp(1))
                {
                    //攻撃を止めたら速度を戻す
                    if (usingWeapons[(int)Weapon.SUB])
                    {
                        if (setSubWeapon == BaseWeapon.Weapon.MISSILE)
                        {
                            //攻撃中は速度低下
                            baseAction.ModifySpeed(1 / atackingDownSpeed);
                        }
                        //レーザーの場合は低下率増加
                        if (setSubWeapon == BaseWeapon.Weapon.LASER)
                        {
                            baseAction.ModifySpeed(1 / (atackingDownSpeed * 0.75f));
                        }
                        usingWeapons[(int)Weapon.SUB] = false;
                    }
                }

                #endregion

                #region Boost

                //ブースト使用
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    //ブーストが使用可能なゲージ量ならブースト使用
                    if (boostGaugeImage.fillAmount >= BOOST_POSSIBLE_MIN)
                    {
                        baseAction.ModifySpeed(boostAccele);
                        isBoost = true;

                        //加速音の再生
                        boostSoundId = soundAction.PlayLoopSE(SoundManager.SE.BOOST, SoundManager.BaseSEVolume * 0.15f);

                        //デバッグ用
                        Debug.Log("ブースト使用");
                    }
                }
                //ブースト使用中の処理
                if (isBoost)
                {
                    //キーを押し続けている間はゲージ消費
                    if (Input.GetKey(KeyCode.Space))
                    {
                        boostGaugeImage.fillAmount -= 1.0f / maxBoostTime * Time.deltaTime;

                        //ゲージが空になったらブースト停止
                        if (boostGaugeImage.fillAmount <= 0)
                        {
                            boostGaugeImage.fillAmount = 0;

                            baseAction.ModifySpeed(1 / boostAccele);
                            isBoost = false;

                            //ブーストSE停止
                            soundAction.StopLoopSE(boostSoundId);


                            //デバッグ用
                            Debug.Log("ブースト終了");
                        }
                    }
                    //キーを離したらブースト停止
                    if (Input.GetKeyUp(KeyCode.Space))
                    {
                        baseAction.ModifySpeed(1 / boostAccele);
                        isBoost = false;

                        //ブーストSE停止
                        soundAction.StopLoopSE(boostSoundId);


                        //デバッグ用
                        Debug.Log("ブースト終了");
                    }
                }

                //ブースト未使用時にゲージ回復
                if (!isBoost)
                {
                    if (boostGaugeImage.fillAmount < 1.0f)
                    {
                        boostGaugeImage.fillAmount += 1.0f / boostRecastTime * Time.deltaTime;
                        if (boostGaugeImage.fillAmount >= 1.0f)
                        {
                            boostGaugeImage.fillAmount = 1;
                        }
                    }
                }

                #endregion


                //アイテム使用
                if (Input.GetKeyUp(KeyCode.Alpha1))
                {
                    UseItem(ItemNum.ITEM_1);
                }
                if (Input.GetKeyUp(KeyCode.Alpha2))
                {
                    UseItem(ItemNum.ITEM_2);
                }
            }

            void FixedUpdate()
            {
                if (isDestroyFall)
                {
                    //加速しながら落ちる
                    _rigidbody.AddForce(new Vector3(0, -10 * gravityAccele, 0), ForceMode.Acceleration);
                    gravityAccele += 20 * Time.deltaTime;

                    //ドローンを傾ける
                    baseAction.RotateDroneObject(deathRotate, deathRotateSpeed * Time.deltaTime);

                    //メイン武器を傾ける
                    mainWeapon.transform.localRotation = Quaternion.Slerp(mainWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

                    //サブ武器を傾ける
                    subWeapon.transform.localRotation = Quaternion.Slerp(subWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

                    //プロペラ減速
                    animator.speed *= 0.993f;

                    return;
                }
            }

            private void OnDestroy()
            {

            }


            void DestroyMe()
            {
                gravityAccele = 1f;
                isDestroyFall = true;
                isDestroy = true;

                //死んだのでロックオン・レーダー解除
                lockOnAction.StopLockOn();
                radarAction.StopRadar();

                //死亡SE再生
                soundAction.PlayOneShot(SoundManager.SE.DEATH, SoundManager.BaseSEVolume);

                //爆破後消去
                Invoke(nameof(CreateExplosion), fallTime);
            }

            //ドローンを非表示にして爆破
            void CreateExplosion()
            {
                //ドローンの非表示
                droneObject.gameObject.SetActive(false);
                barrierAction.BarrierObject.SetActive(false);
                mainWeapon.gameObject.SetActive(false);
                subWeapon.gameObject.SetActive(false);

                //当たり判定も消す
                GetComponent<Collider>().enabled = false;

                //爆破生成
                Instantiate(explosion, cacheTransform);

                //落下停止
                isDestroyFall = false;

                //爆破後一定時間で消去
                Destroy(gameObject, 5f);
            }

            //攻撃処理
            void UseWeapon(Weapon weapon)
            {
                BaseWeapon bw = null;
                if (weapon == Weapon.MAIN)
                {
                    if (mainWeapon == null) return;
                    bw = mainWeapon.GetComponent<BaseWeapon>();
                }
                else if (weapon == Weapon.SUB)
                {
                    if (subWeapon == null) return;
                    bw = subWeapon.GetComponent<BaseWeapon>();
                }
                else
                {
                    return;
                }

                bw.Shot(lockOnAction.Target);
            }

            //アイテム使用
            void UseItem(ItemNum item)
            {
                //アイテム枠にアイテムを持っていたら使用
                if (itemAction.UseItem((int)item))
                {
                    soundAction.PlayOneShot(SoundManager.SE.USE_ITEM, SoundManager.BaseSEVolume);
                }
            }


            private void OnTriggerStay(Collider other)
            {
                if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない

                //死亡処理中は操作不可
                if (isDestroyFall) return;


                //Eキーでアイテム取得
                if (Input.GetKey(KeyCode.E))
                {
                    if (other.CompareTag(TagNameManager.ITEM))
                    {
                        Item item = other.GetComponent<Item>();
                        if (itemAction.SetItem(item.Type))
                        {
                            Destroy(item.gameObject);

                            //デバッグ用
                            Debug.Log("アイテム取得");
                        }
                    }
                }
            }
        }
    }
}