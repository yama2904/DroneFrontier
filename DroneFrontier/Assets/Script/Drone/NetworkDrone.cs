using Common;
using Network;
using UnityEngine;

namespace Drone.Network
{
    public class NetworkDrone : NetworkBehaviour
    {
        public string Name { get; protected set; } = "";

        /// <summary>
        /// ���삷��h���[����
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
        /// �h���[�����_
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

        [SerializeField, Tooltip("�h���[���{�̃I�u�W�F�N�g")]
        protected Transform _droneObject = null;

        [SerializeField, Tooltip("�J����")]
        protected Camera _camera = null;

        [SerializeField, Tooltip("UI�\���pCanvas")]
        protected Canvas _canvas = null;

        /// <summary>
        /// ���͏��<br/>
        /// ���t���[���X�V���s��
        /// </summary>
        protected InputData _input = new InputData();

        /// <summary>
        /// �������ς݂ł��邩
        /// </summary>
        protected bool _initialized = false;

        // �R���|�[�l���g�L���b�V��
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

            // �R���|�[�l���g�擾
            _rigidbody = GetComponent<Rigidbody>();
            _listener = GetComponent<AudioListener>();
            _moveComponent = GetComponent<DroneMoveComponent>();
            _rotateComponent = GetComponent<DroneRotateComponent>();
            _soundComponent = GetComponent<DroneSoundComponent>();
            _boostComponent = GetComponent<DroneBoostComponent>();

            // �R���|�[�l���g������
            _moveComponent.Initialize();
            _rotateComponent.Initialize();
            _soundComponent.Initialize();
            _boostComponent.Initialize();

            // �v���C���[������ɑ��삷�邩����
            if (Name == NetworkManager.MyPlayerName)
            {
                IsControl = true;
            }

            // ���v���C���[�̏ꍇ
            if (!_isControl)
            {
                // ��M�C�x���g�ݒ�
                NetworkManager.OnUdpReceivedOnMainThread += OnReceiveUdpOfOtherPlayer;

                // ��Ԃ��I�t�ɂ��Ȃ��Əu�Ԉړ�����
                _rigidbody.interpolation = RigidbodyInterpolation.None;
            }

            // �v���y�����Đ�
            _soundComponent.Play(SoundManager.SE.Propeller, 1, true);

            _initialized = true;
        }

        protected virtual void Update()
        {
            if (!_initialized) return;

            if (_isControl)
            {
                // �u�[�X�g�J�n
                if (_input.DownedKeys.Contains(KeyCode.Space))
                {
                    _boostComponent.StartBoost();
                    NetworkManager.SendUdpToAll(new DroneBoostPacket(true, false));
                }
                // �u�[�X�g��~
                if (_input.UppedKeys.Contains(KeyCode.Space))
                {
                    _boostComponent.StopBoost();
                    NetworkManager.SendUdpToAll(new DroneBoostPacket(false, true));
                }

                // ���͏��X�V
                _input.UpdateInput();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!_initialized) return;

            // �O�i
            if (_input.Keys.Contains(KeyCode.W))
            {
                _moveComponent.Move(DroneMoveComponent.Direction.Forward);
            }

            // ���ړ�
            if (_input.Keys.Contains(KeyCode.A))
            {
                _moveComponent.Move(DroneMoveComponent.Direction.Left);
            }

            // ���
            if (_input.Keys.Contains(KeyCode.S))
            {
                _moveComponent.Move(DroneMoveComponent.Direction.Backwad);
            }

            // �E�ړ�
            if (_input.Keys.Contains(KeyCode.D))
            {
                _moveComponent.Move(DroneMoveComponent.Direction.Right);
            }

            // �㉺�ړ�
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

            // �}�E�X�ɂ������ύX
            _moveComponent.RotateDir(_input.MouseX, _input.MouseY);

            if (_isControl)
            {
                NetworkManager.SendUdpToAll(new InputPacket(_input));
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // ��M�C�x���g�ݒ�
            if (!_isControl)
                NetworkManager.OnUdpReceivedOnMainThread -= OnReceiveUdpOfOtherPlayer;
        }

        /// <summary>
        /// ���v���C���[����M�C�x���g
        /// </summary>
        /// <param name="player">���M���v���C���[</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        protected virtual void OnReceiveUdpOfOtherPlayer(string player, BasePacket packet)
        {
            if (player != Name) return;

            // ���͏��
            if (packet is InputPacket input)
            {
                _input = input.Input;
            }

            // �u�[�X�g
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