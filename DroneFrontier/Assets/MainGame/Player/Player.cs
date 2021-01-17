using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mirror;

public class Player : NetworkBehaviour, IPlayerStatus
{
    public float HP { get; private set; } = 0; //HP
    Rigidbody _Rigidbody = null;
    Transform cacheTransform = null;  //キャッシュ用

    //移動用
    public float MoveSpeed { get; set; } = 0;   //移動速度
    public float MaxSpeed { get; set; } = 0;   //最高速度

    //回転用
    public float RotateSpeed { get; set; } = 3.0f;
    float LimitCameraTiltX { get; set; } = 40.0f;

    //破壊されたか
    public bool IsDestroy { get; private set; } = false;

    //カメラ
    [SerializeField] Camera cameraInspector = null;
    Camera _camera = null;

    //バリア
    [SerializeField] Barrier barrierInspector = null;
    [SyncVar] GameObject barrier = null;

    //ロックオン
    [SerializeField] LockOn lockOnInspector = null;
    LockOn lockOn = null;
    float lockOnTrackingSpeed = 0.1f;

    //レーダー
    [SerializeField] Radar radar = null;

    //ブースト用
    const float BOOST_POSSIBLE_MIN = 0.2f;  //ブースト可能な最低ゲージ量
    Image boostImage = null;       //ブーストのゲージ画像
    float boostAccele = 2.0f;      //ブーストの加速度
    float maxBoostTime = 5.0f;     //ブーストできる最大の時間
    float boostRecastTime = 6.0f;  //ブーストのリキャスト時間
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
    public static BaseWeapon.Weapon SetSubWeapon { private get; set; } = BaseWeapon.Weapon.SHOTGUN;
    bool[] isUsingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
    float atackingDownSpeed = 0.5f;   //攻撃中の移動速度の低下率
    bool initSubWeapon = false;


    //アイテム
    enum ItemNum
    {
        ITEM_1,   //アイテム枠1
        ITEM_2,   //アイテム枠2

        NONE
    }
    Item.ItemType[] items = new Item.ItemType[(int)ItemNum.NONE];


    //弱体や強化などの状態
    public enum Status
    {
        BARRIER_STRENGTH,   //バリア強化
        BARRIER_WEAK,       //バリア弱体化
        STUN,               //スタン
        JAMMING,            //ジャミング
        SPEED_DOWN,         //スピードダウン

        NONE
    }
    bool[] isStatus = new bool[(int)Status.NONE];   //状態異常が付与されているか
    float speedPercent = 1;

    //デバッグ用
    bool isV = true;


    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _camera.depth++;
        CmdCreateMainWeapon();
        CmdCreateSubWeapon();
        CmdCreateBarrier();

        Debug.Log("End: OnStartLocalPlayer");
    }

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
        b.parentNetId = netId;
        NetworkServer.Spawn(b.gameObject, connectionToClient);
        barrier = b.gameObject;


        Debug.Log("CreateBarrier");
    }


    void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        cacheTransform = transform;

        _camera = cameraInspector;
        lockOn = lockOnInspector;


        //配列初期化
        for (int i = 0; i < (int)Status.NONE; i++)
        {
            isStatus[i] = false;
        }
        for (int i = 0; i < (int)ItemNum.NONE; i++)
        {
            items[i] = Item.ItemType.NONE;
        }
    }

    void Start()
    {
        HP = 30;
        MoveSpeed = 20.0f;
        MaxSpeed = 30.0f;

        //配列初期化
        for (int i = 0; i < (int)Weapon.NONE; i++)
        {
            isUsingWeapons[i] = false;
        }

        boostImage = GameObject.Find("BoostGauge").GetComponent<Image>();
        boostImage.fillAmount = 1;
    }


    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        //状態異常処理
        if (barrier != null)
        {
            IBarrierStatus b = barrier.GetComponent<Barrier>();
            SetStatus(Status.BARRIER_STRENGTH, b.IsStrength);
            SetStatus(Status.BARRIER_WEAK, b.IsWeak);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            isV = !isV;
            Debug.Log("移動処理切り替え");
        }

        //サブウェポンのUpdate
        if (subWeapon != null)
        {
            if (!initSubWeapon)
            {
                subWeapon.GetComponent<BaseWeapon>().Init();
                initSubWeapon = true;
            }
            subWeapon.GetComponent<BaseWeapon>().UpdateMe();
        }

        if (MainGameManager.IsCursorLock)
        {
            //回転処理
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            Rotate(x, y, RotateSpeed);
        }

        //移動処理
        if (Input.GetKey(KeyCode.W))
        {
            Move(MoveSpeed, MaxSpeed, cacheTransform.forward);
        }
        if (Input.GetKey(KeyCode.A))
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * cacheTransform.forward;
            Move(MoveSpeed, MaxSpeed, left);
        }
        if (Input.GetKey(KeyCode.S))
        {
            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * cacheTransform.forward;
            Move(MoveSpeed, MaxSpeed, backward);
        }
        if (Input.GetKey(KeyCode.D))
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * cacheTransform.forward;
            Move(MoveSpeed, MaxSpeed, right);
        }
        if (Input.mouseScrollDelta.y != 0)
        {
            Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
            Vector3 upward = upAngle.normalized * Vector3.forward;
            Move(MoveSpeed * 4 * Input.mouseScrollDelta.y, MaxSpeed * 4, upward);
        }


        //ロックオン
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (!isStatus[(int)Status.JAMMING])
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

        //レーダー使用
        if (Input.GetKey(KeyCode.Space))
        {
            if (!isStatus[(int)Status.JAMMING])
            {
                IRadar r = radar;
                r.StartRadar();
            }
        }
        //レーダー使用
        if (Input.GetKeyUp(KeyCode.Space))
        {
            IRadar r = radar;
            r.ReleaseRadar();
        }


        //
        //設定画面中はここより下の処理は行わない
        if (MainGameManager.IsConfig)
        {
            return;
        }
        //
        //


        //攻撃処理しか使わない簡易メソッド
        Action<float> ModifySpeeds = (x) =>
        {
            MoveSpeed *= x;
            RotateSpeed *= x;
            lockOnTrackingSpeed *= x;
        };

        //メイン武器攻撃
        if (Input.GetMouseButtonDown(0))
        {
            //サブ武器を使用していない場合は移動速度と回転速度とロックオンの追従速度を下げる
            if (!isUsingWeapons[(int)Weapon.SUB])
            {
                ModifySpeeds(atackingDownSpeed);
                isUsingWeapons[(int)Weapon.MAIN] = true;
            }
        }
        if (Input.GetMouseButton(0))
        {
            UseWeapon(Weapon.MAIN);     //メインウェポン攻撃
        }
        if (Input.GetMouseButtonUp(0))
        {
            //メインもサブも使用していないなら速度を戻す
            if (!isUsingWeapons[(int)Weapon.SUB])
            {
                ModifySpeeds(1 / atackingDownSpeed);
            }
            isUsingWeapons[(int)Weapon.MAIN] = false;
        }

        //サブ武器攻撃
        if (Input.GetMouseButtonDown(1))
        {
            //メイン武器を使用していない場合は移動速度と回転速度とロックオンの追従速度を下げる
            if (!isUsingWeapons[(int)Weapon.MAIN])
            {
                ModifySpeeds(atackingDownSpeed);
                isUsingWeapons[(int)Weapon.SUB] = true;
            }
        }
        if (Input.GetMouseButton(1))
        {
            UseWeapon(Weapon.SUB);      //サブウェポン攻撃
        }
        //メインもサブも使用していないなら速度を戻す
        if (Input.GetMouseButtonUp(1))
        {
            if (!isUsingWeapons[(int)Weapon.MAIN])
            {
                ModifySpeeds(1 / atackingDownSpeed);
            }
            isUsingWeapons[(int)Weapon.SUB] = false;
        }


        //ブースト使用
        if (Input.GetKeyDown(KeyCode.Q))
        {
            //ブーストが使用可能なゲージ量ならブースト使用
            if (boostImage.fillAmount >= BOOST_POSSIBLE_MIN)
            {
                //バトルモードの場合
                if (MainGameManager.Mode == MainGameManager.GameMode.BATTLE)
                {

                }

                //レースモードの場合
                else if (MainGameManager.Mode == MainGameManager.GameMode.RACE)
                {

                }

                ModifySpeed(boostAccele);
                isBoost = true;


                //デバッグ用
                Debug.Log("ブースト使用");
            }
        }
        //ブースト使用中の処理
        if (isBoost)
        {
            //キーを押し続けている間はゲージ消費
            if (Input.GetKey(KeyCode.Q))
            {
                boostImage.fillAmount -= 1.0f / maxBoostTime * Time.deltaTime;

                //ゲージが空になったらブースト停止
                if (boostImage.fillAmount <= 0)
                {
                    boostImage.fillAmount = 0;

                    ModifySpeed(1 / boostAccele);
                    isBoost = false;


                    //デバッグ用
                    Debug.Log("ブースト終了");
                }
            }
            //キーを離したらブースト停止
            if (Input.GetKeyUp(KeyCode.Q))
            {
                ModifySpeed(1 / boostAccele);
                isBoost = false;


                //デバッグ用
                Debug.Log("ブースト終了");
            }
        }

        //ブースト未使用時にゲージ回復
        if (!isBoost)
        {
            if (boostImage.fillAmount < 1.0f)
            {
                boostImage.fillAmount += 1.0f / boostRecastTime * Time.deltaTime;
                if (boostImage.fillAmount >= 1.0f)
                {
                    boostImage.fillAmount = 1;
                }
            }
        }


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
            lockOnTrackingSpeed *= 0.1f;
            RotateSpeed *= 0.1f;
            MoveSpeed *= 0.1f;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            lockOnTrackingSpeed *= 10;
            RotateSpeed *= 10;
            MoveSpeed *= 10;
        }
    }

    //移動処理
    void Move(float speed, float _maxSpeed, Vector3 direction)
    {
        if (!isV)
        {
            //最大速度に達していなかったら移動処理
            if (_Rigidbody.velocity.sqrMagnitude < Mathf.Pow(_maxSpeed, 2))
            {
                _Rigidbody.AddForce(direction * speed, ForceMode.Force);


                //デバッグ用
                //Debug.Log(Mathf.Pow(_maxSpeed, 2));
            }
        }
        else
        {
            _Rigidbody.AddForce(direction * speed + (direction * speed - _Rigidbody.velocity), ForceMode.Force);
        }


        //デバッグ用
        //Debug.Log(_rigidbody.velocity.sqrMagnitude);
    }

    //回転処理
    void Rotate(float valueX, float valueY, float speed)
    {
        if (MainGameManager.IsCursorLock)
        {
            Vector3 angle = new Vector3(valueX * speed, valueY * speed, 0);

            //カメラの左右回転
            cacheTransform.RotateAround(cacheTransform.position, Vector3.up, angle.x);

            //カメラの上下の回転に制限をかける
            Vector3 localAngle = cacheTransform.localEulerAngles;
            localAngle.x += angle.y * -1;
            if (localAngle.x > LimitCameraTiltX && localAngle.x < 180)
            {
                localAngle.x = LimitCameraTiltX;
            }
            if (localAngle.x < 360 - LimitCameraTiltX && localAngle.x > 180)
            {
                localAngle.x = 360 - LimitCameraTiltX;
            }
            cacheTransform.localEulerAngles = localAngle;
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
        Item.ItemType i = items[(int)item];   //名前省略

        //アイテム枠1にアイテムを持っていたら使用
        if (i != Item.ItemType.NONE)
        {
            Item.UseItem(this, i);
        }
    }

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

    void DestroyMe()
    {
        IsDestroy = true;
        barrier.GetComponent<Barrier>().enabled = false;
    }


    //プレイヤーにダメージを与える
    public void Damage(float power)
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
            b.Damage(p);
        }
        //バリアが破壊されていたらドローンが直接ダメージを受ける
        else
        {
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
                DestroyMe();
            }


            //デバッグ用
            Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + HP);
        }
    }

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


    //指定したプレイヤーの状態を返す
    public bool GetStatus(Status status)
    {
        return isStatus[(int)status];
    }

    //状態を更新
    void SetStatus(Status status, bool flag)
    {
        isStatus[(int)status] = flag;
    }

    //バリア強化
    public bool SetBarrierStrength(float strengthPercent, float time)
    {
        IBarrier b = barrier.GetComponent<Barrier>();
        IBarrierStatus s = barrier.GetComponent<Barrier>();

        //既に強化中なら強化しない
        if (s.IsStrength)
        {
            return false;
        }
        //バリア弱体化中なら強化しない
        if (s.IsWeak)
        {
            return false;
        }
        //バリアが破壊されていたら強化しない
        if (b.HP <= 0)
        {
            return false;
        }

        s.BarrierStrength(strengthPercent, time);
        SetStatus(Status.BARRIER_STRENGTH, true);

        return true;
    }

    //バリア弱体化
    public void SetBarrierWeak()
    {
        IBarrierStatus b = barrier.GetComponent<Barrier>();
        if (b.IsWeak)
        {
            return;
        }
        b.BarrierWeak();
        SetStatus(Status.BARRIER_WEAK, true);
    }

    //バリア弱体化解除
    public void UnSetBarrierWeak()
    {
        IBarrierStatus b = barrier.GetComponent<Barrier>();
        b.ReleaseBarrierWeak();
        SetStatus(Status.BARRIER_WEAK, false);
    }

    //スピードを変更する
    void ModifySpeed(float speedMgnf)
    {
        MoveSpeed *= speedMgnf;
        MaxSpeed *= speedMgnf;
    }

    //ジャミング
    public void SetJamming()
    {
        ILockOn l = lockOn;
        l.ReleaseLockOn();

        IRadar r = radar;
        r.ReleaseRadar();

        SetStatus(Status.JAMMING, true);
    }

    //ジャミング解除
    public void UnSetJamming()
    {
        SetStatus(Status.JAMMING, false);
    }

    //スタン
    public void SetStun(float time)
    {
        SetStatus(Status.STUN, true);
        StunScreenMask.CreateStunMask(time);
    }

    //スピードダウン
    public void SetSpeedDown(float downPercent)
    {
        speedPercent *= 1 - downPercent;
        MoveSpeed *= speedPercent;
        MaxSpeed *= speedPercent;

        SetStatus(Status.SPEED_DOWN, true);
    }

    //スピードダウン解除
    public void UnSetSpeedDown()
    {
        MoveSpeed /= speedPercent;
        MaxSpeed /= speedPercent;

        speedPercent = 1;
        SetStatus(Status.SPEED_DOWN, false);
    }

    private void OnTriggerStay(Collider other)
    {
        //Eキーでアイテム取得
        if (Input.GetKey(KeyCode.E))
        {
            if (other.CompareTag(TagNameManager.ITEM))
            {
                //アイテム所持枠に空きがあるか調べる
                for (int num = 0; num < (int)ItemNum.NONE; num++)
                {
                    //空きがある
                    if (items[num] == Item.ItemType.NONE)
                    {
                        items[num] = other.GetComponent<Item>().type;
                        NetworkServer.Destroy(other.gameObject);  //アイテムを取得したら削除
                        break;
                    }
                }


                //デバッグ用
                Debug.Log("アイテム取得");
            }
        }
    }
}