using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Mirror;

public abstract class BasePlayer : NetworkBehaviour, IPlayerStatus
{
    public float HP { get; protected set; } = 0; //HP
    protected Rigidbody _Rigidbody = null;
    protected Transform cacheTransform = null;  //キャッシュ用

    //移動用
    public float MoveSpeed = 0;                  //移動速度
    public float MaxSpeed { get; set; } = 0;     //最高速度

    //回転用
    public float RotateSpeed { get; set; } = 3.0f;
    float LimitCameraTiltX { get; set; } = 40.0f;

    //破壊されたか
    public bool IsDestroy { get; private set; } = false;

    //カメラ
    [SerializeField] Camera cameraInspector = null;
    protected Camera _Camera { get; private set; } = null;

    //バリア
    [SerializeField] Barrier barrierInspector = null;
    protected Barrier _Barrier { get; private set; } = null;

    //ロックオン
    [SerializeField] LockOn lockOnInspector = null;
    protected LockOn _LockOn { get; private set; } = null;
    protected float LockOnTrackingSpeed { get; set; } = 0.1f;

    protected float AtackingDownSpeed { get; set; } = 0.5f;   //攻撃中の移動速度の低下率

    //ブースト用
    protected const float BOOST_POSSIBLE_MIN = 0.2f;  //ブースト可能な最低ゲージ量
    protected float boostAccele = 2.0f;      //ブーストの加速度
    protected float maxBoostTime = 5.0f;     //ブーストできる最大の時間
    protected float boostRecastTime = 6.0f;  //ブーストのリキャスト時間
    protected bool isBoost = false;


    //武器
    protected enum Weapon
    {
        MAIN,   //メイン武器
        SUB,    //サブ武器

        NONE
    }
    protected BaseWeapon[] weapons = new BaseWeapon[(int)Weapon.NONE];  //ウェポン群

    //アイテム
    protected enum ItemNum
    {
        ITEM_1,   //アイテム枠1
        ITEM_2,   //アイテム枠2

        NONE
    }
    protected Item[] items = new Item[(int)ItemNum.NONE];


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
    protected bool[] isStatus = new bool[(int)Status.NONE];   //状態異常が付与されているか
    float speedPercent = 1;

    //デバッグ用
    bool isQ = true;


    public override void OnStartClient()
    {
        base.OnStartClient();
        CmdCreateWeapon();
    }

    [Command]
    protected void CmdCreateWeapon()
    {
        GameObject weapon = BaseWeapon.CreateWeapon(gameObject, BaseWeapon.Weapon.GATLING);
        weapon.GetComponent<BaseWeapon>().parentTransform = transform;
        NetworkServer.Spawn(weapon, connectionToClient);
        weapons[(int)Weapon.MAIN] = weapon.GetComponent<BaseWeapon>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _Camera.depth++;
    }

    protected virtual void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        cacheTransform = transform;

        _Camera = cameraInspector;
        _Barrier = barrierInspector;
        _LockOn = lockOnInspector;


        //配列初期化
        for (int i = 0; i < (int)Status.NONE; i++)
        {
            isStatus[i] = false;
        }
    }


    protected virtual void Start() { }
    protected virtual void Update()
    {
        IBarrierStatus b = _Barrier;
        SetStatus(Status.BARRIER_STRENGTH, b.IsStrength);
        SetStatus(Status.BARRIER_WEAK, b.IsWeak);

        if (Input.GetKeyDown(KeyCode.V))
        {
            isQ = !isQ;
            Debug.Log("移動処理切り替え");
        }
    }

    //移動処理
    protected virtual void Move(float speed, float _maxSpeed, Vector3 direction)
    {
        if (!isQ)
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
    protected virtual void Rotate(float valueX, float valueY, float speed)
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

    //ブースト処理
    protected virtual IEnumerator UseBoost(float speedMgnf, float time)
    {
        MoveSpeed *= speedMgnf;
        MaxSpeed *= speedMgnf;

        //time秒後に速度を戻す
        yield return new WaitForSeconds(time);
        MoveSpeed /= speedMgnf;
        MaxSpeed /= speedMgnf;
    }

    //攻撃処理
    protected virtual void UseWeapon(Weapon weapon)
    {
        ILockOn l = _LockOn;
        IWeapon w = weapons[(int)weapon];
        w.Shot(l.Target);
    }

    //アイテム使用
    protected virtual void UseItem(ItemNum item)
    {
        int num = (int)item;  //名前省略

        //アイテム枠1にアイテムを持っていたら使用
        if (items[num] != null)
        {
            items[num].UseItem(this);
        }
    }

    protected virtual void Destroy()
    {
        IsDestroy = true;
        _Barrier.enabled = false;
    }


    //プレイヤーにダメージを与える
    public virtual void Damage(float power)
    {
        if (IsDestroy)
        {
            return;
        }

        IBarrier barrier = _Barrier;
        float p = Useful.DecimalPointTruncation(power, 1);  //小数点第2以下切り捨て

        //バリアが破壊されていなかったらバリアにダメージを肩代わりさせる
        if (barrier.HP > 0)
        {
            barrier.Damage(p);
        }
        //バリアが破壊されていたらドローンが直接ダメージを受ける
        else
        {
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
                Destroy();
            }


            //デバッグ用
            Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + HP);
        }
    }

    //サブウェポンをセットする
    public virtual void SetWeapon(BaseWeapon.Weapon weapon)
    {
        //サブウェポンの処理
        GameObject o = BaseWeapon.CreateWeapon(gameObject, weapon);
        BaseWeapon bw = o.GetComponent<BaseWeapon>();   //名前省略
        bw.SetChild(cacheTransform);
        weapons[(int)Weapon.SUB] = bw;
    }

    //ロックオンしない対象を設定
    public void SetNotLockOnObject(GameObject o)
    {
        ILockOn l = _LockOn;
        l.SetNotLockOnObject(o);
    }

    //SetNotLockOnObjectで設定したオブジェクトを解除
    public void UnSetNotLockOnObject(GameObject o)
    {
        ILockOn l = _LockOn;
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
    public virtual bool SetBarrierStrength(float strengthPercent, float time)
    {
        IBarrier b = _Barrier;
        IBarrierStatus s = _Barrier;

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
    public virtual void SetBarrierWeak()
    {
        IBarrierStatus barrier = _Barrier;
        if (barrier.IsWeak)
        {
            return;
        }
        barrier.BarrierWeak();
        SetStatus(Status.BARRIER_WEAK, true);
    }

    //バリア弱体化解除
    public virtual void UnSetBarrierWeak()
    {
        IBarrierStatus barrier = _Barrier;
        barrier.ReleaseBarrierWeak();
        SetStatus(Status.BARRIER_WEAK, false);
    }

    //ジャミング
    public virtual void SetJamming()
    {
        ILockOn l = _LockOn;
        l.ReleaseLockOn();
        SetStatus(Status.JAMMING, true);
    }

    //ジャミング解除
    public virtual void UnSetJamming()
    {
        SetStatus(Status.JAMMING, false);
    }

    //スタン
    public virtual void SetStun(float time)
    {
        SetStatus(Status.STUN, true);
    }

    //スピードダウン
    public virtual void SetSpeedDown(float downPercent)
    {
        speedPercent *= 1 - downPercent;
        MoveSpeed *= speedPercent;
        MaxSpeed *= speedPercent;

        SetStatus(Status.SPEED_DOWN, true);
    }

    //スピードダウン解除
    public virtual void UnSetSpeedDown()
    {
        MoveSpeed /= speedPercent;
        MaxSpeed /= speedPercent;

        speedPercent = 1;
        SetStatus(Status.SPEED_DOWN, false);
    }
}