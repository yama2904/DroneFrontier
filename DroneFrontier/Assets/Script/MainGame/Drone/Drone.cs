using UnityEngine;

public class Drone : MonoBehaviour
{
    [SerializeField, Tooltip("�h���[���{�̃I�u�W�F�N�g")]
    protected Transform _droneObject = null;

    /// <summary>
    /// ���͏��
    /// </summary>
    protected InputData _input = new InputData();

    // �R���|�[�l���g�L���b�V��
    protected Rigidbody _rigidbody = null;
    protected DroneMoveComponent _moveComponent = null;
    protected DroneRotateComponent _rotateComponent = null;
    protected DroneSoundComponent _soundComponent = null;
    protected DroneBoostComponent _boostComponent = null;

    public virtual void Initialize()
    {
        // �R���|�[�l���g������
        _moveComponent.Initialize();
        _rotateComponent.Initialize();
        _soundComponent.Initialize();
        _boostComponent.Initialize();

        // �v���y�����Đ�
        _soundComponent.Play(SoundManager.SE.Propeller, 1, true);
    }

    protected virtual void Awake()
    {
        // �R���|�[�l���g�擾
        _rigidbody = GetComponent<Rigidbody>();
        _moveComponent = GetComponent<DroneMoveComponent>();
        _rotateComponent = GetComponent<DroneRotateComponent>();
        _soundComponent = GetComponent<DroneSoundComponent>();
        _boostComponent = GetComponent<DroneBoostComponent>();
    }

    protected virtual void Update()
    {
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
