using Common;
using UnityEngine;

namespace Drone
{
    public class Drone : MonoBehaviour
    {
        public string Name { get; private set; } = "";

        [SerializeField, Tooltip("�h���[���{�̃I�u�W�F�N�g")]
        protected Transform _droneObject = null;

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
        protected DroneMoveComponent _moveComponent = null;
        protected DroneRotateComponent _rotateComponent = null;
        protected DroneSoundComponent _soundComponent = null;
        protected DroneBoostComponent _boostComponent = null;

        public virtual void Initialize(string name)
        {
            // �h���[�����ݒ�
            Name = name;

            // �R���|�[�l���g�擾
            _rigidbody = GetComponent<Rigidbody>();
            _moveComponent = GetComponent<DroneMoveComponent>();
            _rotateComponent = GetComponent<DroneRotateComponent>();
            _soundComponent = GetComponent<DroneSoundComponent>();
            _boostComponent = GetComponent<DroneBoostComponent>();

            // �R���|�[�l���g������
            _moveComponent.Initialize();
            _rotateComponent.Initialize();
            _soundComponent.Initialize();
            _boostComponent.Initialize();

            // �v���y�����Đ�
            _soundComponent.Play(SoundManager.SE.Propeller, 1, true);

            _initialized = true;
        }

        protected virtual void Update()
        {
            if (!_initialized) return;

            // ���͏��X�V
            _input.UpdateInput();

            // �u�[�X�g�J�n
            if (_input.DownedKeys.Contains(KeyCode.Space))
            {
                _boostComponent.StartBoost();
            }
            // �u�[�X�g��~
            if (_input.UppedKeys.Contains(KeyCode.Space))
            {
                _boostComponent.StopBoost();
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
        }
    }
}