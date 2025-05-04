using Battle.Weapon;
using Common;
using Cysharp.Threading.Tasks;
using Drone;
using Drone.Battle;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Drone
{
    public class CpuBattleDrone : MonoBehaviour, IBattleDrone, ILockableOn, IRadarable
    {
        #region public

        public string Name { get; private set; } = "";

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
        /// このドローンを見るか
        /// </summary>
        public bool IsWatch
        {
            get { return _isWatch; }
            set
            {
                _camera.depth = value ? 5 : 0;
                _listener.enabled = value;
                _canvas.enabled = value;
                _isWatch = value;
            }
        }
        private bool _isWatch = false;

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

        [SerializeField, Tooltip("ドローン本体オブジェクト")]
        private Transform _droneObject = null;

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
        private DroneWeaponComponent.Weapon _useWeapon = DroneWeaponComponent.Weapon.None;

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
        private Transform _transform = null;
        private Rigidbody _rigidbody = null;
        private Animator _animator = null;
        private AudioListener _listener = null;
        private DroneMoveComponent _moveComponent = null;
        private DroneRotateComponent _rotateComponent = null;
        private DroneDamageComponent _damageComponent = null;
        private DroneSoundComponent _soundComponent = null;
        private DroneLockOnComponent _lockOnComponent = null;
        private DroneRadarComponent _radarComponent = null;
        private DroneItemComponent _itemComponent = null;
        private DroneWeaponComponent _weaponComponent = null;
        private DroneBoostComponent _boostComponent = null;

        public void Initialize(string name, IWeapon mainWeapon, IWeapon subWeapon, int stock)
        {
            // ドローン名設定
            Name = name;

            // メインウェポン設定
            MainWeapon = mainWeapon;
            MainWeapon.Initialize(gameObject);

            // サブウェポン設定
            SubWeapon = subWeapon;
            SubWeapon.Initialize(gameObject);

            // ストック数設定
            _stockNum = stock;
            _stockText.text = _stockNum.ToString();

            // ダメージイベント設定
            _damageComponent.OnDamage += OnDamage;

            // サブ武器残弾イベント設定
            _weaponComponent.OnBulletFull += OnBulletFull;
            _weaponComponent.OnBulletEmpty += OnBulletEmpty;

            // ロックオン・レーダー不可オブジェクトに自分を設定
            NotLockableOnList.Add(gameObject);
            NotRadarableList.Add(gameObject);

            // オブジェクト探索イベント設定
            _searchComponent.OnObjectStay += OnObjectSearch;

            // コンポーネント初期化
            _moveComponent.Initialize();
            _rotateComponent.Initialize();
            _damageComponent.Initialize();
            _soundComponent.Initialize();
            _lockOnComponent.Initialize();
            _radarComponent.Initialize();
            _itemComponent.Initialize();
            _weaponComponent.Initialize();
            _boostComponent.Initialize();
            GetComponent<DroneBarrierComponent>().Initialize();

            // プロペラ音再生
            _soundComponent.Play(SoundManager.SE.Propeller, -1, true);

            // 常にロックオン処理
            _lockOnComponent.StartLockOn();

            // 常にレーダー処理
            _radarComponent.StartRadar();

            // ロックオンイベント設定
            _lockOnComponent.OnTargetLockOn += OnTargetLockOn;
            _lockOnComponent.OnTargetUnLockOn += OnTargetUnLockOn;
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

        private void Awake()
        {
            // コンポーネントの取得
            _rigidbody = GetComponent<Rigidbody>();
            _transform = _rigidbody.transform;
            _animator = GetComponent<Animator>();
            _listener = GetComponent<AudioListener>();
            _moveComponent = GetComponent<DroneMoveComponent>();
            _rotateComponent = GetComponent<DroneRotateComponent>();
            _damageComponent = GetComponent<DroneDamageComponent>();
            _soundComponent = GetComponent<DroneSoundComponent>();
            _lockOnComponent = GetComponent<DroneLockOnComponent>();
            _radarComponent = GetComponent<DroneRadarComponent>();
            _itemComponent = GetComponent<DroneItemComponent>();
            _weaponComponent = GetComponent<DroneWeaponComponent>();
            _boostComponent = GetComponent<DroneBoostComponent>();
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
                if (_useWeapon == DroneWeaponComponent.Weapon.None)
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        _useWeapon = DroneWeaponComponent.Weapon.Main;
                    }
                    else
                    {
                        _useWeapon = DroneWeaponComponent.Weapon.Sub;
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
                float distance = SubWeapon is ShotgunWeapon ? 100f : 250f;
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
                _rotateComponent.Rotate(Quaternion.Euler(28, -28, -28), 2 * Time.deltaTime);

                // プロペラ減速
                _animator.speed *= 0.993f;
            }
        }

        private void LateUpdate()
        {
            // 死亡処理中は操作不可
            if (_isDestroy) return;

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
            if (_useWeapon != DroneWeaponComponent.Weapon.None)
            {
                _weaponComponent.Shot(_useWeapon, _lockOnComponent.Target);
            }
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
        public void OnDamage(IDamageable sender, GameObject source, float damage)
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
            if (type != DroneWeaponComponent.Weapon.Sub) return;

            // サブ武器攻撃へ切り替える
            _attackCancel.Cancel();
            _attackCancel = new CancellationTokenSource();
            _useWeapon = DroneWeaponComponent.Weapon.Sub;
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
            if (type != DroneWeaponComponent.Weapon.Sub) return;

            // メイン武器攻撃へ切り替える
            _attackCancel.Cancel();
            _attackCancel = new CancellationTokenSource();
            _useWeapon = DroneWeaponComponent.Weapon.Main;
            SetWeaopnStopTimer().Forget();
        }

        /// <summary>
        /// 新規ターゲットロックオンイベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        public void OnTargetLockOn(object sender, EventArgs e)
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
        public void OnTargetUnLockOn(object sender, EventArgs e)
        {
            // 攻撃停止
            _attackCancel.Cancel();
            _attackCancel = new CancellationTokenSource();
            _useWeapon = DroneWeaponComponent.Weapon.None;

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
                _soundComponent.Play(SoundManager.SE.UseItem);
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
            _useWeapon = DroneWeaponComponent.Weapon.None;
        }

        /// <summary>
        /// 死亡処理
        /// </summary>
        private async UniTask Destroy()
        {
            // 死亡フラグを立てる
            _isDestroy = true;

            // 移動停止
            _rigidbody.velocity = Vector3.zero;

            // 移動コンポーネント停止
            _moveComponent.enabled = false;

            // ロックオン・レーダー解除
            _lockOnComponent.StopLockOn();

            // ロックオン不可に設定
            IsLockableOn = false;

            // 死亡SE再生
            _soundComponent.Play(SoundManager.SE.Death);

            // 一定時間経過してから爆破
            await UniTask.Delay(TimeSpan.FromSeconds(2.5f));

            // ドローンの非表示
            foreach (GameObject obj in _destroyHides)
            {
                obj.SetActive(false);
            }

            // 当たり判定も消す
            GetComponent<Collider>().enabled = false;

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