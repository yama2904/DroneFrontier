using Cysharp.Threading.Tasks;
using Offline;
using Offline.Player;
using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleDrone : BaseDrone, IBattleDrone
{
    // コンポーネント用
    Transform _transform = null;
    Rigidbody _rigidbody = null;
    Animator _animator = null;
    DroneBaseAction _baseAction = null;
    DroneDamageAction damageAction = null;
    DroneSoundAction soundAction = null;
    DroneLockOnAction lockOnAction = null;
    DroneRadarAction radarAction = null;
    DroneBarrierAction barrierAction = null;
    DroneItemAction itemAction = null;
    DroneStatusAction statusAction = null;

    /// <summary>
    /// ドローンの名前
    /// </summary>
    public string Name { get; set; } = "";

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

    // ドローンが移動した際にオブジェクトが傾く処理用
    float moveRotateSpeed = 2f;
    Quaternion frontMoveRotate = Quaternion.Euler(50, 0, 0);
    Quaternion leftMoveRotate = Quaternion.Euler(0, 0, 60);
    Quaternion rightMoveRotate = Quaternion.Euler(0, 0, -60);
    Quaternion backMoveRotate = Quaternion.Euler(-70, 0, 0);

    // ロックオン
    [SerializeField, Tooltip("ロックオンした際に敵に向く速度")] float lockOnTrackingSpeed = 0.2f;

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
        _rigidbody = GetComponent<Rigidbody>();
        _transform = _rigidbody.transform;
        _animator = GetComponent<Animator>();
        _baseAction = GetComponent<DroneBaseAction>();
        damageAction = GetComponent<DroneDamageAction>();
        soundAction = GetComponent<DroneSoundAction>();
        lockOnAction = GetComponent<DroneLockOnAction>();
        radarAction = GetComponent<DroneRadarAction>();
        barrierAction = GetComponent<DroneBarrierAction>();
        itemAction = GetComponent<DroneItemAction>();
        statusAction = GetComponent<DroneStatusAction>();

        // ストック数初期化
        StockNum = _maxStock;
    }

    protected override void Start()
    {
        base.Start();

        // コンポーネントの初期化
        lockOnAction.Init();
        itemAction.Init((int)ItemNum.NONE);


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
        soundAction.PlayLoopSE(SoundManager.SE.PROPELLER, SoundManager.SEVolume);
    }

    void Update()
    {
        // 死亡処理中は操作不可
        if (_isDestroyFalling || _isDestroy) return;

        if (damageAction.HP <= 0)
        {
            Destroy().Forget();
            return;
        }

        #region Move

        // 前進
        if (Input.GetKey(KeyCode.W))
        {
            _baseAction.Move(_transform.forward);
            _baseAction.RotateDroneObject(frontMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            _baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        // 左移動
        if (Input.GetKey(KeyCode.A))
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * _transform.forward;
            _baseAction.Move(left);
            _baseAction.RotateDroneObject(leftMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            _baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        // 後退
        if (Input.GetKey(KeyCode.S))
        {
            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * _transform.forward;
            _baseAction.Move(backward);
            _baseAction.RotateDroneObject(backMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            _baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        // 右移動
        if (Input.GetKey(KeyCode.D))
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * _transform.forward;
            _baseAction.Move(right);
            _baseAction.RotateDroneObject(rightMoveRotate, moveRotateSpeed * Time.deltaTime);
        }
        else
        {
            _baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed * Time.deltaTime);
        }

        // 上下移動
        if (Input.mouseScrollDelta.y != 0)
        {
            Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
            Vector3 upward = upAngle.normalized * Vector3.forward;
            _baseAction.Move(upward * Input.mouseScrollDelta.y);
        }
        if (Input.GetKey(KeyCode.R))
        {
            Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
            Vector3 upward = upAngle.normalized * Vector3.forward;
            _baseAction.Move(upward);
        }
        if (Input.GetKey(KeyCode.F))
        {
            Quaternion downAngle = Quaternion.Euler(90, 0, 0);
            Vector3 down = downAngle.normalized * Vector3.forward;
            _baseAction.Move(down);
        }

        #endregion

        #region LockOn

        // ロックオン使用
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (!statusAction.GetIsStatus(DroneStatusAction.Status.JAMMING))
            {
                lockOnAction.UseLockOn(lockOnTrackingSpeed);
            }
        }
        // ロックオン解除
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            lockOnAction.StopLockOn();
        }

        #endregion

        #region Radar

        // ジャミング中は処理しない
        if (!statusAction.GetIsStatus(DroneStatusAction.Status.JAMMING))
        {
            // レーダー音の再生
            if (Input.GetKeyDown(KeyCode.Q))
            {
                soundAction.PlayOneShot(SoundManager.SE.RADAR, SoundManager.SEVolume);
            }
            // レーダー使用
            if (Input.GetKey(KeyCode.Q))
            {
                radarAction.UseRadar();
            }
        }
        // レーダー終了
        if (Input.GetKeyUp(KeyCode.Q))
        {
            radarAction.StopRadar();
        }

        #endregion


        // 回転処理
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector3 angle = Vector3.zero;
            angle.x = Input.GetAxis("Mouse X");
            angle.y = Input.GetAxis("Mouse Y");
            _baseAction.Rotate(angle * CameraManager.CameraSpeed);
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
                _baseAction.ModifySpeed(atackingDownSpeed);
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
                _baseAction.ModifySpeed(1 / atackingDownSpeed);
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
                    _baseAction.ModifySpeed(atackingDownSpeed);
                }
                // レーザーの場合は低下率増加
                if (SubWeapon == BaseWeapon.Weapon.LASER)
                {
                    _baseAction.ModifySpeed(atackingDownSpeed * 0.75f);
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
                    _baseAction.ModifySpeed(1 / atackingDownSpeed);
                }
                // レーザーの場合は低下率増加
                if (SubWeapon == BaseWeapon.Weapon.LASER)
                {
                    _baseAction.ModifySpeed(1 / (atackingDownSpeed * 0.75f));
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
                _baseAction.ModifySpeed(boostAccele);
                isBoost = true;

                // 加速音の再生
                boostSoundId = soundAction.PlayLoopSE(SoundManager.SE.BOOST, SoundManager.SEVolume * 0.15f);

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

                    _baseAction.ModifySpeed(1 / boostAccele);
                    isBoost = false;

                    // ブーストSE停止
                    soundAction.StopLoopSE(boostSoundId);


                    // デバッグ用
                    Debug.Log("ブースト終了");
                }
            }
            // キーを離したらブースト停止
            if (Input.GetKeyUp(KeyCode.Space))
            {
                _baseAction.ModifySpeed(1 / boostAccele);
                isBoost = false;

                // ブーストSE停止
                soundAction.StopLoopSE(boostSoundId);


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
            _baseAction.RotateDroneObject(deathRotate, deathRotateSpeed * Time.deltaTime);

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

        // 死んだのでロックオン・レーダー解除
        lockOnAction.StopLockOn();
        radarAction.StopRadar();

        // 死亡SE再生
        soundAction.PlayOneShot(SoundManager.SE.DEATH, SoundManager.SEVolume);

        // 一定時間経過してから爆破
        await UniTask.Delay(TimeSpan.FromSeconds(fallTime));

        // ドローンの非表示
        droneObject.gameObject.SetActive(false);
        barrierAction.BarrierObject.SetActive(false);
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

        bw.Shot(lockOnAction.Target);
    }

    // アイテム使用
    void UseItem(ItemNum item)
    {
        // アイテム枠にアイテムを持っていたら使用
        if (itemAction.UseItem((int)item))
        {
            soundAction.PlayOneShot(SoundManager.SE.USE_ITEM, SoundManager.SEVolume);
        }
    }


    private void OnTriggerStay(Collider other)
    {
        // 死亡処理中は操作不可
        if (_isDestroyFalling || _isDestroy) return;

        // Eキーでアイテム取得
        if (Input.GetKey(KeyCode.E))
        {
            if (other.CompareTag(TagNameManager.ITEM))
            {
                SpawnItem item = other.GetComponent<SpawnItem>();
                if (itemAction.SetItem(item))
                {
                    Destroy(other.gameObject);
                }
            }
        }
    }
}