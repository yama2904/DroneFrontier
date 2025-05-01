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
        /// ���X�|�[��������
        /// </summary>
        public bool IsRespawn { get; set; } = false;

        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        public event EventHandler OnDroneDestroy;

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

        [SerializeField, Tooltip("�h���[�����S���̔����I�u�W�F�N�g")]
        private GameObject _explosion = null;

        [SerializeField, Tooltip("�X�g�b�N����\������Text�R���|�[�l���g")]
        private Text _stockText = null;

        [SerializeField, Tooltip("�I�u�W�F�N�g�T���R���|�[�l���g")]
        private ObjectSearchComponent _searchComponent = null;

        [SerializeField, Tooltip("�e��UI�\���pCanvas")]
        private Canvas _bulletCanvas = null;

        [SerializeField, Tooltip("�h���[����HP")]
        private float _hp = 100f;

        [SerializeField, Tooltip("�X�g�b�N��")]
        private int _stockNum = 2;

        [SerializeField, Tooltip("�X�e�[�^�X�����Ԋu�i�b�j")]
        private int _syncStatusInterval = 1;

        /// <summary>
        /// ���S���ɔ��s����L�����Z��
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// ���S�t���O
        /// </summary>
        private bool _isDestroy = false;

        private readonly object _lock = new object();

        // �R���|�[�l���g�L���b�V��
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

            // ���C���E�F�|���ݒ�
            MainWeapon = mainWeapon;
            MainWeapon.Initialize(gameObject);

            // �T�u�E�F�|���ݒ�
            SubWeapon = subWeapon;
            SubWeapon.Initialize(gameObject);

            // �X�g�b�N���ݒ�
            _stockNum = stock;
            _stockText.text = _stockNum.ToString();

            // �R���|�[�l���g�̎擾
            _animator = GetComponent<Animator>();
            _lockOnComponent = GetComponent<DroneLockOnComponent>();
            _radarComponent = GetComponent<DroneRadarComponent>();
            _itemComponent = GetComponent<DroneItemComponent>();
            _weaponComponent = GetComponent<DroneWeaponComponent>();
            _barrierComponent = GetComponent<DroneBarrierComponent>();

            // ���b�N�I���E���[�_�[�s�I�u�W�F�N�g�Ɏ�����ݒ�
            NotLockableOnList.Add(gameObject);
            NotRadarableList.Add(gameObject);

            // �o���A�C�x���g�ݒ�
            _barrierComponent.OnBarrierBreak += OnBarrierBreak;
            _barrierComponent.OnBarrierResurrect += OnBarrierResurrect;

            // �I�u�W�F�N�g�T���C�x���g�ݒ�
            _searchComponent.OnObjectStay += OnObjectSearch;

            // �C�x���g��M�C�x���g�ݒ�
            NetworkManager.OnUdpReceivedOnMainThread += OnReceiveUdpOfEvent;

            // ���v���C���[�̏ꍇ�͒���I�ɃX�e�[�^�X����
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

            // �R���|�[�l���g������
            _lockOnComponent.Initialize();
            _radarComponent.Initialize();
            _itemComponent.Initialize();
            _weaponComponent.Initialize();
            _barrierComponent.Initialize();
            GetComponent<DroneStatusComponent>().IsPlayer = IsControl;

            // ���X�|�[�������ꍇ�͕���SE�Đ�
            if (IsRespawn)
            {
                _soundComponent.Play(SoundManager.SE.Respawn);
            }
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

        protected override void Update()
        {
            // ���S�������͑���s��
            if (_isDestroy) return;

            base.Update();

            // ���C������U���i�T�u����U�����̏ꍇ�͕s�j
            if (_input.MouseButtonL && !_weaponComponent.ShootingSubWeapon)
            {
                _weaponComponent.Shot(DroneWeaponComponent.Weapon.Main, _lockOnComponent.Target);
            }

            // �T�u����U���i���C������U�����̏ꍇ�͕s�j
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
                    _soundComponent.Play(SoundManager.SE.Radar);
                    _radarComponent.StartRadar();
                }
                // ���[�_�[�I��
                if (_input.UppedKeys.Contains(KeyCode.Q))
                {
                    _radarComponent.StopRadar();
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

                // �A�N�V������񑗐M
                if (sendPacket)
                    NetworkManager.SendUdpToAll(new DroneActionPacket(startLockOn, stopLockOn, useItem1, useItem2));
            }
        }

        protected override void FixedUpdate()
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

            base.FixedUpdate();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // �C�x���g�폜
            _barrierComponent.OnBarrierBreak -= OnBarrierBreak;
            _barrierComponent.OnBarrierResurrect -= OnBarrierResurrect;
            _searchComponent.OnObjectStay -= OnObjectSearch;
            NetworkManager.OnUdpReceivedOnMainThread -= OnReceiveUdpOfEvent;

            // �L�����Z�����s
            _cancel.Cancel();
        }

        /// <summary>
        /// �o���A�j��C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnBarrierBreak(object sender, EventArgs e)
        {
            NetworkManager.SendUdpToAll(new DroneEventPacket(Name, true, false, false));
        }

        /// <summary>
        /// �o���A�����C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnBarrierResurrect(object sender, EventArgs e)
        {
            NetworkManager.SendUdpToAll(new DroneEventPacket(Name, false, true, false));
        }

        /// <summary>
        /// �I�u�W�F�N�g�T���C�x���g
        /// </summary>
        /// <param name="other">�����I�u�W�F�N�g</param>
        private void OnObjectSearch(Collider other)
        {
            // ���S�������͑���s��
            if (_isDestroy) return;

            // �v���C���[�̂ݏ���
            if (!IsControl) return;

            // E�L�[�ŃA�C�e���擾
            if (_input.Keys.Contains(KeyCode.E))
            {
                if (other.CompareTag(TagNameConst.ITEM))
                {
                    ISpawnItem item = other.GetComponent<ISpawnItem>();
                    if (_itemComponent.SetItem(item.DroneItem))
                    {
                        // �擾�A�C�e����񑗐M
                        NetworkManager.SendUdpToAll(new GetItemPacket(item.DroneItem));

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
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        protected override void OnReceiveUdpOfOtherPlayer(string player, BasePacket packet)
        {
            base.OnReceiveUdpOfOtherPlayer(player, packet);

            if (player != Name) return;

            // �A�N�V����
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

            // �A�C�e���擾
            if (packet is GetItemPacket item)
            {
                _itemComponent.SetItem(item.Item);
            }

            // �X�e�[�^�X
            if (packet is DroneStatusPacket status)
            {
                HP = status.Hp;
                _moveComponent.MoveSpeed = status.MoveSpeed;
            }
        }

        /// <summary>
        /// �h���[���C�x���g��M�C�x���g
        /// </summary>
        /// <param name="player">���M���v���C���[</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnReceiveUdpOfEvent(string player, BasePacket packet)
        {
            if (packet is DroneEventPacket evnt)
            {
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
                _soundComponent.Play(SoundManager.SE.UseItem);
            }
        }

        /// <summary>
        /// ���S����
        /// </summary>
        private async UniTask Destroy()
        {
            lock (_lock)
            {
                if (_isDestroy) return;

                // ���S�t���O�𗧂Ă�
                _isDestroy = true;
            }

            // �ړ���~
            _rigidbody.velocity = Vector3.zero;

            // ���S��񑗐M
            NetworkManager.SendUdpToAll(new DroneEventPacket(Name, false, false, true));

            // �R���|�[�l���g��~
            _moveComponent.enabled = false;
            _boostComponent.enabled = false;
            _lockOnComponent.StopLockOn();
            _radarComponent.StopRadar();

            // ���SSE�Đ�
            _soundComponent.Play(SoundManager.SE.Death);

            // ��莞�Ԍo�߂��Ă��甚�j
            await UniTask.Delay(TimeSpan.FromSeconds(2.5f), ignoreTimeScale: true);

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
            OnDroneDestroy?.Invoke(this, EventArgs.Empty);

            // �I�u�W�F�N�g�j��
            Destroy(gameObject);
        }
    }
}