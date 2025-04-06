using Offline;
using UnityEngine;

public class DroneWeaponComponent : MonoBehaviour, IDroneComponent
{
    public enum Weapon
    {
        /// <summary>
        /// メイン武器
        /// </summary>
        MAIN,

        /// <summary>
        /// サブ武器
        /// </summary>
        SUB,

        NONE
    }

    /// <summary>
    /// メイン武器
    /// </summary>
    public IWeapon MainWeapon { get; private set; } = null;

    /// <summary>
    /// サブ武器
    /// </summary>
    public IWeapon SubWeapon { get; private set; } = null;

    /// <summary>
    /// メイン武器攻撃中のスピード低下率
    /// </summary>
    public float MainSpeedDownPer { get; set; } = 0;

    /// <summary>
    /// サブ武器攻撃中のスピード低下率
    /// </summary>
    public float SubSpeedDownPer { get; set; } = 0;

    /// <summary>
    /// 弾丸UIを非表示にするか
    /// </summary>
    public bool HideBulletUI { get; set; } = false;

    /// <summary>
    /// メイン武器攻撃中であるか
    /// </summary>
    public bool ShootingMainWeapon => _isMainShot[0] || _isMainShot[1];

    /// <summary>
    /// サブ武器攻撃中であるか
    /// </summary>
    public bool ShootingSubWeapon => _isSubAttacked[0] || _isSubAttacked[1];

    /// <summary>
    /// 弾丸イベントハンドラー
    /// </summary>
    /// <param name="component">DroneWeaponComponent</param>
    /// <param name="type">イベント発火した武器の種類</param>
    /// <param name="weapon">イベント発火した武器</param>
    public delegate void BulletEventHandler(DroneWeaponComponent component, Weapon type, IWeapon weapon);

    /// <summary>
    /// 全弾補充イベント
    /// </summary>
    public event BulletEventHandler OnBulletFull;

    /// <summary>
    /// 残弾無しイベント
    /// </summary>
    public event BulletEventHandler OnBulletEmpty;

    /// <summary>
    /// 攻撃中のスピード低下率
    /// </summary>
    private const float SPEED_DOWN_PER = 0.5f;

    /// <summary>
    /// レーザー攻撃中のスピード低下率
    /// </summary>
    private const float LASER_SPEED_DOWN_PER = 0.25f;

    [SerializeField, Tooltip("メイン武器装備位置")]
    private Transform _mainWeaponPos = null;

    [SerializeField, Tooltip("サブ武器装備位置")]
    private Transform _subWeaponPos = null;

    [SerializeField, Tooltip("メイン武器発射位置")]
    private Transform _mainShotPos = null;

    [SerializeField, Tooltip("サブ武器発射位置")]
    private Transform _subShotPos = null;

    [SerializeField, Tooltip("弾丸UI表示Canvas")]
    private Canvas _bulletUICanvs = null;

    /// <summary>
    /// サブ武器の種類
    /// </summary>
    private WeaponType _subWeaponType = WeaponType.NONE;

    /// メイン武器使用履歴<br/>
    /// [0]:現在のフレーム<br/>
    /// [1]:1フレーム前
    /// </summary>
    private bool[] _isMainShot = new bool[2];

    /// <summary>
    /// サブ武器使用履歴<br/>
    /// [0]:現在のフレーム<br/>
    /// [1]:1フレーム前
    /// </summary>
    private bool[] _isSubAttacked = new bool[2];

    // コンポーネントキャッシュ
    DroneMoveComponent _moveComponent = null;

    public void Initialize() 
    {
        // メイン武器読み込み
        GameObject mainWeapon = WeaponCreater.CreateWeapon(WeaponType.GATLING);
        mainWeapon.transform.SetParent(_mainWeaponPos, false);
        MainWeapon = mainWeapon.GetComponent<IWeapon>();
        MainWeapon.Owner = gameObject;
        MainWeapon.ShotPosition = _mainShotPos;
        MainWeapon.OnBulletFull += (o, e) =>
        {
            OnBulletFull?.Invoke(this, Weapon.MAIN, MainWeapon);
        };
        MainWeapon.OnBulletEmpty += (o, e) =>
        {
            OnBulletEmpty?.Invoke(this, Weapon.MAIN, MainWeapon);
        };

        // サブ武器読み込み
        _subWeaponType = GetComponent<IBattleDrone>().SubWeapon;
        GameObject subWeapon = WeaponCreater.CreateWeapon(_subWeaponType);
        subWeapon.transform.SetParent(_subWeaponPos, false);
        SubWeapon = subWeapon.GetComponent<IWeapon>();
        SubWeapon.Owner = gameObject;
        SubWeapon.ShotPosition = _subShotPos;
        SubWeapon.BulletUICanvas = HideBulletUI ? null : _bulletUICanvs;
        SubWeapon.OnBulletFull += (o, e) =>
        {
            OnBulletFull?.Invoke(this, Weapon.SUB, SubWeapon);
        };
        SubWeapon.OnBulletEmpty += (o, e) =>
        {
            OnBulletEmpty?.Invoke(this, Weapon.SUB, SubWeapon);
        };

        // 攻撃中のスピード低下率設定
        MainSpeedDownPer = SPEED_DOWN_PER;
        SubSpeedDownPer = _subWeaponType == WeaponType.LASER ? LASER_SPEED_DOWN_PER : SPEED_DOWN_PER;
    }

    /// <summary>
    /// 武器を使用して弾丸発射
    /// </summary>
    /// <param name="weapon">使用する武器</param>
    /// <param name="target">追従対象</param>
    public void Shot(Weapon weapon, GameObject target = null)
    {
        // メイン武器攻撃
        if (weapon == Weapon.MAIN)
        {
            MainWeapon.Shot(target);

            // 攻撃中は速度低下
            if (!_isMainShot[1])
            {
                _moveComponent.MoveSpeed *= MainSpeedDownPer;
            }

            // メイン攻撃フラグを立てる
            _isMainShot[0] = true;
        }

        // サブ武器攻撃
        if (weapon == Weapon.SUB)
        {
            SubWeapon.Shot(target);

            // 攻撃中は速度低下
            if (!_isSubAttacked[1])
            {
                _moveComponent.MoveSpeed *= SubSpeedDownPer;
            }

            // サブ攻撃フラグを立てる
            _isSubAttacked[0] = true;
        }
    }

    private void Awake()
    {
        _moveComponent = GetComponent<DroneMoveComponent>();
    }

    private void LateUpdate()
    {
        // メイン武器の攻撃を停止した場合は速度を戻す
        if (!_isMainShot[0] && _isMainShot[1])
        {
            _moveComponent.MoveSpeed *= 1 / MainSpeedDownPer;
        }

        // サブ武器の攻撃を停止した場合は速度を戻す
        if (!_isSubAttacked[0] && _isSubAttacked[1])
        {
            _moveComponent.MoveSpeed *= 1 / SubSpeedDownPer;
        }

        // 武器使用履歴更新
        _isMainShot[1] = _isMainShot[0];
        _isMainShot[0] = false;
        _isSubAttacked[1] = _isSubAttacked[0];
        _isSubAttacked[0] = false;
    }
}
