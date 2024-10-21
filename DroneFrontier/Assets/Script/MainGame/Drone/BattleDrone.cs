using Cysharp.Threading.Tasks;
using Offline;
using Offline.Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleDrone : MonoBehaviour, IBattleDrone, ILockableOn
{
    // コンポーネント用
    Transform _transform = null;
    Rigidbody _rigidbody = null;
    Animator _animator = null;
    DroneMoveComponent _moveComponent = null;
    DroneRotateComponent _rotateComponent = null;
    DroneSoundComponent _soundComponent = null;
    DroneLockOnComponent _lockOnComponent = null;
    DroneRadarAction _radarComponent = null;
    DroneItemComponent _itemComponent = null;
    DroneStatusComponent _statusComponent = null;

    /// <summary>
    /// ドローンのゲームオブジェクト
    /// </summary>
    public GameObject GameObject { get; private set; } = null;

    /// <summary>
    /// ドローンの名前
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// ドローンのHP
    /// </summary>
    public float HP
    {
        get { return _hp; }
        set
        {
            if (_hp <= 0) return;

            if (value > 0)
            {
                // 小数点第2以下切り捨て
                _hp = Useful.Floor(value, 1);
            }
            else
            {
                // HPが0になったら破壊処理
                _hp = 0;
                Destroy().Forget();
            }
        }
    }
    private float _hp = 0;

    /// <summary>
    /// 現在のストック数
    /// </summary>
    public int StockNum
    {
        get { return _stockNum; }
        set
        {
            _stockNum = value;
            _stockText.text = value.ToString();
        }
    }
    private int _stockNum = 0;

    /// <summary>
    /// ドローンのサブ武器
    /// </summary>
    public BaseWeapon.Weapon SubWeapon { get; set; } = BaseWeapon.Weapon.SHOTGUN;

    /// <summary>
    /// ロックオン可能であるか
    /// </summary>
    public bool IsLockableOn { get; } = true;

    /// <summary>
    /// ロックオン不可にするオブジェクト
    /// </summary>
    public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

    [SerializeField, Tooltip("ドローンの最大HP")]
    private float _maxHP = 100f;

    // ブースト用
    const float BOOST_POSSIBLE_MIN = 0.2f;  // ブースト可能な最低ゲージ量
    [SerializeField] Image boostGaugeImage = null;   // ブーストのゲージ画像
    [SerializeField, Tooltip("ブーストの加速度")] float boostAccele = 2.1f;  // ブーストの加速度
    [SerializeField, Tooltip("ブースト時間")] float maxBoostTime = 6.0f;     // ブーストできる最大の時間
    [SerializeField, Tooltip("ブーストのリキャスト時間")] float boostRecastTime = 8.0f;  // ブーストのリキャスト時間
    int boostSoundId = -1;
    bool isBoost = false;

    [SerializeField, Tooltip("ストック数")]
    private int _maxStock = 3;

    [SerializeField, Tooltip("ストック数を表示するTextコンポーネント")]
    private Text _stockText = null;

    // 武器
    protected enum Weapon
    {
        MAIN,   // メイン武器
        SUB,    // サブ武器

        NONE
    }
    BaseWeapon mainWeapon = null;
    BaseWeapon subWeapon = null;
    bool[] usingWeapons = new bool[(int)Weapon.NONE];    // 使用中の武器
    [SerializeField, Tooltip("攻撃中の移動速度の低下率")] float atackingDownSpeed = 0.5f;   // 攻撃中の移動速度の低下率

    // 死亡処理用
    [SerializeField] GameObject explosion = null;
    [SerializeField] Transform droneObject = null;
    Quaternion deathRotate = Quaternion.Euler(28, -28, -28);
    float deathRotateSpeed = 2f;
    float gravityAccele = 1f;  // 落下加速用
    float fallTime = 2.5f;     // 死亡後の落下時間

    /// <summary>
    /// 死亡落下中
    /// </summary>
    bool _isDestroyFalling = false;

    /// <summary>
    /// 死亡フラグ
    /// </summary>
    bool _isDestroy = false;


    // アイテム枠
    enum ItemNum
    {
        ITEM_1,   // アイテム枠1
        ITEM_2,   // アイテム枠2

        NONE
    }

    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    public event EventHandler DroneDestroyEvent;

    protected void Awake()
    {
        // コンポーネントの取得
        GameObject = gameObject;
        _rigidbody = GetComponent<Rigidbody>();
        _transform = _rigidbody.transform;
        _animator = GetComponent<Animator>();
        _moveComponent = GetComponent<DroneMoveComponent>();
        _rotateComponent = GetComponent<DroneRotateComponent>();
        _soundComponent = GetComponent<DroneSoundComponent>();
        _lockOnComponent = GetComponent<DroneLockOnComponent>();
        _radarComponent = GetComponent<DroneRadarAction>();
        _itemComponent = GetComponent<DroneItemComponent>();
        _statusComponent = GetComponent<DroneStatusComponent>();

        // HP初期化
        _hp = _maxHP;

        // ストック数初期化
        StockNum = _maxStock;
    }

    private void Start()
    {
        // 武器初期化
        mainWeapon = BaseWeapon.CreateWeapon(this, BaseWeapon.Weapon.GATLING, true);
        mainWeapon.SetParent(transform);
        subWeapon = BaseWeapon.CreateWeapon(this, SubWeapon, true);
        subWeapon.SetParent(transform);

        // ブースト初期化
        boostGaugeImage.enabled = true;
        boostGaugeImage.fillAmount = 1;

        // ショットガンの場合はブーストを多少強化する
        if (SubWeapon == BaseWeapon.Weapon.SHOTGUN)
        {
            boostAccele *= 1.2f;
            maxBoostTime *= 1.2f;
            boostRecastTime *= 0.8f;
        }


        // プロペラは最初から流す
        _soundComponent.PlayLoopSE(SoundManager.SE.PROPELLER, SoundManager.SEVolume);
    }

    private void Update()
    {
        // 死亡処理中は操作不可
        if (_isDestroyFalling || _isDestroy) return;

        #region Move

        // 前進
        if (Input.GetKey(KeyCode.W))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Forward);
        }

        // 左移動
        if (Input.GetKey(KeyCode.A))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Left);
        }

        // 後退
        if (Input.GetKey(KeyCode.S))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Backwad);
        }

        // 右移動
        if (Input.GetKey(KeyCode.D))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Right);
        }

        // 上下移動
        if (Input.mouseScrollDelta.y != 0)
        {
            _moveComponent.Move(_transform.up * Input.mouseScrollDelta.y);
        }
        if (Input.GetKey(KeyCode.R))
        {
            _moveComponent.Move(_transform.up);
        }
        if (Input.GetKey(KeyCode.F))
        {
            _moveComponent.Move(_transform.up * -1);
        }

        #endregion

        #region LockOn

        // ロックオン使用
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (!_statusComponent.GetIsStatus(DroneStatusComponent.Status.JAMMING))
            {
                _lockOnComponent.StartLockOn();
            }
        }
        // ロックオン解除
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            _lockOnComponent.StopLockOn();
        }

        #endregion

        #region Radar

        // ジャミング中は処理しない
        if (!_statusComponent.GetIsStatus(DroneStatusComponent.Status.JAMMING))
        {
            // レーダー音の再生
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _soundComponent.PlayOneShot(SoundManager.SE.RADAR, SoundManager.SEVolume);
            }
            // レーダー使用
            if (Input.GetKey(KeyCode.Q))
            {
                _radarComponent.UseRadar();
            }
        }
        // レーダー終了
        if (Input.GetKeyUp(KeyCode.Q))
        {
            _radarComponent.StopRadar();
        }

        #endregion


        // 回転処理
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float x = Input.GetAxis("Mouse X") * CameraManager.ReverseX * CameraManager.CameraSpeed;
            float y = Input.GetAxis("Mouse Y") * CameraManager.ReverseY * CameraManager.CameraSpeed;
            _moveComponent.RotateCamera(x, y);
        }


        #region Weapon

        // メイン武器攻撃
        if (Input.GetMouseButtonDown(0))
        {
            // サブ武器を使用していたら撃てない
            // バグ防止用にメイン武器フラグも調べる
            if (!usingWeapons[(int)Weapon.SUB] && !usingWeapons[(int)Weapon.MAIN])
            {
                // 攻撃中は速度低下
                _moveComponent.MoveSpeed *= atackingDownSpeed;
                usingWeapons[(int)Weapon.MAIN] = true;
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (usingWeapons[(int)Weapon.MAIN])
            {
                UseWeapon(Weapon.MAIN);     // メインウェポン攻撃
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            // 攻撃を止めたら速度を戻す
            if (usingWeapons[(int)Weapon.MAIN])
            {
                _moveComponent.MoveSpeed *= 1 / atackingDownSpeed;
                usingWeapons[(int)Weapon.MAIN] = false;
            }
        }

        // サブ武器攻撃
        if (Input.GetMouseButtonDown(1))
        {
            // サブ武器を使用していたら撃てない
            // バグ防止用にサブ武器フラグも調べる
            if (!usingWeapons[(int)Weapon.MAIN] && !usingWeapons[(int)Weapon.SUB])
            {
                if (SubWeapon == BaseWeapon.Weapon.MISSILE)
                {
                    // 攻撃中は速度低下
                    _moveComponent.MoveSpeed *= atackingDownSpeed;
                }
                // レーザーの場合は低下率増加
                if (SubWeapon == BaseWeapon.Weapon.LASER)
                {
                    _moveComponent.MoveSpeed *= atackingDownSpeed * 0.75f;
                }
                usingWeapons[(int)Weapon.SUB] = true;
            }
        }
        if (Input.GetMouseButton(1))
        {
            if (usingWeapons[(int)Weapon.SUB])
            {
                UseWeapon(Weapon.SUB);      // サブウェポン攻撃
            }
        }
        if (Input.GetMouseButtonUp(1))
        {
            // 攻撃を止めたら速度を戻す
            if (usingWeapons[(int)Weapon.SUB])
            {
                if (SubWeapon == BaseWeapon.Weapon.MISSILE)
                {
                    // 攻撃中は速度低下
                    _moveComponent.MoveSpeed *= 1 / atackingDownSpeed;
                }
                // レーザーの場合は低下率増加
                if (SubWeapon == BaseWeapon.Weapon.LASER)
                {
                    _moveComponent.MoveSpeed *= 1 / (atackingDownSpeed * 0.75f);
                }
                usingWeapons[(int)Weapon.SUB] = false;
            }
        }

        #endregion

        #region Boost

        // ブースト使用
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // ブーストが使用可能なゲージ量ならブースト使用
            if (boostGaugeImage.fillAmount >= BOOST_POSSIBLE_MIN)
            {
                _moveComponent.MoveSpeed *= boostAccele;
                isBoost = true;

                // 加速音の再生
                boostSoundId = _soundComponent.PlayLoopSE(SoundManager.SE.BOOST, SoundManager.SEVolume * 0.15f);

                // デバッグ用
                Debug.Log("ブースト使用");
            }
        }
        // ブースト使用中の処理
        if (isBoost)
        {
            // キーを押し続けている間はゲージ消費
            if (Input.GetKey(KeyCode.Space))
            {
                boostGaugeImage.fillAmount -= 1.0f / maxBoostTime * Time.deltaTime;

                // ゲージが空になったらブースト停止
                if (boostGaugeImage.fillAmount <= 0)
                {
                    boostGaugeImage.fillAmount = 0;

                    _moveComponent.MoveSpeed *= 1 / boostAccele;
                    isBoost = false;

                    // ブーストSE停止
                    _soundComponent.StopLoopSE(boostSoundId);


                    // デバッグ用
                    Debug.Log("ブースト終了");
                }
            }
            // キーを離したらブースト停止
            if (Input.GetKeyUp(KeyCode.Space))
            {
                _moveComponent.MoveSpeed *= 1 / boostAccele;
                isBoost = false;

                // ブーストSE停止
                _soundComponent.StopLoopSE(boostSoundId);


                // デバッグ用
                Debug.Log("ブースト終了");
            }
        }

        // ブースト未使用時にゲージ回復
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


        // アイテム使用
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
        if (_isDestroyFalling)
        {
            // 加速しながら落ちる
            _rigidbody.AddForce(new Vector3(0, -10 * gravityAccele, 0), ForceMode.Acceleration);
            gravityAccele += 20 * Time.deltaTime;

            // ドローンを傾ける
            _rotateComponent.Rotate(deathRotate, deathRotateSpeed * Time.deltaTime);

            // メイン武器を傾ける
            mainWeapon.transform.localRotation = Quaternion.Slerp(mainWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

            // サブ武器を傾ける
            subWeapon.transform.localRotation = Quaternion.Slerp(subWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

            // プロペラ減速
            _animator.speed *= 0.993f;
        }
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    private async UniTask Destroy()
    {
        gravityAccele = 1f;
        _isDestroyFalling = true;
        _isDestroy = true;

        // 移動コンポーネント停止
        _moveComponent.enabled = false;

        // 死んだのでロックオン・レーダー解除
        _lockOnComponent.StopLockOn();
        _radarComponent.StopRadar();

        // 死亡SE再生
        _soundComponent.PlayOneShot(SoundManager.SE.DEATH, SoundManager.SEVolume);

        // 一定時間経過してから爆破
        await UniTask.Delay(TimeSpan.FromSeconds(fallTime));

        // ドローンの非表示
        droneObject.gameObject.SetActive(false);
        mainWeapon.gameObject.SetActive(false);
        subWeapon.gameObject.SetActive(false);

        // 当たり判定も消す
        GetComponent<Collider>().enabled = false;

        // 爆破生成
        GameObject explosionObj = Instantiate(explosion, _transform);

        // 落下停止
        _isDestroyFalling = false;

        // 爆破後一定時間で復活、又は観戦モード切替
        await UniTask.Delay(5000);

        //// ストックが切れた場合は観戦モード
        //if (StockNum <= 0)
        //{
        //    WatchingGame.StartWatchingGame();
        //}
        //else
        //{


        //    // 復活後も非表示のままとなってしまうのでドローンを再表示
        //    droneObject.gameObject.SetActive(true);
        //    barrierAction.BarrierObject.SetActive(true);
        //    mainWeapon.gameObject.SetActive(true);
        //    subWeapon.gameObject.SetActive(true);

        //    // ToDo:Instantiateせずに初期化すべきか
        //    // 復活処理
        //    GameObject respawnDrone = Instantiate(gameObject, _initPosition, _initRotation);
        //    respawnDrone.GetComponent<BattleDrone>().SetStockNum(StockNum - 1);

        //    // 復活SE再生
        //    respawnDrone.GetComponent<DroneSoundAction>().PlayOneShot(SoundManager.SE.RESPAWN, SoundManager.SEVolume);

        //}

        // ドローン破壊イベント通知
        DroneDestroyEvent?.Invoke(this, EventArgs.Empty);

        Destroy(explosionObj);
        Destroy(gameObject);
    }

    // 攻撃処理
    private void UseWeapon(Weapon weapon)
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

        bw.Shot(_lockOnComponent.Target);
    }

    // アイテム使用
    void UseItem(ItemNum item)
    {
        // アイテム枠にアイテムを持っていたら使用
        if (_itemComponent.UseItem((int)item))
        {
            _soundComponent.PlayOneShot(SoundManager.SE.USE_ITEM, SoundManager.SEVolume);
        }
    }


    private void OnTriggerStay(Collider other)
    {
        // 死亡処理中は操作不可
        if (_isDestroyFalling || _isDestroy) return;

        // Eキーでアイテム取得
        if (Input.GetKey(KeyCode.E))
        {
            if (other.CompareTag(TagNameConst.ITEM))
            {
                SpawnItem item = other.GetComponent<SpawnItem>();
                if (_itemComponent.SetItem(item))
                {
                    Destroy(other.gameObject);
                }
            }
        }
    }
}