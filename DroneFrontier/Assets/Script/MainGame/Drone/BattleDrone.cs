using Cysharp.Threading.Tasks;
using Offline;
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

    [SerializeField, Tooltip("攻撃中の移動速度の低下率")] 
    private float _atackingDownSpeed = 0.5f;

    /// <summary>
    /// メイン武器使用履歴<br/>
    /// [0]:現在のフレーム<br/>
    /// [1]:1フレーム前
    /// </summary>
    private bool[] _isMainAttacked = new bool[2];

    /// <summary>
    /// サブ武器使用履歴<br/>
    /// [0]:現在のフレーム<br/>
    /// [1]:1フレーム前
    /// </summary>
    private bool[] _isSubAttacked = new bool[2];

    /// <summary>
    /// 死亡フラグ
    /// </summary>
    private bool _isDestroy = false;

    // コンポーネントキャッシュ
    Transform _transform = null;
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

    private void Awake()
    {
        // コンポーネントの取得
        _rigidbody = GetComponent<Rigidbody>();
        _transform = _rigidbody.transform;
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
        _soundComponent.PlayLoopSE(SoundManager.SE.PROPELLER, SoundManager.SEVolume);
    }

    private void Update()
    {
        // 死亡処理中は操作不可
        if (_isDestroy) return;

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

        // ロックオン使用
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _lockOnComponent.StartLockOn();
        }
        // ロックオン解除
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            _lockOnComponent.StopLockOn();
        }

        // レーダー使用
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _soundComponent.PlayOneShot(SoundManager.SE.RADAR, SoundManager.SEVolume);
            _radarComponent.StartRadar();
        }
        // レーダー終了
        if (Input.GetKeyUp(KeyCode.Q))
        {
            _radarComponent.StopRadar();
        }

        // マウスによるカメラ回転処理
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float x = Input.GetAxis("Mouse X") * CameraManager.ReverseX * CameraManager.CameraSpeed;
            float y = Input.GetAxis("Mouse Y") * CameraManager.ReverseY * CameraManager.CameraSpeed;
            _moveComponent.RotateCamera(x, y);
        }

        // メイン武器攻撃（サブ武器攻撃中の場合は不可）
        if (Input.GetMouseButton(0) && !_isSubAttacked[1])
        {
            // 攻撃中は速度低下
            if (!_isMainAttacked[1])
            {
                _moveComponent.MoveSpeed *= _atackingDownSpeed;
            }

            _weaponComponent.Shot(DroneWeaponComponent.Weapon.MAIN, _lockOnComponent.Target);
            _isMainAttacked[0] = true;
        }

        // サブ武器攻撃（メイン武器攻撃中の場合は不可）
        if (Input.GetMouseButton(1) && !_isMainAttacked[1])
        {
            // 攻撃中は速度低下
            if (!_isSubAttacked[1])
            {
                switch (SubWeapon)
                {
                    case WeaponType.MISSILE:
                        _moveComponent.MoveSpeed *= _atackingDownSpeed;
                        break;

                    case WeaponType.LASER:
                        // レーザーの場合は低下率増加
                        _moveComponent.MoveSpeed *= _atackingDownSpeed * 0.5f;
                        break;
                }
            }

            _weaponComponent.Shot(DroneWeaponComponent.Weapon.SUB, _lockOnComponent.Target);
            _isSubAttacked[0] = true;
        }

        // ブースト使用
        if (Input.GetKey(KeyCode.Space))
        {
            _boostComponent.Boost();
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
        // メイン武器の攻撃を停止した場合は速度を戻す
        if (!_isMainAttacked[0] && _isMainAttacked[1])
        {
            _moveComponent.MoveSpeed *= 1 / _atackingDownSpeed;
        }

        // サブ武器の攻撃を停止した場合は速度を戻す
        if (!_isSubAttacked[0] && _isSubAttacked[1])
        {
            switch (SubWeapon)
            {
                case WeaponType.MISSILE:
                    _moveComponent.MoveSpeed *= 1 / _atackingDownSpeed;
                    break;

                case WeaponType.LASER:
                    _moveComponent.MoveSpeed *= 1 / (_atackingDownSpeed * 0.5f);
                    break;
            }
        }

        // 武器使用履歴更新
        _isMainAttacked[1] = _isMainAttacked[0];
        _isMainAttacked[0] = false;
        _isSubAttacked[1] = _isSubAttacked[0];
        _isSubAttacked[0] = false;
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
                SpawnItem item = other.GetComponent<SpawnItem>();
                if (_itemComponent.SetItem(item))
                {
                    Destroy(other.gameObject);
                }
            }
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
}