using Common;
using UnityEngine;

namespace Drone.Battle
{
    public class DroneWeaponComponent : MonoBehaviour, IDroneComponent
    {
        /// <summary>
        /// 攻撃中のスピード低下率
        /// </summary>
        private const float SPEED_DOWN_PER = 0.5f;

        public enum Weapon
        {
            /// <summary>
            /// メイン武器
            /// </summary>
            Main,

            /// <summary>
            /// サブ武器
            /// </summary>
            Sub,

            None
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
        public float MainSpeedDownPer { get; set; } = SPEED_DOWN_PER;

        /// <summary>
        /// サブ武器攻撃中のスピード低下率
        /// </summary>
        public float SubSpeedDownPer { get; set; } = SPEED_DOWN_PER;

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

        [SerializeField, Tooltip("メイン武器装備位置")]
        private Transform _mainWeaponPos = null;

        [SerializeField, Tooltip("サブ武器装備位置")]
        private Transform _subWeaponPos = null;

        /// <summary>
        /// メイン武器使用履歴
        /// </summary>
        private ValueHistory<bool> _mainShotHistory = new ValueHistory<bool>();

        /// <summary>
        /// サブ武器使用履歴
        /// </summary>
        private ValueHistory<bool> _subShotHistory = new ValueHistory<bool>();

        /// <summary>
        /// 攻撃時に発行された移動速度変更ID
        /// </summary>
        private int _changeSpeedId = -1;

        // コンポーネントキャッシュ
        DroneMoveComponent _moveComponent = null;

        public void Initialize()
        {
            IBattleDrone drone = GetComponent<IBattleDrone>();

            // メイン武器読み込み
            MainWeapon = drone.MainWeapon;
            (MainWeapon as MonoBehaviour).transform.SetParent(_mainWeaponPos, false);
            MainWeapon.OnBulletFull += (o, e) =>
            {
                OnBulletFull?.Invoke(this, Weapon.Main, MainWeapon);
            };
            MainWeapon.OnBulletEmpty += (o, e) =>
            {
                OnBulletEmpty?.Invoke(this, Weapon.Main, MainWeapon);
            };

            // サブ武器読み込み
            SubWeapon = drone.SubWeapon;
            (SubWeapon as MonoBehaviour).transform.SetParent(_subWeaponPos, false);
            SubWeapon.OnBulletFull += (o, e) =>
            {
                OnBulletFull?.Invoke(this, Weapon.Sub, SubWeapon);
            };
            SubWeapon.OnBulletEmpty += (o, e) =>
            {
                OnBulletEmpty?.Invoke(this, Weapon.Sub, SubWeapon);
            };
        }

        /// <summary>
        /// 武器を使用して弾丸発射
        /// </summary>
        /// <param name="weapon">使用する武器</param>
        /// <param name="target">追従対象</param>
        public void Shot(Weapon weapon, GameObject target = null)
        {
            // メイン武器攻撃
            if (weapon == Weapon.Main)
            {
                MainWeapon.Shot(target);

                // 攻撃中は速度低下
                if (!_mainShotHistory.PreviousValue)
                {
                    _changeSpeedId = _moveComponent.ChangeMoveSpeedPercent(MainSpeedDownPer);
                }

                // メイン攻撃フラグを立てる
                _mainShotHistory.CurrentValue = true;
            }

            // サブ武器攻撃
            if (weapon == Weapon.Sub)
            {
                SubWeapon.Shot(target);

                // 攻撃中は速度低下
                if (!_subShotHistory.PreviousValue)
                {
                    _changeSpeedId = _moveComponent.ChangeMoveSpeedPercent(SubSpeedDownPer);
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
                _moveComponent.ResetMoveSpeed(_changeSpeedId);
            }

            // サブ武器の攻撃を停止した場合は速度を戻す
            if (!_subShotHistory.CurrentValue && _subShotHistory.PreviousValue)
            {
                _moveComponent.ResetMoveSpeed(_changeSpeedId);
            }

            // 武器使用履歴更新
            _mainShotHistory.UpdateCurrentValue(false);
            _subShotHistory.UpdateCurrentValue(false);
        }
    }
}