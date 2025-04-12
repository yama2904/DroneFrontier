using Cysharp.Threading.Tasks;
using Network.Udp;
using Offline;
using Offline.Player;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class NetworkBattleDrone : MyNetworkBehaviour, IBattleDrone, ILockableOn, IRadarable
    {
        /// <summary>
        /// 死亡時の落下時間
        /// </summary>
        private const float DEATH_FALL_TIME = 2.5f;

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
            private set
            {
                _hp = value;
                if (value < 0)
                {
                    _hp = 0;
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
        /// 操作するドローンか
        /// </summary>
        public bool IsControl
        {
            get { return _isControl; }
            set
            {
                IsWatch = value;
                _isControl = value;
            }
        }
        private bool _isControl = false;

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

        [SerializeField, Tooltip("ドローン本体オブジェクト")]
        private Transform _droneObject = null;

        [SerializeField, Tooltip("ドローン死亡時の爆発オブジェクト")]
        private GameObject _explosion = null;

        [SerializeField, Tooltip("ストック数を表示するTextコンポーネント")]
        private Text _stockText = null;

        [SerializeField, Tooltip("オブジェクト探索コンポーネント")]
        private ObjectSearchComponent _searchComponent = null;

        [SerializeField, Tooltip("カメラ")]
        private Camera _camera = null;

        [SerializeField, Tooltip("UI表示用Canvas")]
        private Canvas _uiCanvas = null;

        [SerializeField, Tooltip("ドローンのHP")]
        private float _hp = 100f;

        [SerializeField, Tooltip("ストック数")]
        private int _stockNum = 2;

        [SerializeField, Tooltip("ステータス同期間隔（秒）")]
        private int _syncStatusInterval = 1;

        /// <summary>
        /// 入力情報
        /// </summary>
        private InputData _input = new InputData();

        /// <summary>
        /// 死亡時に発行するキャンセル
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// 死亡フラグ
        /// </summary>
        private bool _isDestroy = false;

        // コンポーネントキャッシュ
        Rigidbody _rigidbody = null;
        Animator _animator = null;
        AudioListener _listener = null;
        DroneMoveComponent _moveComponent = null;
        DroneRotateComponent _rotateComponent = null;
        DroneSoundComponent _soundComponent = null;
        DroneLockOnComponent _lockOnComponent = null;
        DroneRadarComponent _radarComponent = null;
        DroneItemComponent _itemComponent = null;
        DroneWeaponComponent _weaponComponent = null;
        DroneBoostComponent _boostComponent = null;
        DroneBarrierComponent _barrierComponent = null;

        public override string GetAddressKey()
        {
            return "NetworkBattleDrone";
        }

        public override object CreateSpawnData()
        {
            return new Dictionary<string, object>()
            {
                { "Name", Name },
                { "Weapon", SubWeapon }
            };
        }

        public override void ImportSpawnData(object data)
        {
            var dic = data as Dictionary<string, object>;
            Name = (string)dic["Name"];
            SubWeapon = (WeaponType)Enum.ToObject(typeof(WeaponType), dic["Weapon"]);
        }

        public override void Initialize()
        {
            // コンポーネントの取得
            _rigidbody = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
            _listener = GetComponent<AudioListener>();
            _moveComponent = GetComponent<DroneMoveComponent>();
            _rotateComponent = GetComponent<DroneRotateComponent>();
            _soundComponent = GetComponent<DroneSoundComponent>();
            _lockOnComponent = GetComponent<DroneLockOnComponent>();
            _radarComponent = GetComponent<DroneRadarComponent>();
            _itemComponent = GetComponent<DroneItemComponent>();
            _weaponComponent = GetComponent<DroneWeaponComponent>();
            _boostComponent = GetComponent<DroneBoostComponent>();
            _barrierComponent = GetComponent<DroneBarrierComponent>();

            // ストック数UI初期化
            StockNum = _stockNum;

            // ロックオン・レーダー不可オブジェクトに自分を設定
            NotLockableOnList.Add(gameObject);
            NotRadarableList.Add(gameObject);

            // バリアイベント設定
            _barrierComponent.BarrierBreakEvent += OnBarrierBreak;
            _barrierComponent.BarrierResurrectEvent += OnBarrierResurrect;

            // オブジェクト探索イベント設定
            _searchComponent.ObjectStayEvent += ObjectSearchEvent;

            // イベント受信イベント設定
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnReceiveUdpOfEvent;

            // プレイヤー名を基に操作するか識別
            if (Name == MyNetworkManager.Singleton.MyPlayerName)
            {
                IsControl = true;
                IsSyncPosition = true;
            }

            // 自プレイヤーの場合
            if (_isControl)
            {
                // 定期的にステータス同期
                UniTask.Void(async () =>
                {
                    while (true)
                    {
                        await UniTask.Delay(_syncStatusInterval * 1000, ignoreTimeScale: true, cancellationToken: _cancel.Token);
                        float moveSpeed = _moveComponent.MoveSpeed;
                        MyNetworkManager.Singleton.SendToAll(new DroneStatusPacket(HP, moveSpeed));
                    }
                });
            }
            else
            {
                // 他プレイヤーの場合

                // UI非表示
                //_lockOnComponent.HideReticle = true;
                //_itemComponent.HideItemUI = true;
                //_weaponComponent.HideBulletUI = true;
                //_boostComponent.HideGaugeUI = true;
                _uiCanvas.enabled = false;

                // 受信イベント設定
                MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnReceiveUdp;

                // 補間をオフにしないと瞬間移動する
                _rigidbody.interpolation = RigidbodyInterpolation.None;
            }

            // コンポーネント初期化
            _moveComponent.Initialize();
            _rotateComponent.Initialize();
            _soundComponent.Initialize();
            _lockOnComponent.Initialize();
            _radarComponent.Initialize();
            _itemComponent.Initialize();
            _weaponComponent.Initialize();
            _boostComponent.Initialize();
            _barrierComponent.Initialize();
            GetComponent<DroneStatusComponent>().IsPlayer = IsControl;
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

        private void Update()
        {
            // 死亡処理中は操作不可
            if (_isDestroy) return;

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

            if (_isControl)
            {
                bool sendPacket = false;

                bool startLockOn = false;
                bool stopLockOn = false;
                bool startBoost = false;
                bool stopBoost = false;
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
                    _soundComponent.PlayOneShot(SoundManager.SE.Radar, SoundManager.MasterSEVolume);
                    _radarComponent.StartRadar();
                }
                // レーダー終了
                if (_input.UppedKeys.Contains(KeyCode.Q))
                {
                    _radarComponent.StopRadar();
                }

                // ブースト開始
                if (_input.DownedKeys.Contains(KeyCode.Space))
                {
                    _boostComponent.StartBoost();
                    startBoost = true;
                    sendPacket = true;
                }
                // ブースト停止
                if (_input.UppedKeys.Contains(KeyCode.Space))
                {
                    _boostComponent.StopBoost();
                    stopBoost = true;
                    sendPacket = true;
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

                // 入力情報更新
                _input.UpdateInput();

                // アクション情報送信
                if (sendPacket)
                    MyNetworkManager.Singleton.SendToAll(new DroneActionPacket(startLockOn, stopLockOn, startBoost, stopBoost, useItem1, useItem2));
            }
        }

        private void FixedUpdate()
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

            if (_isControl)
            {
                MyNetworkManager.Singleton.SendToAll(new InputPacket(_input));
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // イベント削除
            _barrierComponent.BarrierBreakEvent -= OnBarrierBreak;
            _barrierComponent.BarrierResurrectEvent -= OnBarrierResurrect;
            _searchComponent.ObjectStayEvent -= ObjectSearchEvent;
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnReceiveUdpOfEvent;
            if (!_isControl)
                MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnReceiveUdp;
        }

        /// <summary>
        /// バリア破壊イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnBarrierBreak(object sender, EventArgs e)
        {
            MyNetworkManager.Singleton.SendToAll(new DroneEventPacket(Name, true, false, false));
        }

        /// <summary>
        /// バリア復活イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnBarrierResurrect(object sender, EventArgs e)
        {
            MyNetworkManager.Singleton.SendToAll(new DroneEventPacket(Name, false, true, false));
        }

        /// <summary>
        /// オブジェクト探索イベント
        /// </summary>
        /// <param name="other">発見オブジェクト</param>
        private void ObjectSearchEvent(Collider other)
        {
            // 死亡処理中は操作不可
            if (_isDestroy) return;

            // プレイヤーのみ処理
            if (!_isControl) return;

            // Eキーでアイテム取得
            if (_input.Keys.Contains(KeyCode.E))
            {
                if (other.CompareTag(TagNameConst.ITEM))
                {
                    ISpawnItem item = other.GetComponent<ISpawnItem>();
                    if (_itemComponent.SetItem(item.DroneItem))
                    {
                        // 取得アイテム情報送信
                        MyNetworkManager.Singleton.SendToAll(new GetItemPacket(item.DroneItem));

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
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        private void OnReceiveUdp(string player, UdpHeader header, UdpPacket packet)
        {
            if (player != Name) return;

            // 入力情報
            if (header == UdpHeader.Input)
            {
                _input = (packet as InputPacket).Input;
            }

            // アクション
            if (header == UdpHeader.DroneAction)
            {
                DroneActionPacket action = packet as DroneActionPacket;

                if (action.StartLockOn)
                {
                    _lockOnComponent.StartLockOn();
                }
                if (action.StopLockOn)
                {
                    _lockOnComponent.StopLockOn();
                }
                if (action.StartBoost)
                {
                    _boostComponent.StartBoost();
                }
                if (action.StopBoost)
                {
                    _boostComponent.StopBoost();
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
            if (header == UdpHeader.GetItem)
            {
                _itemComponent.SetItem((packet as GetItemPacket).Item);
            }

            // ステータス
            if (header == UdpHeader.DroneStatus)
            {
                DroneStatusPacket status = packet as DroneStatusPacket;
                HP = status.Hp;
                _moveComponent.MoveSpeed = status.MoveSpeed;
            }
        }

        /// <summary>
        /// ドローンイベント受信イベント
        /// </summary>
        /// <param name="player">送信元プレイヤー</param>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        private void OnReceiveUdpOfEvent(string player, UdpHeader header, UdpPacket packet)
        {
            if (header != UdpHeader.DroneEvent) return;

            // パケット取得
            DroneEventPacket evnt = packet as DroneEventPacket;

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
            if (_isDestroy) return;

            // 死亡フラグを立てる
            _isDestroy = true;

            // 移動停止
            _rigidbody.velocity = Vector3.zero;

            // 死亡情報送信
            MyNetworkManager.Singleton.SendToAll(new DroneEventPacket(Name, false, false, true));

            // コンポーネント停止
            _moveComponent.enabled = false;
            _boostComponent.enabled = false;
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

            // キャンセル発行
            _cancel.Cancel();

            // ドローン破壊イベント通知
            DroneDestroyEvent?.Invoke(this, EventArgs.Empty);

            // オブジェクト破棄
            Destroy(gameObject);
        }
    }
}