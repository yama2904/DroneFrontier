using Cysharp.Threading.Tasks;
using Offline;
using Offline.Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleDrone : MonoBehaviour, IBattleDrone, ILockableOn, IRadarable
{
    #region public

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

    /// <summary>
    /// ドローンのサブ武器
    /// </summary>
    public WeaponType SubWeapon { get; set; }

    /// <summary>
    /// ロックオン可能であるか
    /// </summary>
    public bool IsLockableOn { get; } = true;

    /// <summary>
    /// ロックオン不可にするオブジェクト
    /// </summary>
    public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

    public IRadarable.ObjectType Type => IRadarable.ObjectType.Enemy;

    public bool IsRadarable => true;

    public List<GameObject> NotRadarableList { get; } = new List<GameObject>();

    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    public event EventHandler DroneDestroyEvent;

    #endregion

    /// <summary>
    /// 所持アイテム番号
    /// </summary>
    private enum ItemNum
    {
        /// <summary>
        /// アイテム1
        /// </summary>
        Item1,

        /// <summary>
        /// アイテム2
        /// </summary>
        Item2
    }

    /// <summary>
    /// 死亡時の回転量
    /// </summary>
    private readonly Quaternion DEATH_ROTATE = Quaternion.Euler(28, -28, -28);

    /// <summary>
    /// 死亡時の回転速度
    /// </summary>
    private const float DEATH_ROTATE_SPEED = 2f;

    /// <summary>
    /// 死亡時の落下時間
    /// </summary>
    private const float DEATH_FALL_TIME = 2.5f;

    [SerializeField, Tooltip("ドローン本体オブジェクト")]
    private Transform _droneObject = null;

    [SerializeField, Tooltip("ドローン死亡時の爆発オブジェクト")]
    private GameObject _explosion = null;

    [SerializeField, Tooltip("ストック数を表示するTextコンポーネント")]
    private Text _stockText = null;

    [SerializeField, Tooltip("オブジェクト探索コンポーネント")]
    private ObjectSearchComponent _searchComponent = null;

    [SerializeField, Tooltip("ドローンのHP")]
    private float _hp = 100f;

    [SerializeField, Tooltip("ストック数")]
    private int _stockNum = 2;

    /// <summary>
    /// 死亡フラグ
    /// </summary>
    private bool _isDestroy = false;

    private InputData _input = new InputData();

    // コンポーネントキャッシュ
    Rigidbody _rigidbody = null;
    Animator _animator = null;
    DroneMoveComponent _moveComponent = null;
    DroneRotateComponent _rotateComponent = null;
    DroneSoundComponent _soundComponent = null;
    DroneLockOnComponent _lockOnComponent = null;
    DroneRadarComponent _radarComponent = null;
    DroneItemComponent _itemComponent = null;
    DroneWeaponComponent _weaponComponent = null;
    DroneBoostComponent _boostComponent = null;

    public void Initialize()
    {
        // コンポーネント初期化
        _moveComponent.Initialize();
        _rotateComponent.Initialize();
        _soundComponent.Initialize();
        _lockOnComponent.Initialize();
        _radarComponent.Initialize();
        _itemComponent.Initialize();
        _weaponComponent.Initialize();
        _boostComponent.Initialize();
        GetComponent<DroneBarrierComponent>().Initialize();
        GetComponent<DroneStatusComponent>().IsPlayer = true;
    }

    private void Awake()
    {
        // コンポーネントの取得
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _moveComponent = GetComponent<DroneMoveComponent>();
        _rotateComponent = GetComponent<DroneRotateComponent>();
        _soundComponent = GetComponent<DroneSoundComponent>();
        _lockOnComponent = GetComponent<DroneLockOnComponent>();
        _radarComponent = GetComponent<DroneRadarComponent>();
        _itemComponent = GetComponent<DroneItemComponent>();
        _weaponComponent = GetComponent<DroneWeaponComponent>();
        _boostComponent = GetComponent<DroneBoostComponent>();

        // ストック数UI初期化
        StockNum = _stockNum;

        // ロックオン・レーダー不可オブジェクトに自分を設定
        NotLockableOnList.Add(gameObject);
        NotRadarableList.Add(gameObject);

        // オブジェクト探索イベント設定
        _searchComponent.ObjectStayEvent += ObjectSearchEvent;
    }

    private void Start()
    {
        // ショットガンの場合はブーストを多少強化する
        if (SubWeapon == WeaponType.SHOTGUN)
        {
            _boostComponent.BoostAccele *= 1.2f;
            _boostComponent.MaxBoostTime *= 1.2f;
            _boostComponent.MaxBoostRecastTime *= 0.8f;
        }

        // プロペラは最初から流す
        _soundComponent.PlayLoopSE(SoundManager.SE.Propeller, SoundManager.MasterSEVolume);
    }

    private void Update()
    {
        // 死亡処理中は操作不可
        if (_isDestroy)
        {
            // 加速しながら落ちる
            _rigidbody.AddForce(new Vector3(0, -400, 0), ForceMode.Acceleration);

            // ドローンを傾ける
            _rotateComponent.Rotate(DEATH_ROTATE, DEATH_ROTATE_SPEED * Time.deltaTime);

            // プロペラ減速
            _animator.speed *= 0.993f;

            return;
        }

        // 入力情報更新
        _input.UpdateInput();

        // ロックオン使用
        if (_input.DownedKeys.Contains(KeyCode.LeftShift))
        {
            _lockOnComponent.StartLockOn();
        }
        // ロックオン解除
        if (_input.UppedKeys.Contains(KeyCode.LeftShift))
        {
            _lockOnComponent.StopLockOn();
        }

        // レーダー使用
        if (_input.DownedKeys.Contains(KeyCode.Q))
        {
            _soundComponent.PlayOneShot(SoundManager.SE.Radar, SoundManager.MasterSEVolume);
            _radarComponent.StartRadar();
        }
        // レーダー終了
        if (_input.UppedKeys.Contains(KeyCode.Q))
        {
            _radarComponent.StopRadar();
        }

        // メイン武器攻撃（サブ武器攻撃中の場合は不可）
        if (_input.MouseButtonL && !_weaponComponent.ShootingSubWeapon)
        {
            _weaponComponent.Shot(DroneWeaponComponent.Weapon.MAIN, _lockOnComponent.Target);
        }

        // サブ武器攻撃（メイン武器攻撃中の場合は不可）
        if (_input.MouseButtonR && !_weaponComponent.ShootingMainWeapon)
        {
            _weaponComponent.Shot(DroneWeaponComponent.Weapon.SUB, _lockOnComponent.Target);
        }

        // ブースト開始
        if (_input.DownedKeys.Contains(KeyCode.Space))
        {
            _boostComponent.StartBoost();
        }
        // ブースト停止
        if (_input.UppedKeys.Contains(KeyCode.Space))
        {
            _boostComponent.StopBoost();
        }

        // アイテム使用
        if (_input.UppedKeys.Contains(KeyCode.Alpha1))
        {
            UseItem(ItemNum.Item1);
        }
        if (_input.UppedKeys.Contains(KeyCode.Alpha2))
        {
            UseItem(ItemNum.Item2);
        }
    }

    private void FixedUpdate()
    {
        // 前進
        if (_input.Keys.Contains(KeyCode.W))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Forward);
        }

        // 左移動
        if (_input.Keys.Contains(KeyCode.A))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Left);
        }

        // 後退
        if (_input.Keys.Contains(KeyCode.S))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Backwad);
        }

        // 右移動
        if (_input.Keys.Contains(KeyCode.D))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Right);
        }

        // 上下移動
        if (_input.MouseScrollDelta != 0)
        {
            if (_input.MouseScrollDelta > 0)
            {
                _moveComponent.Move(DroneMoveComponent.Direction.Up);
            }
            else
            {
                _moveComponent.Move(DroneMoveComponent.Direction.Down);
            }
        }
        if (_input.Keys.Contains(KeyCode.R))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Up);
        }
        if (_input.Keys.Contains(KeyCode.F))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Down);
        }

        // マウスによる向き変更
        _moveComponent.RotateDir(_input.MouseX, _input.MouseY);
    }

    /// <summary>
    /// オブジェクト探索イベント
    /// </summary>
    /// <param name="other">発見オブジェクト</param>
    private void ObjectSearchEvent(Collider other)
    {
        // 死亡処理中は操作不可
        if (_isDestroy) return;

        // Eキーでアイテム取得
        if (_input.Keys.Contains(KeyCode.E))
        {
            if (other.CompareTag(TagNameConst.ITEM))
            {
                ISpawnItem item = other.GetComponent<ISpawnItem>();
                if (_itemComponent.SetItem(item.DroneItem))
                {
                    Destroy(other.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 指定した番号のアイテム使用
    /// </summary>
    /// <param name="item">使用するアイテム番号</param>
    private void UseItem(ItemNum item)
    {
        // アイテム枠にアイテムを持っていたら使用
        if (_itemComponent.UseItem((int)item))
        {
            _soundComponent.PlayOneShot(SoundManager.SE.UseItem, SoundManager.MasterSEVolume);
        }
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    private async UniTask Destroy()
    {
        // 死亡フラグを立てる
        _isDestroy = true;

        // 移動コンポーネント停止
        _moveComponent.enabled = false;

        // ロックオン・レーダー解除
        _lockOnComponent.StopLockOn();
        _radarComponent.StopRadar();

        // 死亡SE再生
        _soundComponent.PlayOneShot(SoundManager.SE.Death, SoundManager.MasterSEVolume);

        // 一定時間経過してから爆破
        await UniTask.Delay(TimeSpan.FromSeconds(DEATH_FALL_TIME), ignoreTimeScale: true);

        // ドローンの非表示
        _droneObject.gameObject.SetActive(false);

        // 当たり判定も消す
        GetComponent<Collider>().enabled = false;

        // 爆破生成
        _explosion.SetActive(true);

        // Update停止
        enabled = false;

        // 爆破後一定時間でオブジェクト破棄
        await UniTask.Delay(5000);

        // ドローン破壊イベント通知
        DroneDestroyEvent?.Invoke(this, EventArgs.Empty);

        // オブジェクト破棄
        Destroy(gameObject);
    }
}