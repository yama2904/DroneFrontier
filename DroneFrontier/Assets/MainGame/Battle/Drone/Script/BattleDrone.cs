using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mirror;

public class BattleDrone : NetworkBehaviour
{
    const float MAX_HP = 30;
    [SyncVar, SerializeField] float HP = MAX_HP;

    //破壊されたか
    [SyncVar] bool syncIsDestroy = false;
    public bool IsDestroy { get { return syncIsDestroy; } }

    //コンポーネント用
    Transform cacheTransform = null;
    Rigidbody _rigidbody = null;
    Animator animator = null;
    PlayerBaseAction baseAction = null;
    PlayerItemAction itemAction = null;
    PlayerStatusAction statusAction = null;
    bool isStatusActionInit = false;

    //移動用
    [SerializeField, Tooltip("移動速度")] float moveSpeed = 100f;      //移動速度
    float initSpeed = 0;  //移動速度の初期値
    [HideInInspector] float maxSpeed = 100;  //最高速度
    [HideInInspector] float minSpeed = 100;  //最低速度

    //回転用
    [SerializeField, Tooltip("回転速度")] public float rotateSpeed = 5.0f;

    //ドローンが移動した際にオブジェクトが傾く処理用
    float moveRotateSpeed = 2f;
    Quaternion frontMoveRotate = Quaternion.Euler(50, 0, 0);
    Quaternion leftMoveRotate = Quaternion.Euler(0, 0, 60);
    Quaternion rightMoveRotate = Quaternion.Euler(0, 0, -60);
    Quaternion backMoveRotate = Quaternion.Euler(-70, 0, 0);

    //バリア
    [SerializeField] Barrier barrierInspector = null;
    [SyncVar] GameObject barrier = null;

    //ロックオン
    [SerializeField] LockOn lockOn = null;
    [SerializeField, Tooltip("ロックオンした際に敵に向く速度")] float lockOnTrackingSpeed = 0.1f;

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
    [SyncVar] GameObject mainWeapon = null;
    [SyncVar] GameObject subWeapon = null;
    public BaseWeapon.Weapon SetSubWeapon { get; set; } = BaseWeapon.Weapon.LASER;
    bool[] usingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
    [SerializeField, Tooltip("攻撃中の移動速度の低下率")] float atackingDownSpeed = 0.5f;   //攻撃中の移動速度の低下率
    bool initSubWeapon = false;

    //死亡処理用
    Quaternion deathRotate = Quaternion.Euler(28, -28, -28);
    float deathRotateSpeed = 2f;
    [SyncVar] float syncGravityAccele = 1f;
    
    //リスポーン用
    [SyncVar, SerializeField] int syncStock = 2;
    [SerializeField, Tooltip("リポーン時間")] float respawnTime = 5.0f;
    Vector3 startPos;
    Quaternion startRotate;
    [SyncVar] bool syncIsRespawning = false;

    [SyncVar] bool syncFallDrone = false;    //ドローンが落下している時true

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
        BOOST,          //ブースト
        DEATH,          //死亡
        PROPELLER,      //プロペラ
        RADAR,          //レーダー
        RESPAWN,        //リスポーン
        USE_ITEM,       //アイテム使用
        WALL_STUN,      //見えない壁に触れる
        JAMMING,        //ジャミング
        MAGNETIC_AREA,  //磁場エリア内

        NONE
    }
    AudioSource[] audios;


    #region Init

    [Command]
    void CmdCreateMainWeapon()
    {
        GameObject weapon = BaseWeapon.CreateWeapon(gameObject, BaseWeapon.Weapon.GATLING);
        weapon.GetComponent<BaseWeapon>().parentNetId = netId;
        NetworkServer.Spawn(weapon, connectionToClient);
        mainWeapon = weapon;


        Debug.Log("CreateMainWeapon");
    }

    [Command]
    void CmdCreateSubWeapon()
    {
        GameObject weapon = BaseWeapon.CreateWeapon(gameObject, SetSubWeapon);
        weapon.GetComponent<BaseWeapon>().parentNetId = netId;
        NetworkServer.Spawn(weapon, connectionToClient);
        subWeapon = weapon;


        Debug.Log("CreateSubWeapon");
    }

    [Command]
    void CmdCreateBarrier()
    {
        Barrier b = Instantiate(barrierInspector);
        b.syncParentNetId = netId;
        NetworkServer.Spawn(b.gameObject, connectionToClient);
        barrier = b.gameObject;


        Debug.Log("CreateBarrier");
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isServer)
        {
            BattleManager.AddPlayerData(this);
        }

        //AudioSourceの初期化
        audios = GetComponents<AudioSource>();
        audios[(int)SE.BOOST].clip = SoundManager.GetAudioClip(SoundManager.SE.BOOST);
        audios[(int)SE.DEATH].clip = SoundManager.GetAudioClip(SoundManager.SE.DEATH);
        audios[(int)SE.PROPELLER].clip = SoundManager.GetAudioClip(SoundManager.SE.PROPELLER);
        audios[(int)SE.RADAR].clip = SoundManager.GetAudioClip(SoundManager.SE.RADAR);
        audios[(int)SE.RESPAWN].clip = SoundManager.GetAudioClip(SoundManager.SE.RESPAWN);
        audios[(int)SE.USE_ITEM].clip = SoundManager.GetAudioClip(SoundManager.SE.USE_ITEM);
        audios[(int)SE.WALL_STUN].clip = SoundManager.GetAudioClip(SoundManager.SE.WALL_STUN);
        audios[(int)SE.JAMMING].clip = SoundManager.GetAudioClip(SoundManager.SE.JAMMING_NOISE);
        audios[(int)SE.MAGNETIC_AREA].clip = SoundManager.GetAudioClip(SoundManager.SE.MAGNETIC_AREA);

        //プロペラは延々流す
        PlaySE((int)SE.PROPELLER, SoundManager.BaseSEVolume, true);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        boostGaugeImage.enabled = true;
        boostGaugeImage.fillAmount = 1;
        boostGaugeFrameImage.enabled = true;
        itemAction.Init((int)ItemNum.NONE);

        CmdCreateMainWeapon();
        CmdCreateSubWeapon();
        CmdCreateBarrier();

        startPos = transform.position;
        startRotate = transform.rotation;


        Debug.Log("End: OnStartLocalPlayer");
    }

    void Awake()
    {
        //コンポーネントの初期化
        cacheTransform = transform;
        _rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        baseAction = GetComponent<PlayerBaseAction>();
        itemAction = GetComponent<PlayerItemAction>();
        statusAction = GetComponent<PlayerStatusAction>();

        initSpeed = moveSpeed;
        maxSpeed = moveSpeed * 10;
        minSpeed = moveSpeed * 0.2f;
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
        //if (!BattleManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない
        if (syncFallDrone)
        {
            //ドローンを傾ける
            baseAction.droneObject.localRotation = Quaternion.Slerp(baseAction.droneObject.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);
            
            //プロペラ減速
            animator.speed *= 0.993f;  

            //加速しながら落ちる
            _rigidbody.AddForce(new Vector3(0, -10 * syncGravityAccele, 0), ForceMode.Acceleration);
            syncGravityAccele += 10 * Time.deltaTime;

            return;
        }

        //デバッグ用
        if (Input.GetKeyDown(KeyCode.Y))
        {
            CmdDamage(10000);
            CmdDamage(100);
        }
        

        //PlayerStatusActionの初期化
        if (!isStatusActionInit && statusAction != null)
        {
            if (barrier != null && lockOn != null && radar != null)  //エラー防止
            {
                statusAction.Init(barrier.GetComponent<Barrier>(), lockOn, radar, minSpeed, maxSpeed);
                isStatusActionInit = true;
            }
        }

        //サブウェポンのUpdate
        if (subWeapon != null)
        {
            //Start系で初期化するとネットワークのラグでウェポンが生成されていないので
            //Update内で初期化
            if (!initSubWeapon)
            {
                subWeapon.GetComponent<BaseWeapon>().Init();
                initSubWeapon = true;
            }
            subWeapon.GetComponent<BaseWeapon>().UpdateMe();
        }


        #region Move

        //移動処理
        //前進
        if (Input.GetKey(KeyCode.W))
        {
            baseAction.Move(moveSpeed, cacheTransform.forward);
            baseAction.CmdCallRotateDroneObject(frontMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            baseAction.CmdCallRotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        //左移動
        if (Input.GetKey(KeyCode.A))
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, left);
            baseAction.CmdCallRotateDroneObject(leftMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            baseAction.CmdCallRotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        //後退
        if (Input.GetKey(KeyCode.S))
        {
            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, backward);
            baseAction.CmdCallRotateDroneObject(backMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            baseAction.CmdCallRotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        //右移動
        if (Input.GetKey(KeyCode.D))
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, right);
            baseAction.CmdCallRotateDroneObject(rightMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            baseAction.CmdCallRotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
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
            if (!statusAction.GetIsStatus(PlayerStatusAction.Status.JAMMING))
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
        if (!statusAction.GetIsStatus(PlayerStatusAction.Status.JAMMING))
        {
            //レーダー音の再生
            if (Input.GetKeyDown(KeyCode.Q))
            {
                PlaySE((int)SE.RADAR, SoundManager.BaseSEVolume);
            }
            //レーダー使用
            if (Input.GetKey(KeyCode.Q))
            {
                if (!statusAction.GetIsStatus(PlayerStatusAction.Status.JAMMING))
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
                PlaySE((int)SE.BOOST, SoundManager.BaseSEVolume * 0.15f, true);    //加速音の再生


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


        //デバッグ用
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 0.1f);
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 10f);
        }
    }


    //攻撃処理
    void UseWeapon(Weapon weapon)
    {
        GameObject o;
        if (weapon == Weapon.MAIN)
        {
            if (mainWeapon == null)
            {
                return;
            }
            o = mainWeapon;
        }
        else if (weapon == Weapon.SUB)
        {
            if (subWeapon == null)
            {
                return;
            }
            o = subWeapon;
        }
        else
        {
            return;
        }

        ILockOn l = lockOn;
        IWeapon w = o.GetComponent<BaseWeapon>();
        w.Shot(l.Target);
    }

    //アイテム使用
    void UseItem(ItemNum item)
    {
        //アイテム枠1にアイテムを持っていたら使用
        if (itemAction.UseItem((int)item))
        {
            PlaySE((int)SE.USE_ITEM, SoundManager.BaseSEVolume);    //アイテム使用音の再生
        }
    }

    //武器作成
    void SetWeapon(Weapon weapon, BaseWeapon.Weapon weaponType)
    {
        GameObject create = BaseWeapon.CreateWeapon(gameObject, weaponType);
        create.GetComponent<BaseWeapon>().SetChild(cacheTransform);

        if (weapon == Weapon.MAIN)
        {
            mainWeapon = create;
            Debug.Log(mainWeapon);
        }
        else if (weapon == Weapon.SUB)
        {
            subWeapon = create;
        }
        else
        {
            return;
        }
    }


    //プレイヤーにダメージを与える
    [Command(ignoreAuthority = true)]
    public void CmdDamage(float power)
    {
        if (IsDestroy)
        {
            return;
        }

        IBarrier b = barrier.GetComponent<Barrier>();
        float p = Useful.DecimalPointTruncation(power, 1);  //小数点第2以下切り捨て

        //バリアが破壊されていなかったらバリアにダメージを肩代わりさせる
        if (b.HP > 0)
        {
            b.CmdDamage(p);
        }
        //バリアが破壊されていたらドローンが直接ダメージを受ける
        else
        {
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
                CmdDestroyMe();
            }


            //デバッグ用
            Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + HP);
        }
    }


    #region Death

    [Command(ignoreAuthority = true)]
    void CmdDestroyMe()
    {
        barrier.GetComponent<Barrier>().enabled = false;  //死んだらバリア処理をしない
        RpcPlayDeathSE();
        syncGravityAccele = 1f;
        syncFallDrone = true;

        //Updateが呼ばれなくなるので代わりに呼ぶ
        lockOn.GetComponent<LockOn>().ReleaseLockOn();
        radar.GetComponent<Radar>().ReleaseRadar();

        if (syncStock <= 0)
        {
            syncIsDestroy = true;
        }
        else
        {
            syncStock--;
            Invoke(nameof(Respawn), respawnTime);
        }
    }

    [ClientRpc]
    void RpcPlayDeathSE()
    {
        PlaySE((int)SE.DEATH, SoundManager.BaseSEVolume);
    }

    //リスポーン処理
    void Respawn()
    {
        HP = MAX_HP;
        moveSpeed = initSpeed;

        //座標の初期化
        cacheTransform.localRotation = Quaternion.identity;
        cacheTransform.position = startPos;
        cacheTransform.rotation = startRotate;

        //バリア復活
        Barrier b = barrier.GetComponent<Barrier>();
        b.enabled = true;
        b.ResetBarrier();

        //所持アイテム初期化
        itemAction.ResetItem();

        //状態異常初期化
        statusAction.ResetStatus();

        //プロペラ再生
        animator.speed = 1f;

        //ブーストゲージ回復
        boostGaugeImage.fillAmount = 1f;
        isBoost = false;

        //サブ武器初期化
        subWeapon.GetComponent<BaseWeapon>().ResetWeapon();


        _rigidbody.velocity = new Vector3(0, 0, 0);
        RpcPlayRespawnSE();
        syncFallDrone = false;
    }

    [ClientRpc]
    void RpcPlayRespawnSE()
    {
        PlaySE((int)SE.RESPAWN, SoundManager.BaseSEVolume);
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

    //SE再生
    void PlaySE(int index, float volume, bool loop = false)
    {
        if (index >= (int)SE.NONE) return;
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }

        audios[index].volume = volume;
        audios[index].loop = loop;
        audios[index].Play();
    }

    //SE停止
    void StopSE(int index)
    {
        if (index >= (int)SE.NONE) return;
        audios[index].Stop();
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
        PlaySE((int)SE.JAMMING, SoundManager.BaseSEVolume, true);
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
        PlaySE((int)SE.MAGNETIC_AREA, SoundManager.BaseSEVolume, true);
        return statusAction.SetSpeedDown(ref moveSpeed, downPercent);
    }

    //スピードダウン解除
    public void UnSetSpeedDown(int id)
    {
        statusAction.UnSetSpeedDown(ref moveSpeed, id);
        StopSE((int)SE.MAGNETIC_AREA);
    }

    #endregion


    private void OnTriggerStay(Collider other)
    {
        if (syncFallDrone) return;

        //Eキーでアイテム取得
        if (Input.GetKey(KeyCode.E))
        {
            if (other.CompareTag(TagNameManager.ITEM))
            {
                Item item = other.GetComponent<Item>();
                if (itemAction.SetItem(item.type))
                {
                    item.type = Item.ItemType.NONE;  //通信のラグのせいで1つのアイテムを2回取るバグの防止
                    CmdDestroy(item.gameObject);


                    //デバッグ用
                    Debug.Log("アイテム取得");
                }
            }
        }
    }

    #region スポーンせずに元からシーン上に配置しているオブジェクトを削除する用

    [Command(ignoreAuthority = true)]
    void CmdDestroy(GameObject o)
    {
        RpcDestroy(o);
    }

    [ClientRpc]
    void RpcDestroy(GameObject o)
    {
        Destroy(o);
    }

    #endregion
}