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
        const float MAX_HP = 30;
        [SyncVar] float syncHP = MAX_HP;

        //コンポーネント用
        Transform cacheTransform = null;
        Rigidbody _rigidbody = null;
        Animator animator = null;
        DroneBaseAction baseAction = null;
        DroneChildObject childObject = null;
        DroneLockOnAction lockOnAction = null;
        DroneRadarAction radarAction = null;
        DroneBarrierAction barrierAction = null;
        DroneItemAction itemAction = null;
        DroneStatusAction statusAction = null;

        //移動用
        [SerializeField, Tooltip("移動速度")] float moveSpeed = 800f;  //移動速度
        float initSpeed = 0; //移動速度の初期値
        float maxSpeed = 0;  //最高速度
        float minSpeed = 0;  //最低速度

        //回転用
        [SerializeField, Tooltip("回転速度")] public float rotateSpeed = 5.0f;

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
        [SerializeField] Image boostGaugeFrameImage = null; //ゲージ枠
        [SerializeField, Tooltip("ブーストの加速度")] float boostAccele = 2.1f;  //ブーストの加速度
        [SerializeField, Tooltip("ブースト時間")] float maxBoostTime = 6.0f;     //ブーストできる最大の時間
        [SerializeField, Tooltip("ブーストのリキャスト時間")] float boostRecastTime = 8.0f;  //ブーストのリキャスト時間
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
        public BaseWeapon.Weapon SetSubWeapon { get; set; } = BaseWeapon.Weapon.SHOTGUN;
        bool[] usingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
        [SerializeField, Tooltip("攻撃中の移動速度の低下率")] float atackingDownSpeed = 0.5f;   //攻撃中の移動速度の低下率
        bool isWeaponInit = false;

        //死亡処理用
        [SerializeField] GameObject explosion = null;
        GameObject spawnedExplosion = null;
        [SerializeField] Image stockIcon = null;
        [SerializeField] Text stockText = null;
        [SyncVar, SerializeField] int syncStock = 1;
        Quaternion deathRotate = Quaternion.Euler(28, -28, -28);
        float deathRotateSpeed = 2f;
        float gravityAccele = 1f;  //落下加速用

        //フラグ
        enum DeathFlag
        {
            FALL,       //破壊されて落下中
            DESTROY,    //破壊されたとき
            RESPAWN,    //リスポーン中
            NON_DAMAGE, //無敵中
            GAME_OVER,  //ストックが尽きて破壊された状態

            NONE
        }
        SyncList<bool> deathFlags = new SyncList<bool>();
        public bool IsDestroy { get { return deathFlags[(int)DeathFlag.DESTROY]; } }
        public bool IsGameOver { get { return deathFlags[(int)DeathFlag.GAME_OVER]; } }

        //リスポーン用
        Vector3 startPos;  //初期座標
        Quaternion startRotate;  //初期角度
        float fallTime = 5.0f;  //死亡後の落下時間
        float nonDamageTime = 4f;  //リスポーン後の無敵時間


        //アイテム枠
        enum ItemNum
        {
            ITEM_1,   //アイテム枠1
            ITEM_2,   //アイテム枠2

            NONE
        }


        //サウンド
        enum SE
        {
            ONE_SHOT,       //ループしない1回きりのSE再生用
            BOOST,          //ブースト
            PROPELLER,      //プロペラ
            JAMMING,        //ジャミング
            MAGNETIC_AREA,  //磁場エリア内

            NONE
        }
        AudioSource[] audios;


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
            BaseWeapon weapon = BaseWeapon.CreateWeapon(gameObject, SetSubWeapon);
            weapon.parentNetId = netId;
            NetworkServer.Spawn(weapon.gameObject, connectionToClient);
            syncSubWeapon = weapon.gameObject;

            Debug.Log("CreateSubWeapon");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            BattleManager.AddPlayerData(this, isLocalPlayer, connectionToClient);

            //コンポーネントの初期化
            cacheTransform = transform;
            _rigidbody = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            baseAction = GetComponent<DroneBaseAction>();
            childObject = GetComponent<DroneChildObject>();
            lockOnAction = GetComponent<DroneLockOnAction>();
            radarAction = GetComponent<DroneRadarAction>();
            barrierAction = GetComponent<DroneBarrierAction>();
            itemAction = GetComponent<DroneItemAction>();
            statusAction = GetComponent<DroneStatusAction>();

            //パラメータ初期化
            initSpeed = moveSpeed;
            maxSpeed = moveSpeed * 3;
            minSpeed = moveSpeed * 0.2f;

            //配列初期化
            for (int i = 0; i < (int)Weapon.NONE; i++)
            {
                usingWeapons[i] = false;
            }
            if (isServer)
            {
                for (int i = 0; i < (int)DeathFlag.NONE; i++)
                {
                    deathFlags.Add(false);
                }
            }

            //AudioSourceの初期化
            audios = GetComponents<AudioSource>();
            audios[(int)SE.BOOST].clip = SoundManager.GetAudioClip(SoundManager.SE.BOOST);
            audios[(int)SE.PROPELLER].clip = SoundManager.GetAudioClip(SoundManager.SE.PROPELLER);
            audios[(int)SE.JAMMING].clip = SoundManager.GetAudioClip(SoundManager.SE.JAMMING_NOISE);
            audios[(int)SE.MAGNETIC_AREA].clip = SoundManager.GetAudioClip(SoundManager.SE.MAGNETIC_AREA);

            //プロペラは延々流す
            PlayLoopSE((int)SE.PROPELLER, SoundManager.BaseSEVolume);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            //ロックオン初期化
            lockOnAction.Init();

            //ブースト初期化
            boostGaugeImage.enabled = true;
            boostGaugeImage.fillAmount = 1;
            boostGaugeFrameImage.enabled = true;

            //ショットガンの場合はブーストを多少強化する
            if (SetSubWeapon == BaseWeapon.Weapon.SHOTGUN)
            {
                boostAccele *= 1.2f;
                maxBoostTime *= 1.2f;
                boostRecastTime *= 0.8f;
            }

            //コンポーネント初期化
            itemAction.Init((int)ItemNum.NONE);
            statusAction.Init(minSpeed, maxSpeed);

            //残機UIの初期化
            stockIcon.enabled = true;
            stockText.enabled = true;
            stockText.text = syncStock.ToString();

            //初期値保存
            startPos = transform.position;
            startRotate = transform.rotation;

            //武器の初期化
            CmdCreateMainWeapon();
            CmdCreateSubWeapon();

            Debug.Log("End: OnStartLocalPlayer");
        }

        #endregion


        void Update()
        {
            if (!isLocalPlayer) return;
            if (!BattleManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない

            //死亡・リスポーン処理中は操作不可
            if (deathFlags[(int)DeathFlag.GAME_OVER] ||
                deathFlags[(int)DeathFlag.FALL] ||
                deathFlags[(int)DeathFlag.DESTROY] ||
                deathFlags[(int)DeathFlag.RESPAWN]) return;


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
                baseAction.Move(moveSpeed, cacheTransform.forward);
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
                baseAction.Move(moveSpeed, left);
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
                baseAction.Move(moveSpeed, backward);
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
                baseAction.Move(moveSpeed, right);
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
                baseAction.Move(moveSpeed * Input.mouseScrollDelta.y, upward);
            }
            if (Input.GetKey(KeyCode.R))
            {
                Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
                Vector3 upward = upAngle.normalized * Vector3.forward;
                baseAction.Move(moveSpeed, upward);
            }
            if (Input.GetKey(KeyCode.F))
            {
                Quaternion downAngle = Quaternion.Euler(90, 0, 0);
                Vector3 down = downAngle.normalized * Vector3.forward;
                baseAction.Move(moveSpeed, down);
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
                    PlayOneShotSE(SoundManager.SE.RADAR, SoundManager.BaseSEVolume);
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
                float x = Input.GetAxis("Mouse X");
                float y = Input.GetAxis("Mouse Y");
                baseAction.Rotate(x, y, rotateSpeed * CameraManager.CameraSpeed);
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
                    moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, atackingDownSpeed);
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
                    moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 1 / atackingDownSpeed);
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
                    if (SetSubWeapon == BaseWeapon.Weapon.MISSILE)
                    {
                        //攻撃中は速度低下
                        moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, atackingDownSpeed);
                    }
                    if(SetSubWeapon == BaseWeapon.Weapon.LASER)
                    {
                        moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, atackingDownSpeed * 0.75f);
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
                    if (SetSubWeapon == BaseWeapon.Weapon.MISSILE)
                    {
                        //攻撃中は速度低下
                        moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 1 / atackingDownSpeed);
                    }
                    if (SetSubWeapon == BaseWeapon.Weapon.LASER)
                    {
                        moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 1 / (atackingDownSpeed * 0.75f));
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
                    moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, boostAccele);
                    isBoost = true;
                    PlayLoopSE((int)SE.BOOST, SoundManager.BaseSEVolume * 0.15f);    //加速音の再生


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

                        moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 1 / boostAccele);
                        isBoost = false;
                        StopSE((int)SE.BOOST);


                        //デバッグ用
                        Debug.Log("ブースト終了");
                    }
                }
                //キーを離したらブースト停止
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 1 / boostAccele);
                    isBoost = false;
                    StopSE((int)SE.BOOST);


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

            //スピードのバグが起きたときに無理やり戻す
            bool useWeapon = false;
            foreach (bool use in usingWeapons)
            {
                if (use)
                {
                    useWeapon = true;
                    break;
                }
            }
            if (!useWeapon)
            {
                if (!statusAction.GetIsStatus(DroneStatusAction.Status.SPEED_DOWN) && !isBoost)
                {
                    moveSpeed = initSpeed;
                }
            }
        }

        void FixedUpdate()
        {
            if (!isLocalPlayer) return;
            if (deathFlags[(int)DeathFlag.FALL])
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

        //プレイヤーにダメージを与える
        [Command(ignoreAuthority = true)]
        public void CmdDamage(float power)
        {
            //死亡・リスポーン処理・無敵中はダメージ処理を行わない
            if (deathFlags[(int)DeathFlag.GAME_OVER] ||
                deathFlags[(int)DeathFlag.FALL] ||
                deathFlags[(int)DeathFlag.DESTROY] ||
                deathFlags[(int)DeathFlag.RESPAWN] ||
                deathFlags[(int)DeathFlag.NON_DAMAGE]) return;

            //小数点第2以下切り捨て
            float p = Useful.DecimalPointTruncation(power, 1);

            //バリアが破壊されていなかったらバリアにダメージを肩代わりさせる
            if (barrierAction.HP > 0)
            {
                barrierAction.Damage(p);
            }
            //バリアが破壊されていたらドローンが直接ダメージを受ける
            else
            {
                syncHP -= power;
                if (syncHP < 0)
                {
                    syncHP = 0;
                    StartCoroutine(DestroyMe());
                }

                //デバッグ用
                Debug.Log(name + "に" + power + "のダメージ\n残りHP: " + syncHP);
            }
        }


        #region Sound

        //ループSE再生
        void PlayLoopSE(int index, float volume)
        {
            if (index >= (int)SE.NONE) return;
            if (volume > 1.0f)
            {
                volume = 1.0f;
            }

            audios[index].volume = volume;
            audios[index].loop = true;
            audios[index].Play();
        }

        //SE停止
        void StopSE(int index)
        {
            if (index >= (int)SE.NONE) return;
            audios[index].Stop();
        }

        //1回のみ発生する再生のSE
        public void PlayOneShotSE(SoundManager.SE se, float volume)
        {
            if (se == SoundManager.SE.NONE) return;

            AudioSource audio = audios[(int)SE.ONE_SHOT];
            audio.volume = volume;
            audio.PlayOneShot(SoundManager.GetAudioClip(se));
        }

        //全てのクライアントでSEを再生する
        [ClientRpc]
        public void RpcPlayOneShotSEAllClient(SoundManager.SE se, float volume)
        {
            PlayOneShotSE(se, volume);
        }

        #endregion


        #region 状態系処理

        //バリア強化
        public bool SetBarrierStrength(float strengthPercent, float time)
        {
            return statusAction.SetBarrierStrength(strengthPercent, time);
        }

        //バリア弱体化
        public void SetBarrierWeak()
        {
            statusAction.SetBarrierWeak();
        }

        //バリア弱体化解除
        public void UnSetBarrierWeak()
        {
            statusAction.UnSetBarrierWeak();
        }

        //ジャミング
        public void SetJamming()
        {
            statusAction.SetJamming();
            PlayLoopSE((int)SE.JAMMING, SoundManager.BaseSEVolume);
        }

        //ジャミング解除
        public void UnSetJamming()
        {
            statusAction.UnSetJamming();
            StopSE((int)SE.JAMMING);
        }

        //スタン
        public void SetStun(float time)
        {
            statusAction.SetStun(time);
        }

        //スピードダウン
        public int SetSpeedDown(float downPercent)
        {
            PlayLoopSE((int)SE.MAGNETIC_AREA, SoundManager.BaseSEVolume);
            return statusAction.SetSpeedDown(ref moveSpeed, downPercent);
        }

        //スピードダウン解除
        public void UnSetSpeedDown(int id)
        {
            statusAction.UnSetSpeedDown(ref moveSpeed, id);
            StopSE((int)SE.MAGNETIC_AREA);
        }

        #endregion

        //カメラの深度を変更する
        public void SetCameraDepth(int depth)
        {
            baseAction._Camera.depth = depth;
        }

        //AudioListenerのオンオフ
        public void SetAudioListener(bool flag)
        {
            baseAction.Listener.enabled = flag;
        }

        #region Death

        [Server]
        IEnumerator DestroyMe()
        {
            gravityAccele = 1f;
            deathFlags[(int)DeathFlag.FALL] = true;
            deathFlags[(int)DeathFlag.DESTROY] = true;
            deathFlags[(int)DeathFlag.RESPAWN] = true;

            //全クライアントで死亡SE再生
            RpcPlayOneShotSEAllClient(SoundManager.SE.DEATH, SoundManager.BaseSEVolume);

            //死亡後爆破
            yield return new WaitForSeconds(2.5f);

            //ドローンを非表示にして爆破
            childObject.RpcSetActive(false, DroneChildObject.Child.DRONE_OBJECT);
            childObject.RpcSetActive(false, DroneChildObject.Child.BARRIER);
            RpcSetActive(syncMainWeapon, false);
            RpcSetActive(syncSubWeapon, false);
            RpcSetClliderEnabled(false);  //ついでに当たり判定も消す
            GameObject o = Instantiate(explosion, cacheTransform.position, Quaternion.identity);
            NetworkServer.Spawn(o, connectionToClient);
            spawnedExplosion = o;
            deathFlags[(int)DeathFlag.FALL] = false;

            if (syncStock <= 0)
            {
                //ドローンを完全に非表示にして終了処理
                Invoke(nameof(GameOver), fallTime);
            }
            else
            {
                syncStock--;
                Invoke(nameof(Respawn), fallTime);
            }
        }

        [ClientRpc]
        void RpcSetClliderEnabled(bool flag)
        {
            GetComponent<Collider>().enabled = flag;
        }

        [TargetRpc]
        void TargetStopLockOnAndRadar(NetworkConnection target)
        {
            //死んだのでロックオン・レーダー解除
            lockOnAction.StopLockOn();
            radarAction.StopRadar();
        }

        [Server]
        void GameOver()
        {
            RpcGameOver();
            RpcSetActive(gameObject, false);
            deathFlags[(int)DeathFlag.GAME_OVER] = true;
        }

        [ClientRpc]
        void RpcGameOver()
        {
            BattleManager.Singleton.SetDestroyedDrone(netId);
        }

        #endregion

        #region Respawn

        //リスポーン処理
        [Server]
        void Respawn()
        {
            //ドローン表示
            childObject.RpcSetActive(true, DroneChildObject.Child.DRONE_OBJECT);
            childObject.RpcSetActive(true, DroneChildObject.Child.BARRIER);
            RpcSetActive(syncMainWeapon, true);
            RpcSetActive(syncSubWeapon, true);

            //当たり判定も戻す
            RpcSetClliderEnabled(true);

            //ローカルプレイヤー用処理
            TargetRespawn(connectionToClient);

            //HP初期化
            syncHP = MAX_HP;

            //一時的に無敵
            deathFlags[(int)DeathFlag.NON_DAMAGE] = true;
            Invoke(nameof(SetNonDamageFalse), nonDamageTime);

            //生成した爆破を削除
            NetworkServer.Destroy(spawnedExplosion);

            //操作可能にする
            deathFlags[(int)DeathFlag.DESTROY] = false;
            deathFlags[(int)DeathFlag.RESPAWN] = false;
        }

        [TargetRpc]
        void TargetRespawn(NetworkConnection target)
        {
            //移動の初期化
            _rigidbody.velocity = new Vector3(0, 0, 0);
            moveSpeed = initSpeed;

            //初期位置に移動
            cacheTransform.position = startPos;

            //重力補正初期化
            gravityAccele = 1f;

            //所持アイテム初期化
            itemAction.ResetItem();

            //状態異常初期化
            statusAction.ResetStatus();

            //ブーストゲージ回復
            boostGaugeImage.fillAmount = 1f;
            isBoost = false;

            //残機を表記に適用
            stockText.text = syncStock.ToString();

            //サブ武器初期化
            syncSubWeapon.GetComponent<BaseWeapon>().ResetWeapon();

            //バリア復活
            barrierAction.CmdInit();

            //プロペラ再生
            animator.speed = 1f;

            //SEストップ
            StopSE((int)SE.MAGNETIC_AREA);
            StopSE((int)SE.JAMMING);

            //角度の初期化
            cacheTransform.rotation = startRotate;
            childObject.GetChild(DroneChildObject.Child.DRONE_OBJECT).localRotation = Quaternion.identity;
            syncMainWeapon.transform.localRotation = Quaternion.identity;
            syncSubWeapon.transform.localRotation = Quaternion.identity;

            //リスポーンSE再生
            RpcPlayOneShotSEAllClient(SoundManager.SE.RESPAWN, SoundManager.BaseSEVolume);
        }

        //無敵解除
        void SetNonDamageFalse()
        {
            deathFlags[(int)DeathFlag.NON_DAMAGE] = false;
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
                PlayOneShotSE(SoundManager.SE.USE_ITEM, SoundManager.BaseSEVolume);
            }
        }

        //オブジェクトをすべてのクライアントから非表示
        [ClientRpc]
        void RpcSetActive(GameObject o, bool flag)
        {
            o.SetActive(flag);
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
            if (!BattleManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない

            //死亡・リスポーン処理中は操作不可
            if (deathFlags[(int)DeathFlag.GAME_OVER] ||
                deathFlags[(int)DeathFlag.FALL] ||
                deathFlags[(int)DeathFlag.DESTROY] ||
                deathFlags[(int)DeathFlag.RESPAWN]) return;


            //Eキーでアイテム取得
            if (Input.GetKey(KeyCode.E))
            {
                if (other.CompareTag(TagNameManager.ITEM))
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