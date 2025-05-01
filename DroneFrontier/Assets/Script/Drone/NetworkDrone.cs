using Common;
using Network;
using UnityEngine;

namespace Drone.Network
{
    public class NetworkDrone : NetworkBehaviour
    {
        public string Name { get; protected set; } = "";

        /// <summary>
        /// 操作するドローンか
        /// </summary>
        public bool IsControl
        {
            get { return _isControl; }
            set
            {
                _isControl = value;
                IsWatch = value;
                IsSyncPosition = value;
            }
        }
        private bool _isControl = false;

        /// <summary>
        /// ドローン視点
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

        [SerializeField, Tooltip("ドローン本体オブジェクト")]
        protected Transform _droneObject = null;

        [SerializeField, Tooltip("カメラ")]
        protected Camera _camera = null;

        [SerializeField, Tooltip("UI表示用Canvas")]
        protected Canvas _canvas = null;

        /// <summary>
        /// 入力情報<br/>
        /// 毎フレーム更新を行う
        /// </summary>
        protected InputData _input = new InputData();

        /// <summary>
        /// 初期化済みであるか
        /// </summary>
        protected bool _initialized = false;

        // コンポーネントキャッシュ
        protected Rigidbody _rigidbody = null;
        protected AudioListener _listener = null;
        protected DroneMoveComponent _moveComponent = null;
        protected DroneRotateComponent _rotateComponent = null;
        protected DroneSoundComponent _soundComponent = null;
        protected DroneBoostComponent _boostComponent = null;

        public override string GetAddressKey()
        {
            return "NetworkDrone";
        }

        public override object CreateSpawnData()
        {
            return Name;
        }

        public override void ImportSpawnData(object data)
        {
            Name = (string)data;
        }

        public override void InitializeSpawn()
        {
            Initialize(Name);
        }

        public virtual void Initialize(string name)
        {
            Name = name;

            // コンポーネント取得
            _rigidbody = GetComponent<Rigidbody>();
            _listener = GetComponent<AudioListener>();
            _moveComponent = GetComponent<DroneMoveComponent>();
            _rotateComponent = GetComponent<DroneRotateComponent>();
            _soundComponent = GetComponent<DroneSoundComponent>();
            _boostComponent = GetComponent<DroneBoostComponent>();

            // コンポーネント初期化
            _moveComponent.Initialize();
            _rotateComponent.Initialize();
            _soundComponent.Initialize();
            _boostComponent.Initialize();

            // プレイヤー名を基に操作するか識別
            if (Name == NetworkManager.MyPlayerName)
            {
                IsControl = true;
            }

            // 他プレイヤーの場合
            if (!_isControl)
            {
                // 受信イベント設定
                NetworkManager.OnUdpReceivedOnMainThread += OnReceiveUdpOfOtherPlayer;

                // 補間をオフにしないと瞬間移動する
                _rigidbody.interpolation = RigidbodyInterpolation.None;
            }

            // プロペラ音再生
            _soundComponent.Play(SoundManager.SE.Propeller, 1, true);

            _initialized = true;
        }

        protected virtual void Update()
        {
            if (!_initialized) return;

            if (_isControl)
            {
                // ブースト開始
                if (_input.DownedKeys.Contains(KeyCode.Space))
                {
                    _boostComponent.StartBoost();
                    NetworkManager.SendUdpToAll(new DroneBoostPacket(true, false));
                }
                // ブースト停止
                if (_input.UppedKeys.Contains(KeyCode.Space))
                {
                    _boostComponent.StopBoost();
                    NetworkManager.SendUdpToAll(new DroneBoostPacket(false, true));
                }

                // 入力情報更新
                _input.UpdateInput();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!_initialized) return;

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
                NetworkManager.SendUdpToAll(new InputPacket(_input));
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 受信イベント設定
            if (!_isControl)
                NetworkManager.OnUdpReceivedOnMainThread -= OnReceiveUdpOfOtherPlayer;
        }

        /// <summary>
        /// 他プレイヤー情報受信イベント
        /// </summary>
        /// <param name="player">送信元プレイヤー</param>
        /// <param name="packet">受信したUDPパケット</param>
        protected virtual void OnReceiveUdpOfOtherPlayer(string player, BasePacket packet)
        {
            if (player != Name) return;

            // 入力情報
            if (packet is InputPacket input)
            {
                _input = input.Input;
            }

            // ブースト
            if (packet is DroneBoostPacket boost)
            {
                if (boost.StartBoost)
                {
                    _boostComponent.StartBoost();
                }
                if (boost.StopBoost)
                {
                    _boostComponent.StopBoost();
                }
            }
        }
    }
}