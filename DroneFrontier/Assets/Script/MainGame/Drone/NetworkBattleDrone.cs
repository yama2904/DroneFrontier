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
        /// ���S���̗�������
        /// </summary>
        private const float DEATH_FALL_TIME = 2.5f;

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

        [SerializeField, Tooltip("UI�\���pCanvas")]
        private Canvas _uiCanvas = null;

        [SerializeField, Tooltip("�h���[����HP")]
        private float _hp = 100f;

        [SerializeField, Tooltip("�X�g�b�N��")]
        private int _stockNum = 2;

        [SerializeField, Tooltip("�X�e�[�^�X�����Ԋu�i�b�j")]
        private int _syncStatusInterval = 1;

        /// <summary>
        /// ���͏��
        /// </summary>
        private InputData _input = new InputData();

        /// <summary>
        /// ���S���ɔ��s����L�����Z��
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

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
            _barrierComponent = GetComponent<DroneBarrierComponent>();

            // �X�g�b�N��UI������
            StockNum = _stockNum;

            // ���b�N�I���E���[�_�[�s�I�u�W�F�N�g�Ɏ�����ݒ�
            NotLockableOnList.Add(gameObject);
            NotRadarableList.Add(gameObject);

            // �o���A�C�x���g�ݒ�
            _barrierComponent.BarrierBreakEvent += OnBarrierBreak;
            _barrierComponent.BarrierResurrectEvent += OnBarrierResurrect;

            // �I�u�W�F�N�g�T���C�x���g�ݒ�
            _searchComponent.ObjectStayEvent += ObjectSearchEvent;

            // �C�x���g��M�C�x���g�ݒ�
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnReceiveUdpOfEvent;

            // �v���C���[������ɑ��삷�邩����
            if (Name == MyNetworkManager.Singleton.MyPlayerName)
            {
                IsControl = true;
                IsSyncPosition = true;
            }

            // ���v���C���[�̏ꍇ
            if (_isControl)
            {
                // ����I�ɃX�e�[�^�X����
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
                // ���v���C���[�̏ꍇ

                // UI��\��
                //_lockOnComponent.HideReticle = true;
                //_itemComponent.HideItemUI = true;
                //_weaponComponent.HideBulletUI = true;
                //_boostComponent.HideGaugeUI = true;
                _uiCanvas.enabled = false;

                // ��M�C�x���g�ݒ�
                MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnReceiveUdp;

                // ��Ԃ��I�t�ɂ��Ȃ��Əu�Ԉړ�����
                _rigidbody.interpolation = RigidbodyInterpolation.None;
            }

            // �R���|�[�l���g������
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
            // �h���[�����j�󂳂�Ă���ꍇ�͉������Ȃ�
            if (_hp <= 0) return;

            // �����_��2�ȉ��؂�̂ĂŃ_���[�W�K�p
            HP -= Useful.Floor(value, 1);

            // HP��0�ɂȂ�����j�󏈗�
            if (_hp <= 0)
            {
                Destroy().Forget();
            }
        }

        private void Update()
        {
            // ���S�������͑���s��
            if (_isDestroy) return;

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

            if (_isControl)
            {
                bool sendPacket = false;

                bool startLockOn = false;
                bool stopLockOn = false;
                bool startBoost = false;
                bool stopBoost = false;
                bool useItem1 = false;
                bool useItem2 = false;

                // ���b�N�I���g�p
                if (_input.DownedKeys.Contains(KeyCode.LeftShift))
                {
                    _lockOnComponent.StartLockOn();
                    startLockOn = true;
                    sendPacket = true;
                }
                // ���b�N�I������
                if (_input.UppedKeys.Contains(KeyCode.LeftShift))
                {
                    _lockOnComponent.StopLockOn();
                    stopLockOn = true;
                    sendPacket = true;
                }

                // ���[�_�[�g�p
                if (_input.DownedKeys.Contains(KeyCode.Q))
                {
                    _soundComponent.PlayOneShot(SoundManager.SE.Radar, SoundManager.MasterSEVolume);
                    _radarComponent.StartRadar();
                }
                // ���[�_�[�I��
                if (_input.UppedKeys.Contains(KeyCode.Q))
                {
                    _radarComponent.StopRadar();
                }

                // �u�[�X�g�J�n
                if (_input.DownedKeys.Contains(KeyCode.Space))
                {
                    _boostComponent.StartBoost();
                    startBoost = true;
                    sendPacket = true;
                }
                // �u�[�X�g��~
                if (_input.UppedKeys.Contains(KeyCode.Space))
                {
                    _boostComponent.StopBoost();
                    stopBoost = true;
                    sendPacket = true;
                }

                // �A�C�e���g�p
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

                // ���͏��X�V
                _input.UpdateInput();

                // �A�N�V������񑗐M
                if (sendPacket)
                    MyNetworkManager.Singleton.SendToAll(new DroneActionPacket(startLockOn, stopLockOn, startBoost, stopBoost, useItem1, useItem2));
            }
        }

        private void FixedUpdate()
        {
            // ���S����
            if (_isDestroy)
            {
                // �������Ȃ��痎����
                _rigidbody.AddForce(new Vector3(0, -400, 0), ForceMode.Acceleration);

                // �h���[�����X����
                _rotateComponent.Rotate(Quaternion.Euler(28, -28, -28), 2 * Time.deltaTime);

                // �v���y������
                _animator.speed *= 0.993f;

                return;
            }

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
                MyNetworkManager.Singleton.SendToAll(new InputPacket(_input));
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // �C�x���g�폜
            _barrierComponent.BarrierBreakEvent -= OnBarrierBreak;
            _barrierComponent.BarrierResurrectEvent -= OnBarrierResurrect;
            _searchComponent.ObjectStayEvent -= ObjectSearchEvent;
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnReceiveUdpOfEvent;
            if (!_isControl)
                MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnReceiveUdp;
        }

        /// <summary>
        /// �o���A�j��C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnBarrierBreak(object sender, EventArgs e)
        {
            MyNetworkManager.Singleton.SendToAll(new DroneEventPacket(Name, true, false, false));
        }

        /// <summary>
        /// �o���A�����C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnBarrierResurrect(object sender, EventArgs e)
        {
            MyNetworkManager.Singleton.SendToAll(new DroneEventPacket(Name, false, true, false));
        }

        /// <summary>
        /// �I�u�W�F�N�g�T���C�x���g
        /// </summary>
        /// <param name="other">�����I�u�W�F�N�g</param>
        private void ObjectSearchEvent(Collider other)
        {
            // ���S�������͑���s��
            if (_isDestroy) return;

            // �v���C���[�̂ݏ���
            if (!_isControl) return;

            // E�L�[�ŃA�C�e���擾
            if (_input.Keys.Contains(KeyCode.E))
            {
                if (other.CompareTag(TagNameConst.ITEM))
                {
                    ISpawnItem item = other.GetComponent<ISpawnItem>();
                    if (_itemComponent.SetItem(item.DroneItem))
                    {
                        // �擾�A�C�e����񑗐M
                        MyNetworkManager.Singleton.SendToAll(new GetItemPacket(item.DroneItem));

                        // �擾�����A�C�e���폜
                        Destroy(other.gameObject);
                    }

                }
            }
        }

        /// <summary>
        /// ���v���C���[����M�C�x���g
        /// </summary>
        /// <param name="player">���M���v���C���[</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnReceiveUdp(string player, UdpHeader header, UdpPacket packet)
        {
            if (player != Name) return;

            // ���͏��
            if (header == UdpHeader.Input)
            {
                _input = (packet as InputPacket).Input;
            }

            // �A�N�V����
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

            // �A�C�e���擾
            if (header == UdpHeader.GetItem)
            {
                _itemComponent.SetItem((packet as GetItemPacket).Item);
            }

            // �X�e�[�^�X
            if (header == UdpHeader.DroneStatus)
            {
                DroneStatusPacket status = packet as DroneStatusPacket;
                HP = status.Hp;
                _moveComponent.MoveSpeed = status.MoveSpeed;
            }
        }

        /// <summary>
        /// �h���[���C�x���g��M�C�x���g
        /// </summary>
        /// <param name="player">���M���v���C���[</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnReceiveUdpOfEvent(string player, UdpHeader header, UdpPacket packet)
        {
            if (header != UdpHeader.DroneEvent) return;

            // �p�P�b�g�擾
            DroneEventPacket evnt = packet as DroneEventPacket;

            // �C�x���g�����҂̃h���[���ȊO�͏������Ȃ�
            if (Name != evnt.Name) return;

            if (evnt.BarrierBreak)
            {
                // �o���A�ɍő�_���[�W��^���Ĕj��
                _barrierComponent.Damage(_barrierComponent.MaxHP);
            }
            if (evnt.BarrierResurrect)
            {
                // �o���A����
                _barrierComponent.ResurrectBarrier();
            }
            if (evnt.Destroy)
            {
                // �h���[���j��
                Destroy().Forget();
            }
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
                _soundComponent.PlayOneShot(SoundManager.SE.UseItem, SoundManager.MasterSEVolume);
            }
        }

        /// <summary>
        /// ���S����
        /// </summary>
        private async UniTask Destroy()
        {
            if (_isDestroy) return;

            // ���S�t���O�𗧂Ă�
            _isDestroy = true;

            // �ړ���~
            _rigidbody.velocity = Vector3.zero;

            // ���S��񑗐M
            MyNetworkManager.Singleton.SendToAll(new DroneEventPacket(Name, false, false, true));

            // �R���|�[�l���g��~
            _moveComponent.enabled = false;
            _boostComponent.enabled = false;
            _lockOnComponent.StopLockOn();
            _radarComponent.StopRadar();

            // ���SSE�Đ�
            _soundComponent.PlayOneShot(SoundManager.SE.Death, SoundManager.MasterSEVolume);

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

            // �L�����Z�����s
            _cancel.Cancel();

            // �h���[���j��C�x���g�ʒm
            DroneDestroyEvent?.Invoke(this, EventArgs.Empty);

            // �I�u�W�F�N�g�j��
            Destroy(gameObject);
        }
    }
}