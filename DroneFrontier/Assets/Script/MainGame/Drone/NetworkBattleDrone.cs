using Cysharp.Threading.Tasks;
using Offline;
using System;
using System.Collections;
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
                _camera.depth = 5;
                _isControl = value;
            }
        }

        public Camera Camera => _camera;

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

        [SerializeField, Tooltip("�h���[����HP")]
        private float _hp = 100f;

        [SerializeField, Tooltip("�X�g�b�N��")]
        private int _stockNum = 2;

        /// <summary>
        /// ���S�t���O
        /// </summary>
        private bool _isDestroy = false;

        // �R���|�[�l���g�L���b�V��
        Rigidbody _rigidbody = null;
        Animator _animator = null;
        DroneMoveComponent _moveComponent = null;
        DroneRotateComponent _rotateComponent = null;
        DroneSoundComponent _soundComponent = null;
        DroneLockOnComponent _lockOnComponent = null;
        DroneRadarComponent _radarComponent = null;
        DroneItemComponent _itemComponent = null;
        DroneWeaponComponent _weaponComponent = null;
        DroneBoostComponent _boostComponent = null;

        private bool _isControl = false;

        [SerializeField, Tooltip("�J����")]
        private Camera _camera = null;

        private void Start()
        {
        
        }

        private void Update()
        {
        
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
            if (Input.GetKey(KeyCode.E))
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
            await UniTask.Delay(TimeSpan.FromSeconds(DEATH_FALL_TIME));

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