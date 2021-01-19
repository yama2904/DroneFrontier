using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mirror;

public class Player : NetworkBehaviour
{
    [SyncVar] float syncHP = 0;
    public float HP { get { return syncHP; } } //HP
    public bool IsLocalPlayer { get { return isLocalPlayer; } }
    public bool IsDestroy { get; private set; } = false;    //破壊されたか

    //コンポーネント用
    Rigidbody _Rigidbody = null;
    Transform cacheTransform = null;  //キャッシュ用
    PlayerItemAction itemAction = null;
    PlayerStatusAction statusAction = null;
    bool isPlayerStatusInit = false;

    //移動用
    float moveSpeed = 0;      //移動速度
    float maxSpeed = 0;       //最高速度
    float minSpeed = 0;       //最低速度

    //回転用
    float rotateSpeed = 4.0f;
    float LimitCameraTiltX { get; set; } = 40.0f;

    //カメラ
    [SerializeField] Camera cameraInspector = null;
    Camera _camera = null;

    //バリア
    [SerializeField] Barrier barrierInspector = null;
    [SyncVar] GameObject barrier = null;

    //ロックオン
    [SerializeField] LockOn lockOn = null;
    float lockOnTrackingSpeed = 0.1f;

    //レーダー
    [SerializeField] Radar radar = null;

    //ブースト用
    const float BOOST_POSSIBLE_MIN = 0.2f;  //ブースト可能な最低ゲージ量
    [SerializeField] Image boostImage = null;       //ブーストのゲージ画像
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
    bool[] usingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
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


    //デバッグ用
    bool isV = true;


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


    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _camera.depth++;
        boostImage.enabled = true;
        boostImage.fillAmount = 1;

        CmdCreateMainWeapon();
        CmdCreateSubWeapon();
        CmdCreateBarrier();

        Debug.Log("End: OnStartLocalPlayer");
    }

    void Awake()
    {
        cacheTransform = transform; //キャッシュ用

        //コンポーネントの初期化
        _Rigidbody = GetComponent<Rigidbody>();
        itemAction = GetComponent<PlayerItemAction>();
        statusAction = GetComponent<PlayerStatusAction>();
        _camera = cameraInspector;

        for (int i = 0; i < (int)ItemNum.NONE; i++)
        {
            items[i] = Item.ItemType.NONE;
        }
    }

    void Start()
    {
        //パラメータ初期化
        syncHP = 30;
        moveSpeed = 20.0f;
        maxSpeed = moveSpeed * 2;
        minSpeed = moveSpeed * 0.3f;

        //配列初期化
        for (int i = 0; i < (int)Weapon.NONE; i++)
        {
            usingWeapons[i] = false;
        }
    }


    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        //PlayerStatusActionの初期化
        if (!isPlayerStatusInit)
        {
            if (barrier != null && lockOn != null && radar != null)  //エラー防止
            {
                statusAction.Init(barrier.GetComponent<Barrier>(), lockOn, radar, minSpeed, maxSpeed);
                isPlayerStatusInit = true;
            }
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
            Rotate(x, y, rotateSpeed * CameraManager.CameraSpeed);
        }

        //移動処理
        if (Input.GetKey(KeyCode.W))
        {
            Move(moveSpeed, maxSpeed, cacheTransform.forward);
        }
        if (Input.GetKey(KeyCode.A))
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * cacheTransform.forward;
            Move(moveSpeed, maxSpeed, left);
        }
        if (Input.GetKey(KeyCode.S))
        {
            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * cacheTransform.forward;
            Move(moveSpeed, maxSpeed, backward);
        }
        if (Input.GetKey(KeyCode.D))
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * cacheTransform.forward;
            Move(moveSpeed, maxSpeed, right);
        }
        if (Input.mouseScrollDelta.y != 0)
        {
            Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
            Vector3 upward = upAngle.normalized * Vector3.forward;
            Move(moveSpeed * 4 * Input.mouseScrollDelta.y, maxSpeed * 4, upward);
        }


        //ロックオン
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

        //レーダー使用
        if (Input.GetKey(KeyCode.Space))
        {
            if (!statusAction.GetIsStatus(PlayerStatusAction.Status.JAMMING))
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
            ModifySpeed(x);
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
            rotateSpeed *= 0.1f;
            ModifySpeed(0.1f);
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            lockOnTrackingSpeed *= 10;
            rotateSpeed *= 10;
            ModifySpeed(10f);
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
            Vector3 angle = new Vector3(valueX * speed * CameraManager.ReverseX, valueY * speed * CameraManager.ReverseY, 0);

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
        Item.ItemType t = items[(int)item];   //名前省略

        //アイテム枠1にアイテムを持っていたら使用
        if (t != Item.ItemType.NONE)
        {
            if (itemAction.UseItem(t))
            {
                items[(int)item] = Item.ItemType.NONE;
            }
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
            syncHP -= p;
            if (syncHP < 0)
            {
                syncHP = 0;
                DestroyMe();
            }


            //デバッグ用
            Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + syncHP);
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


    //////////////////////////////////////////////////////// 
    //状態異常系メソッド
    //////////////////////////////////////////////////////// 

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

    //スピードを変更する
    void ModifySpeed(float speedMgnf)
    {
        moveSpeed *= speedMgnf;
        if (moveSpeed > maxSpeed)
        {
            moveSpeed = maxSpeed;
        }
        if (moveSpeed < minSpeed)
        {
            moveSpeed = minSpeed;
        }
    }

    //ジャミング
    public void SetJamming()
    {
        statusAction.SetJamming();
    }

    //ジャミング解除
    public void UnSetJamming()
    {
        statusAction.UnSetJamming();
    }

    //スタン
    public void SetStun(float time)
    {
        statusAction.SetStun(time);
    }

    //スピードダウン
    public int SetSpeedDown(float downPercent)
    {
        return statusAction.SetSpeedDown(ref moveSpeed, downPercent);
    }


    //スピードダウン解除
    public void UnSetSpeedDown(int id)
    {
        statusAction.UnSetSpeedDown(ref moveSpeed, id);
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
                        Item item = other.GetComponent<Item>();
                        if (item.type == Item.ItemType.NONE) continue;

                        items[num] = item.type;
                        item.type = Item.ItemType.NONE;  //通信のラグのせいで1つのアイテムを2回とるバグの防止
                        CmdDestroy(other.gameObject);


                        //デバッグ用
                        Debug.Log("アイテム取得");

                        break;
                    }
                }
            }
        }
    }

    //スポーンせずに元からシーン上に配置しているオブジェクトを削除する用
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
}