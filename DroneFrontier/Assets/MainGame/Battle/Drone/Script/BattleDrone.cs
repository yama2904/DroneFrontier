using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mirror;

public class BattleDrone : NetworkBehaviour
{
    const float MAX_HP = 30;
    [SyncVar, SerializeField] float syncHP = MAX_HP;

    //コンポーネント用
    Transform cacheTransform = null;
    Rigidbody _rigidbody = null;
    Animator animator = null;
    DroneBaseAction baseAction = null;
    DroneBarrierAction barrierAction = null;
    DroneItemAction itemAction = null;
    DroneStatusAction statusAction = null;

    //移動用
    [SerializeField, Tooltip("移動速度")] float moveSpeed = 100f;      //移動速度
    float initSpeed = 0;  //移動速度の初期値
    [HideInInspector] float maxSpeed = 100;  //最高速度
    [HideInInspector] float minSpeed = 100;  //最低速度

    //回転用
    [SerializeField, Tooltip("回転速度")] public float rotateSpeed = 5.0f;
    float initRotateSpeed = 0;

    //ドローンが移動した際にオブジェクトが傾く処理用
    float moveRotateSpeed = 2f;
    Quaternion frontMoveRotate = Quaternion.Euler(50, 0, 0);
    Quaternion leftMoveRotate = Quaternion.Euler(0, 0, 60);
    Quaternion rightMoveRotate = Quaternion.Euler(0, 0, -60);
    Quaternion backMoveRotate = Quaternion.Euler(-70, 0, 0);

    //ロックオン
    [SerializeField] LockOn lockOn = null;
    [SerializeField, Tooltip("ロックオンした際に敵に向く速度")] float lockOnTrackingSpeed = 0.1f;
    float initLockOnTrackingSpeed = 0;

    //レーダー
    [SerializeField] Radar radar = null;

    //ブースト用
    const float BOOST_POSSIBLE_MIN = 0.2f;  //ブースト可能な最低ゲージ量
    [SerializeField] Image boostGaugeImage = null;   //ブーストのゲージ画像
    [SerializeField] Image boostGaugeFrameImage = null; //ゲージ枠
    [SerializeField, Tooltip("ブーストの加速度")] float boostAccele = 3.0f;  //ブーストの加速度
    [SerializeField, Tooltip("ブースト時間")] float maxBoostTime = 5.0f;     //ブーストできる最大の時間
    [SerializeField, Tooltip("ブーストのリキャスト時間")] float boostRecastTime = 6.0f;  //ブーストのリキャスト時間
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
    public BaseWeapon.Weapon SetSubWeapon { get; set; } = BaseWeapon.Weapon.LASER;
    bool[] usingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
    [SerializeField, Tooltip("攻撃中の移動速度の低下率")] float atackingDownSpeed = 0.5f;   //攻撃中の移動速度の低下率
    bool initSubWeapon = false;

    //死亡処理用
    Quaternion deathRotate = Quaternion.Euler(28, -28, -28);
    float deathRotateSpeed = 2f;
    [SyncVar] float syncGravityAccele = 1f;  //落下加速用
    [SyncVar] bool syncIsDestroy = false;    //ドローンが破壊されたときtrue
    public bool IsDestroy { get { return syncIsDestroy; } }

    //リスポーン用
    [SyncVar, SerializeField] int syncStock = 2;
    [SerializeField, Tooltip("死亡後の落下時間")] float fallTime = 5.0f;
    Vector3 startPos;
    Quaternion startRotate;
    [SyncVar] bool syncIsRespawning = false;
    [SyncVar] bool nonDamage = false; //無敵
    [SerializeField, Tooltip("リスポーン後の無敵時間")] float nonDamageTime = 4f;

    //ゲームオーバーになったらtrue
    [SyncVar] bool syncIsGameOver = false;
    public bool IsGameOver { get { return syncIsGameOver; } }


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
        RpcSetWeapon(weapon.gameObject, true);

        Debug.Log("CreateMainWeapon");
    }

    //サブ武器の生成
    [Command]
    void CmdCreateSubWeapon()
    {
        BaseWeapon weapon = BaseWeapon.CreateWeapon(gameObject, SetSubWeapon);
        weapon.parentNetId = netId;
        NetworkServer.Spawn(weapon.gameObject, connectionToClient);
        RpcSetWeapon(weapon.gameObject, false);

        Debug.Log("CreateSubWeapon");
    }

    //生成した武器情報を渡す
    [ClientRpc]
    void RpcSetWeapon(GameObject weapon, bool setMainWeapon)
    {
        if (setMainWeapon)
        {
            syncMainWeapon = weapon;
        }
        else
        {
            syncSubWeapon = weapon;
        }
    }


    public override void OnStartClient()
    {
        base.OnStartClient();
        BattleManager.AddPlayerData(this, isLocalPlayer);

        //コンポーネントの初期化
        cacheTransform = transform;
        _rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        baseAction = GetComponent<DroneBaseAction>();
        barrierAction = GetComponent<DroneBarrierAction>();
        itemAction = GetComponent<DroneItemAction>();
        statusAction = GetComponent<DroneStatusAction>();

        initSpeed = moveSpeed;
        maxSpeed = moveSpeed * 10;
        minSpeed = moveSpeed * 0.2f;
        initRotateSpeed = rotateSpeed;
        initLockOnTrackingSpeed = lockOnTrackingSpeed;

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

        //ブースト初期化
        boostGaugeImage.enabled = true;
        boostGaugeImage.fillAmount = 1;
        boostGaugeFrameImage.enabled = true;

        //コンポーネント初期化
        itemAction.Init((int)ItemNum.NONE);
        if (lockOn != null && radar != null)  //エラー防止
        {
            statusAction.Init(lockOn, radar, minSpeed, maxSpeed);
        }

        //武器の初期化
        CmdCreateMainWeapon();
        CmdCreateSubWeapon();

        //初期値保存
        startPos = transform.position;
        startRotate = transform.rotation;

        Debug.Log("End: OnStartLocalPlayer");
    }

    void Awake()
    {
    }

    void Start()
    {
        //配列初期化
        for (int i = 0; i < (int)Weapon.NONE; i++)
        {
            usingWeapons[i] = false;
        }
    }

    #endregion


    void Update()
    {
        if (!isLocalPlayer) return;
        if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない
        if (IsGameOver || syncIsRespawning || syncIsDestroy) return;  //死亡・リスポーン処理中は操作不可

        //デバッグ用
        if (Input.GetKeyDown(KeyCode.Y))
        {
            CmdDamage(10000);
            CmdDamage(100);
        }

        //サブウェポンのUpdate
        if (syncSubWeapon != null)
        {
            //Start系で初期化するとネットワークのラグでウェポンが生成されていないので
            //Update内で初期化
            if (!initSubWeapon)
            {
                syncSubWeapon.GetComponent<BaseWeapon>().Init();
                initSubWeapon = true;
            }
            syncSubWeapon.GetComponent<BaseWeapon>().UpdateMe();
        }


        #region Move

        //移動処理
        //前進
        if (Input.GetKey(KeyCode.W))
        {
            baseAction.Move(moveSpeed, cacheTransform.forward);
            baseAction.CmdRotateDroneObject(frontMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            baseAction.CmdRotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        //左移動
        if (Input.GetKey(KeyCode.A))
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, left);
            baseAction.CmdRotateDroneObject(leftMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            baseAction.CmdRotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        //後退
        if (Input.GetKey(KeyCode.S))
        {
            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, backward);
            baseAction.CmdRotateDroneObject(backMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            baseAction.CmdRotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        //右移動
        if (Input.GetKey(KeyCode.D))
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, right);
            baseAction.CmdRotateDroneObject(rightMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            baseAction.CmdRotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        //上下移動
        if (Input.mouseScrollDelta.y != 0)
        {
            Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
            Vector3 upward = upAngle.normalized * Vector3.forward;
            baseAction.Move(moveSpeed * 4 * Input.mouseScrollDelta.y, upward);
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
                ILockOn l = lockOn;
                l.StartLockOn(lockOnTrackingSpeed);
            }
        }
        //ロックオン解除
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            ILockOn l = lockOn;
            l.ReleaseLockOn();
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
                    IRadar r = radar;
                    r.StartRadar();
                }
            }
        }
        //レーダー終了
        if (Input.GetKeyUp(KeyCode.Q))
        {
            IRadar r = radar;
            r.ReleaseRadar();
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

        //攻撃処理しか使わない簡易メソッド
        Action<float> ModifySpeeds = (x) =>
        {
            moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, x);
            rotateSpeed *= x;
            lockOnTrackingSpeed *= x;
        };

        //メイン武器攻撃
        if (Input.GetMouseButtonDown(0))
        {
            //サブ武器を使用していたら撃てない
            //バグ防止用にメイン武器フラグも調べる
            if (!usingWeapons[(int)Weapon.SUB] && !usingWeapons[(int)Weapon.MAIN])
            {
                //攻撃中は速度低下
                ModifySpeeds(atackingDownSpeed);
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
                ModifySpeeds(1 / atackingDownSpeed);
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
                ModifySpeeds(atackingDownSpeed);
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
                ModifySpeeds(1 / atackingDownSpeed);
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
        foreach(bool use in usingWeapons)
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
                rotateSpeed = initRotateSpeed;
                lockOnTrackingSpeed = initLockOnTrackingSpeed;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        if (syncIsDestroy)
        {
            //加速しながら落ちる
            _rigidbody.AddForce(new Vector3(0, -10 * syncGravityAccele, 0), ForceMode.Acceleration);

            //落下処理を全クライアントに処理
            CmdFallDrone();

            return;
        }
    }


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

        ILockOn l = lockOn;
        bw.Shot(l.Target);
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
        

    //プレイヤーにダメージを与える
    [Command(ignoreAuthority = true)]
    public void CmdDamage(float power)
    {
        if (IsGameOver || syncIsRespawning || syncIsDestroy) return;
        if (nonDamage) return;  //無敵中はダメージ処理をしない

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
                DestroyMe();
            }

            //デバッグ用
            Debug.Log(name + "に" + power + "のダメージ\n残りHP: " + syncHP);
        }
    }


    #region Death

    void DestroyMe()
    {
        syncGravityAccele = 1f;
        syncIsDestroy = true;
        RpcDestroyMe();

        if (syncStock <= 0)
        {
            Invoke(nameof(Death), fallTime);
        }
        else
        {
            syncStock--;
            Invoke(nameof(Respawn), fallTime);
        }
    }

    [ClientRpc]
    void RpcDestroyMe()
    {
        if (isLocalPlayer)
        {
            //死んだのでロックオン・レーダー解除
            lockOn.GetComponent<LockOn>().ReleaseLockOn();
            radar.GetComponent<Radar>().ReleaseRadar();
        }
        PlayOneShotSE(SoundManager.SE.DEATH, SoundManager.BaseSEVolume);
    }

    
    void Death()
    {
        syncIsGameOver = true;
        RpcDeath();
    }

    [ClientRpc]
    void RpcDeath()
    {
        BattleManager.Singleton.SetDestroyedDrone(netId);
        gameObject.SetActive(false);
    }

    //リスポーン処理
    void Respawn()
    {
        //全クライアント用リスポーン処理
        RpcRespawn();

        //HP初期化
        syncHP = MAX_HP;

        //落下停止
        syncIsDestroy = false;

        //一時的に無敵
        nonDamage = true;
        Invoke(nameof(SetNonDamageFalse), nonDamageTime);
    }

    void SetNonDamageFalse()
    {
        nonDamage = false;
    }

    [ClientRpc]
    void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            //移動の初期化
            _rigidbody.velocity = new Vector3(0, 0, 0);
            moveSpeed = initSpeed;

            //初期位置に移動
            cacheTransform.position = startPos;

            //所持アイテム初期化
            itemAction.ResetItem();

            //状態異常初期化
            statusAction.ResetStatus();

            //ブーストゲージ回復
            boostGaugeImage.fillAmount = 1f;
            isBoost = false;

            //サブ武器初期化
            syncSubWeapon.GetComponent<BaseWeapon>().ResetWeapon();

            //バリア復活
            barrierAction.CmdInit();
        }

        //角度の初期化
        cacheTransform.rotation = startRotate;
        baseAction.droneObject.localRotation = Quaternion.identity;
        syncMainWeapon.transform.localRotation = Quaternion.identity;
        syncSubWeapon.transform.localRotation = Quaternion.identity;

        //プロペラ再生
        animator.speed = 1f;

        //リスポーンSE再生
        PlayOneShotSE(SoundManager.SE.RESPAWN, SoundManager.BaseSEVolume);
    }

    [Command]
    void CmdFallDrone()
    {
        RpcFallDrone();
        syncGravityAccele += 20 * Time.deltaTime;
    }

    [ClientRpc]
    void RpcFallDrone()
    {
        //ドローンを傾ける
        baseAction.droneObject.localRotation = Quaternion.Slerp(baseAction.droneObject.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);
        syncMainWeapon.transform.localRotation = Quaternion.Slerp(syncMainWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);
        syncSubWeapon.transform.localRotation = Quaternion.Slerp(syncSubWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

        //プロペラ減速
        animator.speed *= 0.993f;
    }

    #endregion


    //ロックオンしない対象を設定
    public void SetNotLockOnObject(GameObject o)
    {
        ILockOn l = lockOn;
        l.SetNotLockOnObject(o);
    }

    //SetNotLockOnObjectで設定したオブジェクトを解除
    public void UnSetNotLockOnObject(GameObject o)
    {
        ILockOn l = lockOn;
        l.UnSetNotLockOnObject(o);
    }


    //レーダーに照射しない対象を設定
    public void SetNotRadarObject(GameObject o)
    {
        IRadar r = radar;
        r.SetNotRadarObject(o);
    }

    //SetNotRadarObjectで設定したオブジェクトを解除
    public void UnSetNotRadarObject(GameObject o)
    {
        IRadar r = radar;
        r.UnSetNotRadarObject(o);
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

    public void SetCameraDepth(int depth)
    {
        baseAction._Camera.depth = depth;
    }


    private void OnTriggerStay(Collider other)
    {
        if (!isLocalPlayer) return;
        if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない
        if (IsGameOver || syncIsRespawning || syncIsDestroy) return;  //死亡・リスポーン処理中は操作不可

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

    [Command]
    void CmdDestroy(uint netId)
    {
        NetworkServer.Destroy(NetworkIdentity.spawned[netId].gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isLocalPlayer) return;
        if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない
        if (IsGameOver || syncIsRespawning || syncIsDestroy) return;  //死亡・リスポーン処理中は操作不可

        //ステージの見えない壁に当たったらSE
        if (collision.gameObject.CompareTag(TagNameManager.WORLD_WALL))
        {
            PlayOneShotSE(SoundManager.SE.WALL_STUN, SoundManager.BaseSEVolume);
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