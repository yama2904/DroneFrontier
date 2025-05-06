using Common;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Drone.Battle
{
    public class BattleDrone : Drone, IBattleDrone, ILockableOn, IRadarable
    {
        #region public

        public float HP
        {
            get { return _hp; }
            private set
            {
                _hp = value;
                if (value < 0)
                {
                    _hp = 0;
                }
            }
        }

        public IWeapon MainWeapon { get; private set; }

        public IWeapon SubWeapon { get; private set; }

        public int StockNum => _stockNum;

        public Canvas Canvas => _canvas;

        public Canvas BulletCanvas => _bulletCanvas;

        public bool IsLockableOn { get; private set; } = true;

        public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Enemy;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList { get; } = new List<GameObject>();

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        public event EventHandler OnDroneDestroy;

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

        [SerializeField, Tooltip("死亡時に非表示にするオブジェクト")]
        private GameObject[] _destroyHides = null;

        [SerializeField, Tooltip("ドローン死亡時の爆発オブジェクト")]
        private GameObject _explosion = null;

        [SerializeField, Tooltip("ストック数を表示するTextコンポーネント")]
        private Text _stockText = null;

        [SerializeField, Tooltip("オブジェクト探索コンポーネント")]
        private ObjectSearchComponent _searchComponent = null;

        [SerializeField, Tooltip("UI表示用Canvas")]
        private Canvas _canvas = null;

        [SerializeField, Tooltip("弾丸UI表示用Canvas")]
        private Canvas _bulletCanvas = null;

        [SerializeField, Tooltip("ドローンのHP")]
        private float _hp = 100f;

        [SerializeField, Tooltip("ストック数")]
        private int _stockNum = 2;

        /// <summary>
        /// 死亡フラグ
        /// </summary>
        private bool _isDestroy = false;

        // コンポーネントキャッシュ
        private Animator _animator = null;
        private DroneLockOnComponent _lockOnComponent = null;
        private DroneRadarComponent _radarComponent = null;
        private DroneItemComponent _itemComponent = null;
        private DroneWeaponComponent _weaponComponent = null;

        public void Initialize(string name, IWeapon mainWeapon, IWeapon subWeapon, int stock)
        {
            Initialize(name);

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

            // メインウェポン設定
            MainWeapon = mainWeapon;
            MainWeapon.Initialize(gameObject);

            // サブウェポン設定
            SubWeapon = subWeapon;
            SubWeapon.Initialize(gameObject);

            // ストック数設定
            _stockNum = stock;
            _stockText.text = _stockNum.ToString();

            // ロックオン・レーダー不可オブジェクトに自分を設定
            NotLockableOnList.Add(gameObject);
            NotRadarableList.Add(gameObject);

            // オブジェクト探索イベント設定
            _searchComponent.OnObjectStay += OnObjectSearch;

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
            GetComponent<DroneDamageComponent>().Initialize();
            GetComponent<DroneStatusComponent>().Initialize();

            // プロペラ音再生
            _soundComponent.Play(SoundManager.SE.Propeller, -1, true);
        }

        public void Damage(float value)
        {
            // ドローンが破壊されている場合は何もしない
            if (_hp <= 0) return;

            // 小数点第2以下切り捨てでダメージ適用
            HP -= Useful.Floor(value, 1);

            // HPが0になったら破壊処理
            if (_hp <= 0)
            {
                Destroy().Forget();
            }
        }

        protected override void Update()
        {
            // 死亡処理中は操作不可
            if (_isDestroy) return;

            base.Update();

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
                _soundComponent.Play(SoundManager.SE.Radar);
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
                _weaponComponent.Shot(DroneWeaponComponent.Weapon.Main, _lockOnComponent.Target);
            }

            // サブ武器攻撃（メイン武器攻撃中の場合は不可）
            if (_input.MouseButtonR && !_weaponComponent.ShootingMainWeapon)
            {
                _weaponComponent.Shot(DroneWeaponComponent.Weapon.Sub, _lockOnComponent.Target);
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

        protected override void FixedUpdate()
        {
            // 死亡処理
            if (_isDestroy)
            {
                // 加速しながら落ちる
                _rigidbody.AddForce(new Vector3(0, -400, 0), ForceMode.Acceleration);

                // ドローンを傾ける
                _rotateComponent.Rotate(Quaternion.Euler(28, -28, -28), 2 * Time.deltaTime);

                // プロペラ減速
                _animator.speed *= 0.993f;

                return;
            }

            base.FixedUpdate();
        }

        /// <summary>
        /// オブジェクト探索イベント
        /// </summary>
        /// <param name="other">発見オブジェクト</param>
        private void OnObjectSearch(Collider other)
        {
            // 死亡処理中は操作不可
            if (_isDestroy) return;

            // Eキーでアイテム取得
            if (_input.Keys.Contains(KeyCode.E))
            {
                if (other.TryGetComponent(out ISpawnItem item))
                {
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
                _soundComponent.Play(SoundManager.SE.UseItem);
            }
        }

        /// <summary>
        /// 死亡処理
        /// </summary>
        private async UniTask Destroy()
        {
            if (_isDestroy) return;

            // 死亡フラグを立てる
            _isDestroy = true;

            // 移動停止
            _rigidbody.velocity = Vector3.zero;

            // コンポーネント停止
            _moveComponent.enabled = false;
            _boostComponent.enabled = false;
            _lockOnComponent.StopLockOn();
            _radarComponent.StopRadar();

            // 死亡SE再生
            _soundComponent.Play(SoundManager.SE.Death);

            // 一定時間経過してから爆破
            await UniTask.Delay(TimeSpan.FromSeconds(2.5f), ignoreTimeScale: true);

            // ドローンの非表示
            foreach (GameObject obj in _destroyHides)
            {
                obj.SetActive(false);
            }

            // 当たり判定も消す
            GetComponent<Collider>().enabled = false;

            // ロックオン不可
            IsLockableOn = false;

            // Update停止
            enabled = false;

            // 爆破生成
            Instantiate(_explosion, transform);

            // 爆破後一定時間でオブジェクト破棄
            await UniTask.Delay(5000);

            // ドローン破壊イベント通知
            OnDroneDestroy?.Invoke(this, EventArgs.Empty);

            // オブジェクト破棄
            Destroy(gameObject);
        }
    }
}