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
    /// メイン武器攻撃中であるか
    /// </summary>
    public bool ShootingMainWeapon => _mainShotHistory.CurrentValue || _mainShotHistory.PreviousValue;

    /// <summary>
    /// サブ武器攻撃中であるか
    /// </summary>
    public bool ShootingSubWeapon => _subShotHistory.CurrentValue || _subShotHistory.PreviousValue;

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

    /// <summary>
    /// メイン武器使用履歴
    /// </summary>
    private ValueHistory<bool> _mainShotHistory = new ValueHistory<bool>();

    /// <summary>
    /// サブ武器使用履歴
    /// </summary>
    private ValueHistory<bool> _subShotHistory = new ValueHistory<bool>();

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
        SubWeapon.BulletUICanvas = _bulletUICanvs;
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
            if (!_mainShotHistory.PreviousValue)
            {
                _moveComponent.MoveSpeed *= MainSpeedDownPer;
            }

            // メイン攻撃フラグを立てる
            _mainShotHistory.CurrentValue = true;
        }

        // サブ武器攻撃
        if (weapon == Weapon.SUB)
        {
            SubWeapon.Shot(target);

            // 攻撃中は速度低下
            if (!_subShotHistory.PreviousValue)
            {
                _moveComponent.MoveSpeed *= SubSpeedDownPer;
            }

            // サブ攻撃フラグを立てる
            _subShotHistory.CurrentValue = true;
        }
    }

    private void Awake()
    {
        _moveComponent = GetComponent<DroneMoveComponent>();
    }

    private void LateUpdate()
    {
        // メイン武器の攻撃を停止した場合は速度を戻す
        if (!_mainShotHistory.CurrentValue && _mainShotHistory.PreviousValue)
        {
            _moveComponent.MoveSpeed *= 1 / MainSpeedDownPer;
        }

        // サブ武器の攻撃を停止した場合は速度を戻す
        if (!_subShotHistory.CurrentValue && _subShotHistory.PreviousValue)
        {
            _moveComponent.MoveSpeed *= 1 / SubSpeedDownPer;
        }

        // 武器使用履歴更新
        _mainShotHistory.UpdateCurrentValue(false);
        _subShotHistory.UpdateCurrentValue(false);
    }
}
