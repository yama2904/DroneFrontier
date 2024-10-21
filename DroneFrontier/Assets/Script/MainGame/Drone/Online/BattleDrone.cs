using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mirror;

namespace Online
{
    public class BattleDrone : NetworkBehaviour
    {
        //生成時間
        public float StartTime { get; private set; } = 0;

        //コンポーネント用
        Transform cacheTransform = null;
        Rigidbody _rigidbody = null;
        Animator animator = null;
        DroneBaseAction baseAction = null;
        DroneDamageAction damageAction = null;
        DroneSoundAction soundAction = null;
        DroneLockOnComponent lockOnAction = null;
        DroneRadarAction radarAction = null;
        DroneBarrierAction barrierAction = null;
        DroneItemAction itemAction = null;
        DroneStatusAction statusAction = null;
        AudioListener listener = null;
        NetworkTransformChild[] childObjects;

        //ドローンが移動した際にオブジェクトが傾く処理用
        float moveRotateSpeed = 2f;
        Quaternion frontMoveRotate = Quaternion.Euler(50, 0, 0);
        Quaternion leftMoveRotate = Quaternion.Euler(0, 0, 60);
        Quaternion rightMoveRotate = Quaternion.Euler(0, 0, -60);
        Quaternion backMoveRotate = Quaternion.Euler(-70, 0, 0);

        //ブースト用
        const float BOOST_POSSIBLE_MIN = 0.2f;  //ブースト可能な最低ゲージ量
        [SerializeField] Image boostGaugeImage = null;   //ブーストのゲージ画像
        [SerializeField] Image boostGaugeFrameImage = null; //ゲージ枠
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
        [SyncVar] GameObject syncMainWeapon = null;
        [SyncVar] GameObject syncSubWeapon = null;
        [SyncVar, HideInInspector] public int syncSetSubWeapon = 0;
        BaseWeapon.Weapon setSubWeapon = BaseWeapon.Weapon.SHOTGUN;
        bool[] usingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
        float[] atackingSpeeds = new float[(int)BaseWeapon.Weapon.NONE];   //攻撃中の移動速度
        bool isWeaponInit = false;

        //死亡処理用
        [SerializeField] GameObject droneObject = null;
        [SerializeField] GameObject barrierObject = null;
        [SerializeField] GameObject explosion = null;     //破壊されたときに生成する爆破
        [SyncVar] GameObject syncSpawnedExplosion = null; //生成したexplosion
        Quaternion deathRotate = Quaternion.Euler(28, -28, -28);
        float deathRotateSpeed = 2f;
        float gravityAccele = 1f;  //落下加速用
        float fallTime = 2.5f;   //死亡後の落下時間
        [SyncVar] bool syncIsDestroyFall = false;
        [SyncVar] bool syncIsDestroy = false;


        //アイテム枠
        enum ItemNum
        {
            ITEM_1,   //アイテム枠1
            ITEM_2,   //アイテム枠2

            NONE
        }


        #region Init

        //メイン武器の生成
        [Command]
        void CmdCreateMainWeapon()
        {
            BaseWeapon weapon = BaseWeapon.CreateWeapon(gameObject, BaseWeapon.Weapon.GATLING);
            weapon.parentNetId = netId;
            NetworkServer.Spawn(weapon.gameObject, connectionToClient);
            syncMainWeapon = weapon.gameObject;

            Debug.Log("CreateMainWeapon");
        }

        //サブ武器の生成
        [Command]
        void CmdCreateSubWeapon()
        {
            BaseWeapon weapon = BaseWeapon.CreateWeapon(gameObject, (BaseWeapon.Weapon)syncSetSubWeapon);
            weapon.parentNetId = netId;
            NetworkServer.Spawn(weapon.gameObject, connectionToClient);
            syncSubWeapon = weapon.gameObject;

            Debug.Log("CreateSubWeapon");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            //生成された時間の初期化
            StartTime = Time.time;

            //コンポーネントの初期化
            _rigidbody = GetComponent<Rigidbody>();
            cacheTransform = _rigidbody.transform;
            animator = GetComponent<Animator>();
            baseAction = GetComponent<DroneBaseAction>();
            damageAction = GetComponent<DroneDamageAction>();
            soundAction = GetComponent<DroneSoundAction>();
            lockOnAction = GetComponent<DroneLockOnComponent>();
            radarAction = GetComponent<DroneRadarAction>();
            barrierAction = GetComponent<DroneBarrierAction>();
            itemAction = GetComponent<DroneItemAction>();
            statusAction = GetComponent<DroneStatusAction>();
            childObjects = GetComponents<NetworkTransformChild>();

            //AudioListenerの初期化
            listener = GetComponent<AudioListener>();
            if (!isLocalPlayer)
            {
                listener.enabled = false;
            }

            //プロペラは最初から流す
            soundAction.PlayLoopSE(SoundManager.SE.PROPELLER, SoundManager.SEVolume);

            if (isServer)
            {
                BattleManager.AddServerPlayerData(this, connectionToClient);
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            //攻撃中の移動速度の設定
            atackingSpeeds[(int)BaseWeapon.Weapon.GATLING] = 1f;
            atackingSpeeds[(int)BaseWeapon.Weapon.SHOTGUN] = 1f;
            atackingSpeeds[(int)BaseWeapon.Weapon.MISSILE] = 0.5f;
            atackingSpeeds[(int)BaseWeapon.Weapon.LASER] = 0.35f;

            //ブースト初期化
            boostGaugeImage.enabled = true;
            boostGaugeImage.fillAmount = 1;
            boostGaugeFrameImage.enabled = true;

            //ショットガンの場合はブーストを多少強化する
            setSubWeapon = (BaseWeapon.Weapon)syncSetSubWeapon;
            if (setSubWeapon == BaseWeapon.Weapon.SHOTGUN)
            {
                boostAccele *= 1.2f;
                maxBoostTime *= 1.2f;
                boostRecastTime *= 0.8f;
            }

            //コンポーネント初期化
            itemAction.Init((int)ItemNum.NONE);

            //武器の初期化
            CmdCreateMainWeapon();
            CmdCreateSubWeapon();

            Debug.Log("End: OnStartLocalPlayer");
        }

        #endregion


        void Update()
        {
            if (!isLocalPlayer) return;
            if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない

            //死亡処理中は操作不可
            if (syncIsDestroyFall || syncIsDestroy) return;

            if (damageAction.HP <= 0)
            {
                gravityAccele = 1f;
                CmdDestroyMe();
                return;
            }


            //サブウェポンのUpdate
            if (syncSubWeapon != null)
            {
                if (!isWeaponInit)
                {
                    syncSubWeapon.GetComponent<BaseWeapon>().Init();
                    isWeaponInit = true;
                }
                syncSubWeapon.GetComponent<BaseWeapon>().UpdateMe();
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
                    lockOnAction.StartLockOn();
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
                    soundAction.PlayOneShot(SoundManager.SE.RADAR, SoundManager.SEVolume);
                }
                //レーダー使用
                if (Input.GetKey(KeyCode.Q))
                {
                    if (!statusAction.GetIsStatus(DroneStatusAction.Status.JAMMING))
                    {
                        radarAction.UseRadar();
                    }
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
                    baseAction.ModifySpeed(atackingSpeeds[(int)BaseWeapon.Weapon.GATLING]);
                    usingWeapons[(int)Weapon.MAIN] = true;
                }
            }
            if (Input.GetMouseButton(0))
            {
                if (usingWeapons[(int)Weapon.MAIN])
                {
                    UseWeapon(Weapon.MAIN);  //メインウェポン攻撃
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                //攻撃を止めたら速度を戻す
                if (usingWeapons[(int)Weapon.MAIN])
                {
                    baseAction.ModifySpeed(1 / atackingSpeeds[(int)BaseWeapon.Weapon.GATLING]);
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
                    //攻撃中は速度低下
                    baseAction.ModifySpeed(atackingSpeeds[(int)setSubWeapon]);
                    usingWeapons[(int)Weapon.SUB] = true;
                }
            }
            if (Input.GetMouseButton(1))
            {
                if (usingWeapons[(int)Weapon.SUB])
                {
                    UseWeapon(Weapon.SUB);  //サブウェポン攻撃
                }
            }
            if (Input.GetMouseButtonUp(1))
            {
                //攻撃を止めたら速度を戻す
                if (usingWeapons[(int)Weapon.SUB])
                {
                    baseAction.ModifySpeed(1 / atackingSpeeds[(int)setSubWeapon]);
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
                    boostSoundId = soundAction.PlayLoopSE(SoundManager.SE.BOOST, SoundManager.SEVolume * 0.15f);


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
            if (!isLocalPlayer) return;
            if (syncIsDestroyFall)
            {
                //加速しながら落ちる
                _rigidbody.AddForce(new Vector3(0, -10 * gravityAccele, 0), ForceMode.Acceleration);
                gravityAccele += 20 * Time.deltaTime;

                //ドローンを傾ける
                baseAction.RotateDroneObject(deathRotate, deathRotateSpeed * Time.deltaTime);

                //メイン武器を傾ける
                syncMainWeapon.transform.localRotation = Quaternion.Slerp(syncMainWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

                //サブ武器を傾ける
                syncSubWeapon.transform.localRotation = Quaternion.Slerp(syncSubWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

                //プロペラ減速
                animator.speed *= 0.993f;

                return;
            }
        }

        private void OnDestroy()
        {
            NetworkServer.Destroy(syncSpawnedExplosion);
        }


        //カメラの深度を変更する
        public void SetCameraDepth(int depth)
        {
            baseAction._Camera.depth = depth;
        }

        //AudioListenerのオンオフ
        public void SetAudioListener(bool flag)
        {
            listener.enabled = flag;
        }

        #region Death

        [Command(ignoreAuthority = true)]
        void CmdDestroyMe()
        {
            syncIsDestroyFall = true;
            syncIsDestroy = true;

            //全クライアントで死亡SE再生
            soundAction.RpcPlayOneShotSEAllClient(SoundManager.SE.DEATH, SoundManager.SEVolume);

            //死亡後爆破
            Invoke(nameof(CreateExplosion), fallTime);
        }

        [Server]
        void CreateExplosion()
        {
            //ドローンを非表示にして爆破
            RpcSetActiveAllChildObject(false);

            //ついでに当たり判定も消す
            RpcSetClliderEnabled(false);

            //爆破生成
            GameObject o = Instantiate(explosion, cacheTransform.position, Quaternion.identity);
            NetworkServer.Spawn(o, connectionToClient);
            syncSpawnedExplosion = o;

            //落下停止
            syncIsDestroyFall = false;

            //爆破後消去
            Invoke(nameof(Destroy), 5f);
        }

        void Destroy()
        {
            NetworkServer.Destroy(gameObject);
        }

        //当たり判定のオンオフ
        [ClientRpc]
        void RpcSetClliderEnabled(bool flag)
        {
            GetComponent<Collider>().enabled = flag;
        }

        //レーダーとロックオン解除
        [TargetRpc]
        void TargetStopLockOnAndRadar(NetworkConnection target)
        {
            //死んだのでロックオン・レーダー解除
            lockOnAction.StopLockOn();
            radarAction.StopRadar();
        }

        #endregion


        //攻撃処理
        void UseWeapon(Weapon weapon)
        {
            BaseWeapon bw = null;
            if (weapon == Weapon.MAIN)
            {
                if (syncMainWeapon == null) return;
                bw = syncMainWeapon.GetComponent<BaseWeapon>();
            }
            else if (weapon == Weapon.SUB)
            {
                if (syncSubWeapon == null) return;
                bw = syncSubWeapon.GetComponent<BaseWeapon>();
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
                soundAction.PlayOneShot(SoundManager.SE.USE_ITEM, SoundManager.SEVolume);
            }
        }

        //オブジェクトをすべてのクライアントから非表示
        [ClientRpc]
        void RpcSetActiveAllChildObject(bool flag)
        {
            droneObject.SetActive(flag);
            barrierObject.SetActive(flag);
            syncMainWeapon.SetActive(flag);
            syncSubWeapon.SetActive(flag);
        }

        //オブジェクトを全てのクライアントから削除
        [Command]
        void CmdDestroy(uint netId)
        {
            NetworkServer.Destroy(NetworkIdentity.spawned[netId].gameObject);
        }


        private void OnTriggerStay(Collider other)
        {
            if (!isLocalPlayer) return;
            if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない

            //死亡処理中は操作不可
            if (syncIsDestroyFall || syncIsDestroy) return;


            //Eキーでアイテム取得
            if (Input.GetKey(KeyCode.E))
            {
                if (other.CompareTag(TagNameConst.ITEM))
                {
                    Item item = other.GetComponent<Item>();
                    if (itemAction.SetItem(item.Type))
                    {
                        CmdDestroy(item.netId);
                        Destroy(item.gameObject);   //通信ラグで2回取得するバグの防止


                        //デバッグ用
                        Debug.Log("アイテム取得");
                    }
                }
            }
        }

        [Command(ignoreAuthority = true)]
        void CmdDebugLog(string text)
        {
            RpcDebugLog(text);
        }

        [ClientRpc]
        void RpcDebugLog(string text)
        {
            Debug.Log(text);
        }
    }
}