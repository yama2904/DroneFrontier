using Common;
using Cysharp.Threading.Tasks;
using Drone.Network;
using Network;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Drone.Battle.Network
{
    public class NetworkBattleDrone : NetworkDrone, IBattleDrone, ILockableOn, IRadarable
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

        public bool IsLockableOn { get; } = true;

        public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Enemy;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList { get; } = new List<GameObject>();

        /// <summary>
        /// リスポーンしたか
        /// </summary>
        public bool IsRespawn { get; set; } = false;

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

        [SerializeField, Tooltip("ドローン死亡時の爆発オブジェクト")]
        private GameObject _explosion = null;

        [SerializeField, Tooltip("ストック数を表示するTextコンポーネント")]
        private Text _stockText = null;

        [SerializeField, Tooltip("オブジェクト探索コンポーネント")]
        private ObjectSearchComponent _searchComponent = null;

        [SerializeField, Tooltip("弾丸UI表示用Canvas")]
        private Canvas _bulletCanvas = null;

        [SerializeField, Tooltip("ドローンのHP")]
        private float _hp = 100f;

        [SerializeField, Tooltip("ストック数")]
        private int _stockNum = 2;

        [SerializeField, Tooltip("ステータス同期間隔（秒）")]
        private int _syncStatusInterval = 1;

        /// <summary>
        /// 死亡時に発行するキャンセル
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// 死亡フラグ
        /// </summary>
        private bool _isDestroy = false;

        private readonly object _lock = new object();

        // コンポーネントキャッシュ
        private Animator _animator = null;
        private DroneLockOnComponent _lockOnComponent = null;
        private DroneRadarComponent _radarComponent = null;
        private DroneItemComponent _itemComponent = null;
        private DroneWeaponComponent _weaponComponent = null;
        private DroneBarrierComponent _barrierComponent = null;

        public override string GetAddressKey()
        {
            return "NetworkBattleDrone";
        }

        public override object CreateSpawnData()
        {
            return new Dictionary<string, object>()
            {
                { "Name", Name },
                { "MainWeapon", MainWeapon.GetAddressKey() },
                { "SubWeapon", SubWeapon.GetAddressKey() },
                { "Stock", StockNum },
                { "enabled", enabled },
                { "IsRespawn", IsRespawn }
            };
        }

        public override void ImportSpawnData(object data)
        {
            var dic = data as Dictionary<string, object>;
            Name = (string)dic["Name"];
            MainWeapon = Addressables.InstantiateAsync((string)dic["MainWeapon"]).WaitForCompletion().GetComponent<IWeapon>();
            SubWeapon = Addressables.InstantiateAsync((string)dic["SubWeapon"]).WaitForCompletion().GetComponent<IWeapon>();
            _stockNum = Convert.ToInt32(dic["Stock"]);
            enabled = Convert.ToBoolean(dic["enabled"]);
            IsRespawn = Convert.ToBoolean(dic["IsRespawn"]);
        }

        public override void InitializeSpawn()
        {
            Initialize(Name, MainWeapon, SubWeapon, StockNum);
        }

        public void Initialize(string name, IWeapon mainWeapon, IWeapon subWeapon, int stock)
        {
            base.Initialize(name);

            // メインウェポン設定
            MainWeapon = mainWeapon;
            MainWeapon.Initialize(gameObject);

            // サブウェポン設定
            SubWeapon = subWeapon;
            SubWeapon.Initialize(gameObject);

            // ストック数設定
            _stockNum = stock;
            _stockText.text = _stockNum.ToString();

            // コンポーネントの取得
            _animator = GetComponent<Animator>();
            _lockOnComponent = GetComponent<DroneLockOnComponent>();
            _radarComponent = GetComponent<DroneRadarComponent>();
            _itemComponent = GetComponent<DroneItemComponent>();
            _weaponComponent = GetComponent<DroneWeaponComponent>();
            _barrierComponent = GetComponent<DroneBarrierComponent>();

            // ロックオン・レーダー不可オブジェクトに自分を設定
            NotLockableOnList.Add(gameObject);
            NotRadarableList.Add(gameObject);

            // バリアイベント設定
            _barrierComponent.OnBarrierBreak += OnBarrierBreak;
            _barrierComponent.OnBarrierResurrect += OnBarrierResurrect;

            // オブジェクト探索イベント設定
            _searchComponent.OnObjectStay += OnObjectSearch;

            // イベント受信イベント設定
            NetworkManager.OnUdpReceivedOnMainThread += OnReceiveUdpOfEvent;

            // 自プレイヤーの場合は定期的にステータス同期
            if (IsControl)
            {
                UniTask.Void(async () =>
                {
                    while (true)
                    {
                        await UniTask.Delay(_syncStatusInterval * 1000, ignoreTimeScale: true, cancellationToken: _cancel.Token);
                        float moveSpeed = _moveComponent.MoveSpeed;
                        NetworkManager.SendUdpToAll(new DroneStatusPacket(HP, moveSpeed));
                    }
                });
            }

            // コンポーネント初期化
            _lockOnComponent.Initialize();
            _radarComponent.Initialize();
            _itemComponent.Initialize();
            _weaponComponent.Initialize();
            _barrierComponent.Initialize();
            GetComponent<DroneStatusComponent>().IsPlayer = IsControl;

            // リスポーンした場合は復活SE再生
            if (IsRespawn)
            {
                _soundComponent.Play(SoundManager.SE.Respawn);
            }
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

            if (IsControl)
            {
                bool sendPacket = false;

                bool startLockOn = false;
                bool stopLockOn = false;
                bool useItem1 = false;
                bool useItem2 = false;

                // ロックオン使用
                if (_input.DownedKeys.Contains(KeyCode.LeftShift))
                {
                    _lockOnComponent.StartLockOn();
                    startLockOn = true;
                    sendPacket = true;
                }
                // ロックオン解除
                if (_input.UppedKeys.Contains(KeyCode.LeftShift))
                {
                    _lockOnComponent.StopLockOn();
                    stopLockOn = true;
                    sendPacket = true;
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

                // アイテム使用
                if (_input.UppedKeys.Contains(KeyCode.Alpha1))
                {
                    UseItem(ItemNum.Item1);
                    useItem1 = true;
                    sendPacket = true;
                }
                if (_input.UppedKeys.Contains(KeyCode.Alpha2))
                {
                    UseItem(ItemNum.Item2);
                    useItem2 = true;
                    sendPacket = true;
                }

                // アクション情報送信
                if (sendPacket)
                    NetworkManager.SendUdpToAll(new DroneActionPacket(startLockOn, stopLockOn, useItem1, useItem2));
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // イベント削除
            _barrierComponent.OnBarrierBreak -= OnBarrierBreak;
            _barrierComponent.OnBarrierResurrect -= OnBarrierResurrect;
            _searchComponent.OnObjectStay -= OnObjectSearch;
            NetworkManager.OnUdpReceivedOnMainThread -= OnReceiveUdpOfEvent;

            // キャンセル発行
            _cancel.Cancel();
        }

        /// <summary>
        /// バリア破壊イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnBarrierBreak(object sender, EventArgs e)
        {
            NetworkManager.SendUdpToAll(new DroneEventPacket(Name, true, false, false));
        }

        /// <summary>
        /// バリア復活イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnBarrierResurrect(object sender, EventArgs e)
        {
            NetworkManager.SendUdpToAll(new DroneEventPacket(Name, false, true, false));
        }

        /// <summary>
        /// オブジェクト探索イベント
        /// </summary>
        /// <param name="other">発見オブジェクト</param>
        private void OnObjectSearch(Collider other)
        {
            // 死亡処理中は操作不可
            if (_isDestroy) return;

            // プレイヤーのみ処理
            if (!IsControl) return;

            // Eキーでアイテム取得
            if (_input.Keys.Contains(KeyCode.E))
            {
                if (other.CompareTag(TagNameConst.ITEM))
                {
                    ISpawnItem item = other.GetComponent<ISpawnItem>();
                    if (_itemComponent.SetItem(item.DroneItem))
                    {
                        // 取得アイテム情報送信
                        NetworkManager.SendUdpToAll(new GetItemPacket(item.DroneItem));

                        // 取得したアイテム削除
                        Destroy(other.gameObject);
                    }

                }
            }
        }

        /// <summary>
        /// 他プレイヤー情報受信イベント
        /// </summary>
        /// <param name="player">送信元プレイヤー</param>
        /// <param name="packet">受信したUDPパケット</param>
        protected override void OnReceiveUdpOfOtherPlayer(string player, BasePacket packet)
        {
            base.OnReceiveUdpOfOtherPlayer(player, packet);

            if (player != Name) return;

            // アクション
            if (packet is DroneActionPacket action)
            {
                if (action.StartLockOn)
                {
                    _lockOnComponent.StartLockOn();
                }
                if (action.StopLockOn)
                {
                    _lockOnComponent.StopLockOn();
                }
                if (action.UseItem1)
                {
                    UseItem(ItemNum.Item1);
                }
                if (action.UseItem2)
                {
                    UseItem(ItemNum.Item2);
                }
            }

            // アイテム取得
            if (packet is GetItemPacket item)
            {
                _itemComponent.SetItem(item.Item);
            }

            // ステータス
            if (packet is DroneStatusPacket status)
            {
                HP = status.Hp;
                _moveComponent.MoveSpeed = status.MoveSpeed;
            }
        }

        /// <summary>
        /// ドローンイベント受信イベント
        /// </summary>
        /// <param name="player">送信元プレイヤー</param>
        /// <param name="packet">受信したUDPパケット</param>
        private void OnReceiveUdpOfEvent(string player, BasePacket packet)
        {
            if (packet is DroneEventPacket evnt)
            {
                // イベント発生者のドローン以外は処理しない
                if (Name != evnt.Name) return;

                if (evnt.BarrierBreak)
                {
                    // バリアに最大ダメージを与えて破壊
                    _barrierComponent.Damage(_barrierComponent.MaxHP);
                }
                if (evnt.BarrierResurrect)
                {
                    // バリア復活
                    _barrierComponent.ResurrectBarrier();
                }
                if (evnt.Destroy)
                {
                    // ドローン破壊
                    Destroy().Forget();
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
            lock (_lock)
            {
                if (_isDestroy) return;

                // 死亡フラグを立てる
                _isDestroy = true;
            }

            // 移動停止
            _rigidbody.velocity = Vector3.zero;

            // 死亡情報送信
            NetworkManager.SendUdpToAll(new DroneEventPacket(Name, false, false, true));

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
            _droneObject.gameObject.SetActive(false);

            // 当たり判定も消す
            GetComponent<Collider>().enabled = false;

            // 爆破生成
            _explosion.SetActive(true);

            // Update停止
            enabled = false;

            // 爆破後一定時間でオブジェクト破棄
            await UniTask.Delay(5000);

            // キャンセル発行
            _cancel.Cancel();

            // ドローン破壊イベント通知
            OnDroneDestroy?.Invoke(this, EventArgs.Empty);

            // オブジェクト破棄
            Destroy(gameObject);
        }
    }
}