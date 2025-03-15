using Cysharp.Threading.Tasks;
using Network.Udp;
using Offline;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class NetworkBattleDrone : MyNetworkBehaviour, IBattleDrone
    {
        #region public

        /// <summary>
        /// �h���[���̖��O
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// �h���[����HP
        /// </summary>
        public float HP
        {
            get { return _hp; }
            set
            {
                if (_hp <= 0) return;

                if (value > 0)
                {
                    // �����_��2�ȉ��؂�̂�
                    _hp = Useful.Floor(value, 1);
                }
                else
                {
                    // HP��0�ɂȂ�����j�󏈗�
                    _hp = 0;
                    Destroy().Forget();
                }
            }
        }

        /// <summary>
        /// ���݂̃X�g�b�N��
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
        /// �h���[���̃T�u����
        /// </summary>
        public WeaponType SubWeapon { get; set; }

        /// <summary>
        /// ���b�N�I���\�ł��邩
        /// </summary>
        public bool IsLockableOn { get; } = true;

        /// <summary>
        /// ���b�N�I���s�ɂ���I�u�W�F�N�g
        /// </summary>
        public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Enemy;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList { get; } = new List<GameObject>();

        /// <summary>
        /// ���삷��h���[����
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
        /// ���̃h���[�������邩
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
        /// �h���[���j��C�x���g
        /// </summary>
        public event EventHandler DroneDestroyEvent;

        #endregion

        /// <summary>
        /// �����A�C�e���ԍ�
        /// </summary>
        private enum ItemNum
        {
            /// <summary>
            /// �A�C�e��1
            /// </summary>
            Item1,

            /// <summary>
            /// �A�C�e��2
            /// </summary>
            Item2
        }

        /// <summary>
        /// ���S���̉�]��
        /// </summary>
        private readonly Quaternion DEATH_ROTATE = Quaternion.Euler(28, -28, -28);

        /// <summary>
        /// ���S���̉�]���x
        /// </summary>
        private const float DEATH_ROTATE_SPEED = 2f;

        /// <summary>
        /// ���S���̗�������
        /// </summary>
        private const float DEATH_FALL_TIME = 2.5f;

        [SerializeField, Tooltip("�h���[���{�̃I�u�W�F�N�g")]
        private Transform _droneObject = null;

        [SerializeField, Tooltip("�h���[�����S���̔����I�u�W�F�N�g")]
        private GameObject _explosion = null;

        [SerializeField, Tooltip("�X�g�b�N����\������Text�R���|�[�l���g")]
        private Text _stockText = null;

        [SerializeField, Tooltip("�I�u�W�F�N�g�T���R���|�[�l���g")]
        private ObjectSearchComponent _searchComponent = null;

        [SerializeField, Tooltip("�J����")]
        private Camera _camera = null;

        [SerializeField, Tooltip("�h���[����HP")]
        private float _hp = 100f;

        [SerializeField, Tooltip("�X�g�b�N��")]
        private int _stockNum = 2;

        private InputData _input = new InputData();

        /// <summary>
        /// ���S�t���O
        /// </summary>
        private bool _isDestroy = false;

        // �R���|�[�l���g�L���b�V��
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

        protected override void Awake()
        {
            base.Awake();

            // �R���|�[�l���g�̎擾
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

            // �X�g�b�N��UI������
            StockNum = _stockNum;

            // ���b�N�I���E���[�_�[�s�I�u�W�F�N�g�Ɏ�����ݒ�
            NotLockableOnList.Add(gameObject);
            NotRadarableList.Add(gameObject);

            // �I�u�W�F�N�g�T���C�x���g�ݒ�
            _searchComponent.ObjectStayEvent += ObjectSearchEvent;
        }

        private void Start()
        {
            // �v���C���[������ɑ��삷�邩����
            if (Name == MyNetworkManager.Singleton.MyPlayerName)
            {
                IsControl = true;
                IsSyncPosition = true;
            }

            // ���͏���M�C�x���g�ݒ�
            if (!_isControl)
                MyNetworkManager.Singleton.OnUdpReceive += OnReceiveUdpOfInput;

            enabled = false;
        }

        private void Update()
        {
            // ���S�������͑���s��
            if (_isDestroy)
            {
                // �������Ȃ��痎����
                _rigidbody.AddForce(new Vector3(0, -400, 0), ForceMode.Acceleration);

                // �h���[�����X����
                _rotateComponent.Rotate(DEATH_ROTATE, DEATH_ROTATE_SPEED * Time.deltaTime);

                // �v���y������
                _animator.speed *= 0.993f;
                
                return;
            }

            // ���b�N�I���g�p
            if (_input.DownedKeys.Contains(KeyCode.LeftShift))
            {
                _lockOnComponent.StartLockOn();
            }
            // ���b�N�I������
            if (_input.UppedKeys.Contains(KeyCode.LeftShift))
            {
                _lockOnComponent.StopLockOn();
            }

            // ���[�_�[�g�p
            if (_input.DownedKeys.Contains(KeyCode.Q))
            {
                _soundComponent.PlayOneShot(SoundManager.SE.RADAR, SoundManager.SEVolume);
                _radarComponent.StartRadar();
            }
            // ���[�_�[�I��
            if (_input.UppedKeys.Contains(KeyCode.Q))
            {
                _radarComponent.StopRadar();
            }

            // ���C������U���i�T�u����U�����̏ꍇ�͕s�j
            if (_input.MouseButtonL && !_weaponComponent.ShootingSubWeapon)
            {
                _weaponComponent.Shot(DroneWeaponComponent.Weapon.MAIN, _lockOnComponent.Target);
            }

            // �T�u����U���i���C������U�����̏ꍇ�͕s�j
            if (_input.MouseButtonR && !_weaponComponent.ShootingMainWeapon)
            {
                _weaponComponent.Shot(DroneWeaponComponent.Weapon.SUB, _lockOnComponent.Target);
            }

            // �u�[�X�g�g�p
            if (_input.Keys.Contains(KeyCode.Space))
            {
                _boostComponent.Boost();
            }

            // �A�C�e���g�p
            if (_input.UppedKeys.Contains(KeyCode.Alpha1))
            {
                UseItem(ItemNum.Item1);
            }
            if (_input.UppedKeys.Contains(KeyCode.Alpha2))
            {
                UseItem(ItemNum.Item2);
            }

            if (_isControl)
            {
                // ���͏��X�V
                _input.UpdateInput();

                // ���͏�񑗐M
                if (!NetworkDelayMonitor.IsPause)
                    MyNetworkManager.Singleton.SendToAll(new InputPacket(_input));
            }
        }

        private void FixedUpdate()
        {
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
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // �C�x���g�폜
            _searchComponent.ObjectStayEvent -= ObjectSearchEvent;
            if (!_isControl) 
                MyNetworkManager.Singleton.OnUdpReceive -= OnReceiveUdpOfInput;
        }

        /// <summary>
        /// �I�u�W�F�N�g�T���C�x���g
        /// </summary>
        /// <param name="other">�����I�u�W�F�N�g</param>
        private void ObjectSearchEvent(Collider other)
        {
            // ���S�������͑���s��
            if (_isDestroy) return;

            // E�L�[�ŃA�C�e���擾
            if (_input.Keys.Contains(KeyCode.E))
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
        /// ���͏��p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="player">���M���v���C���[</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnReceiveUdpOfInput(string player, UdpHeader header, UdpPacket packet)
        {
            if (header != UdpHeader.Input) return;
            if (player != Name) return;
            _input = (packet as InputPacket).Input;
        }

        /// <summary>
        /// �w�肵���ԍ��̃A�C�e���g�p
        /// </summary>
        /// <param name="item">�g�p����A�C�e���ԍ�</param>
        private void UseItem(ItemNum item)
        {
            // �A�C�e���g�ɃA�C�e���������Ă�����g�p
            if (_itemComponent.UseItem((int)item))
            {
                _soundComponent.PlayOneShot(SoundManager.SE.USE_ITEM, SoundManager.SEVolume);
            }
        }

        /// <summary>
        /// ���S����
        /// </summary>
        private async UniTask Destroy()
        {
            // ���S�t���O�𗧂Ă�
            _isDestroy = true;

            // �ړ��R���|�[�l���g��~
            _moveComponent.enabled = false;

            // ���b�N�I���E���[�_�[����
            _lockOnComponent.StopLockOn();
            _radarComponent.StopRadar();

            // ���SSE�Đ�
            _soundComponent.PlayOneShot(SoundManager.SE.DEATH, SoundManager.SEVolume);

            // ��莞�Ԍo�߂��Ă��甚�j
            await UniTask.Delay(TimeSpan.FromSeconds(DEATH_FALL_TIME), ignoreTimeScale: true);

            // �h���[���̔�\��
            _droneObject.gameObject.SetActive(false);

            // �����蔻�������
            GetComponent<Collider>().enabled = false;

            // ���j����
            _explosion.SetActive(true);

            // Update��~
            enabled = false;

            // ���j���莞�ԂŃI�u�W�F�N�g�j��
            await UniTask.Delay(5000);

            // �h���[���j��C�x���g�ʒm
            DroneDestroyEvent?.Invoke(this, EventArgs.Empty);

            // �I�u�W�F�N�g�j��
            Destroy(gameObject);
        }
    }
}