using Cysharp.Threading.Tasks;
using Offline;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CpuBattleDrone : MonoBehaviour, IBattleDrone, ILockableOn, IRadarable
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
        set { _stockNum = value; }
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
    /// このドローンを見るか
    /// </summary>
    public bool IsWatch
    {
        get { return _isWatch; }
        set
        {
            _camera.depth = value ? 5 : 0;
            _isWatch = value;
        }
    }
    private bool _isWatch = false;

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

    [SerializeField, Tooltip("オブジェクト探索コンポーネント")]
    private ObjectSearchComponent _searchComponent = null;

    [SerializeField, Tooltip("CPUのカメラ")]
    private Camera _camera = null;

    [SerializeField, Tooltip("ドローンのHP")]
    private float _hp = 100f;

    [SerializeField, Tooltip("ストック数")]
    private int _stockNum = 2;

    /// <summary>
    /// ビットフラグで管理した移動方向（桁：DroneMoveComponent.Direction）
    /// </summary>
    private int _moveDir = 0;

    /// <summary>
    /// 移動中であるか
    /// </summary>
    private bool _isMoving = false;

    /// <summary>
    /// 回転方向<br/>
    /// [0]:左右の回転量<br/>
    /// [1]:上下の回転量
    /// </summary>
    private float[] _rotateDirs = new float[2];

    /// <summary>
    /// 攻撃中の武器
    /// </summary>
    private DroneWeaponComponent.Weapon _useWeapon = DroneWeaponComponent.Weapon.NONE;

    /// <summary>
    /// 死亡フラグ
    /// </summary>
    private bool _isDestroy = false;

    /// <summary>
    /// 探索キャンセルトークン発行クラス
    /// </summary>
    private CancellationTokenSource _searchCancel = new CancellationTokenSource();

    /// <summary>
    /// 攻撃キャンセルトークン発行クラス
    /// </summary>
    private CancellationTokenSource _attackCancel = new CancellationTokenSource();

    /// <summary>
    /// 移動キャンセルトークン発行クラス
    /// </summary>
    private CancellationTokenSource _moveCancel = new CancellationTokenSource();

    // コンポーネントキャッシュ
    Transform _transform = null;
    Rigidbody _rigidbody = null;
    Animator _animator = null;
    DroneMoveComponent _moveComponent = null;
    DroneRotateComponent _rotateComponent = null;
    DroneDamageComponent _damageComponent = null;
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
    }

    private void Awake()
    {
        // コンポーネントの取得
        _rigidbody = GetComponent<Rigidbody>();
        _transform = _rigidbody.transform;
        _animator = GetComponent<Animator>();
        _moveComponent = GetComponent<DroneMoveComponent>();
        _rotateComponent = GetComponent<DroneRotateComponent>();
        _damageComponent = GetComponent<DroneDamageComponent>();
        _soundComponent = GetComponent<DroneSoundComponent>();
        _lockOnComponent = GetComponent<DroneLockOnComponent>();
        _radarComponent = GetComponent<DroneRadarComponent>();
        _itemComponent = GetComponent<DroneItemComponent>();
        _weaponComponent = GetComponent<DroneWeaponComponent>();
        _boostComponent = GetComponent<DroneBoostComponent>();
        
        // ダメージイベント設定
        _damageComponent.DamageEvent += DamageEvent;

        // サブ武器残弾イベント設定
        _weaponComponent.OnBulletFull += OnBulletFull;
        _weaponComponent.OnBulletEmpty += OnBulletEmpty;

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
        _soundComponent.PlayLoopSE(SoundManager.SE.PROPELLER, SoundManager.SEVolume);

        // 常にロックオン処理
        _lockOnComponent.StartLockOn();

        // 常にレーダー処理
        _radarComponent.StartRadar();

        // ロックオンイベント設定
        _lockOnComponent.OnTargetLockOn += OnTargetLockon;
        _lockOnComponent.OnTargetUnLockOn += OnTargetUnLockon;
    }

    private void Update()
    {
        // 死亡処理中は操作不可
        if (_isDestroy) return;

        if (_lockOnComponent.Target == null)
        {
            _moveDir = BitFlagUtil.UpdateFlag(_moveDir, (int)DroneMoveComponent.Direction.Forward, true);

            if (_rotateDirs[0] == 0 && _rotateDirs[1] == 0)
            {
                // 回転量をランダムに決定
                _rotateDirs[0] = UnityEngine.Random.Range(-1f, 1f);
                _rotateDirs[1] = UnityEngine.Random.Range(-1f, 1f);

                // 回転時間をランダムに決定
                int rotateSec = UnityEngine.Random.Range(1, 4);
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(rotateSec), cancellationToken: _searchCancel.Token);
                    _rotateDirs[0] = 0;
                    _rotateDirs[1] = 0;
                });
            }
        }
        else
        {
            // 使用武器をランダムに決定
            if (_useWeapon == DroneWeaponComponent.Weapon.NONE)
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    _useWeapon = DroneWeaponComponent.Weapon.MAIN;
                }
                else
                {
                    _useWeapon = DroneWeaponComponent.Weapon.SUB;
                }

                // 攻撃時間をランダムに決定
                SetWeaopnStopTimer().Forget();
            }

            // 移動方向と移動時間をランダムに決定
            if (!_isMoving)
            {
                float moveMilSec = 0;
                switch (UnityEngine.Random.Range(0, 5))
                {
                    // 左移動
                    case 0:
                        _moveDir = BitFlagUtil.UpdateFlag(_moveDir, (int)DroneMoveComponent.Direction.Left, true);
                        moveMilSec = Useful.RandomByNormalDistribution(1f, 2.5f) * 1000;
                        break;

                    // 右移動
                    case 1:
                        _moveDir = BitFlagUtil.UpdateFlag(_moveDir, (int)DroneMoveComponent.Direction.Right, true);
                        moveMilSec = Useful.RandomByNormalDistribution(1f, 2.5f) * 1000;
                        break;

                    // 上移動
                    case 2:
                        _moveDir = BitFlagUtil.UpdateFlag(_moveDir, (int)DroneMoveComponent.Direction.Up, true);
                        moveMilSec = Useful.RandomByNormalDistribution(1f, 0.8f) * 1000;
                        break;

                    // 下移動
                    case 3:
                        _moveDir = BitFlagUtil.UpdateFlag(_moveDir, (int)DroneMoveComponent.Direction.Down, true);
                        moveMilSec = Useful.RandomByNormalDistribution(1f, 0.8f) * 1000;
                        break;

                    default:
                        _moveDir = 0;
                        moveMilSec = Useful.RandomByNormalDistribution(1f, 1f) * 1000;
                        break;
                }

                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromMilliseconds(moveMilSec), cancellationToken: _moveCancel.Token);
                    _moveDir = 0;
                    _isMoving = false;
                });

                _isMoving = true;
            }

            // ターゲットロックオン中は一定距離まで近づくと離れる
            float distance = SubWeapon == WeaponType.SHOTGUN ? 100f : 250f;
            if (Vector3.Distance(_transform.position, _lockOnComponent.Target.transform.position) < distance)
            {
                _moveDir = BitFlagUtil.UpdateFlag(_moveDir, (int)DroneMoveComponent.Direction.Forward, false);
                _moveDir = BitFlagUtil.UpdateFlag(_moveDir, (int)DroneMoveComponent.Direction.Backwad, true);
            }
            else
            {
                _moveDir = BitFlagUtil.UpdateFlag(_moveDir, (int)DroneMoveComponent.Direction.Backwad, false);
            }
        }

        // アイテム使用
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            UseItem(ItemNum.Item1);
        }
        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            UseItem(ItemNum.Item2);
        }
    }

    private void FixedUpdate()
    {
        if (_isDestroy)
        {
            // 加速しながら落ちる
            _rigidbody.AddForce(new Vector3(0, -400, 0), ForceMode.Acceleration);

            // ドローンを傾ける
            _rotateComponent.Rotate(DEATH_ROTATE, DEATH_ROTATE_SPEED * Time.deltaTime);

            // プロペラ減速
            _animator.speed *= 0.993f;
        }
    }

    private void LateUpdate()
    {
        // 前進
        if (BitFlagUtil.CheckFlag(_moveDir, (int)DroneMoveComponent.Direction.Forward))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Forward);
        }

        // 左移動
        if (BitFlagUtil.CheckFlag(_moveDir, (int)DroneMoveComponent.Direction.Left))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Left);
        }

        // 後退
        if (BitFlagUtil.CheckFlag(_moveDir, (int)DroneMoveComponent.Direction.Backwad))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Backwad);
        }

        // 右移動
        if (BitFlagUtil.CheckFlag(_moveDir, (int)DroneMoveComponent.Direction.Right))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Right);
        }

        // 上下移動
        if (BitFlagUtil.CheckFlag(_moveDir, (int)DroneMoveComponent.Direction.Up))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Up);
        }
        if (BitFlagUtil.CheckFlag(_moveDir, (int)DroneMoveComponent.Direction.Down))
        {
            _moveComponent.Move(DroneMoveComponent.Direction.Down);
        }

        // 回転
        _moveComponent.RotateDir(_rotateDirs[0], _rotateDirs[1]);

        // 攻撃
        if (_useWeapon != DroneWeaponComponent.Weapon.NONE)
        {
            _weaponComponent.Shot(_useWeapon, _lockOnComponent.Target);
        }
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
        if (Input.GetKey(KeyCode.E))
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
    /// ダメージイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="source">ダメージを与えたオブジェクト</param>
    /// <param name="damage">ダメージ量</param>
    public void DamageEvent(DroneDamageComponent sender, GameObject source, float damage)
    {
        if (_lockOnComponent.Target == null)
        {
            _transform.LookAt(source.transform);
        }
    }

    /// <summary>
    /// 全弾補充イベント
    /// </summary>
    /// <param name="component">DroneWeaponComponent</param>
    /// <param name="type">イベント発火した武器の種類</param>
    /// <param name="weapon">イベント発火した武器</param>
    public void OnBulletFull(DroneWeaponComponent component, DroneWeaponComponent.Weapon type, IWeapon weapon)
    {
        // サブ武器以外は何もしない
        if (type != DroneWeaponComponent.Weapon.SUB) return;

        // サブ武器攻撃へ切り替える
        _attackCancel.Cancel();
        _attackCancel = new CancellationTokenSource();
        _useWeapon = DroneWeaponComponent.Weapon.SUB;
        SetWeaopnStopTimer().Forget();
    }

    /// <summary>
    /// 残弾無しイベント
    /// </summary>
    /// <param name="component">DroneWeaponComponent</param>
    /// <param name="type">イベント発火した武器の種類</param>
    /// <param name="weapon">イベント発火した武器</param>
    public void OnBulletEmpty(DroneWeaponComponent component, DroneWeaponComponent.Weapon type, IWeapon weapon)
    {
        // サブ武器以外は何もしない
        if (type != DroneWeaponComponent.Weapon.SUB) return;

        // メイン武器攻撃へ切り替える
        _attackCancel.Cancel();
        _attackCancel = new CancellationTokenSource();
        _useWeapon = DroneWeaponComponent.Weapon.MAIN;
        SetWeaopnStopTimer().Forget();
    }

    /// <summary>
    /// 新規ターゲットロックオンイベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    public void OnTargetLockon(object sender, EventArgs e)
    {
        // 索敵モード解除
        _searchCancel.Cancel();

        // 回転停止
        _rotateDirs[0] = 0;
        _rotateDirs[1] = 0;
    }

    /// <summary>
    /// ターゲットロックオン解除イベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    public void OnTargetUnLockon(object sender, EventArgs e)
    {
        // 攻撃停止
        _attackCancel.Cancel();
        _attackCancel = new CancellationTokenSource();
        _useWeapon = DroneWeaponComponent.Weapon.NONE;

        // 移動停止
        _moveCancel.Cancel();
        _moveCancel = new CancellationTokenSource();
        _moveDir = 0;
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
            _soundComponent.PlayOneShot(SoundManager.SE.USE_ITEM, SoundManager.SEVolume);
        }
    }

    /// <summary>
    /// 攻撃停止タイマーをランダムな時間で設定
    /// </summary>
    /// <returns></returns>
    private async UniTask SetWeaopnStopTimer()
    {
        int attackSec = UnityEngine.Random.Range(5, 11);
        await UniTask.Delay(TimeSpan.FromSeconds(attackSec), cancellationToken: _attackCancel.Token);
        _useWeapon = DroneWeaponComponent.Weapon.NONE;
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

        // 死亡SE再生
        _soundComponent.PlayOneShot(SoundManager.SE.DEATH, SoundManager.SEVolume);

        // 一定時間経過してから爆破
        await UniTask.Delay(TimeSpan.FromSeconds(DEATH_FALL_TIME));

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