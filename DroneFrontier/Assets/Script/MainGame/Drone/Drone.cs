using UnityEngine;

public class Drone : MonoBehaviour
{
    [SerializeField, Tooltip("ドローン本体オブジェクト")]
    protected Transform _droneObject = null;

    /// <summary>
    /// 入力情報
    /// </summary>
    protected InputData _input = new InputData();

    // コンポーネントキャッシュ
    protected Rigidbody _rigidbody = null;
    protected DroneMoveComponent _moveComponent = null;
    protected DroneRotateComponent _rotateComponent = null;
    protected DroneSoundComponent _soundComponent = null;
    protected DroneBoostComponent _boostComponent = null;

    public virtual void Initialize()
    {
        // コンポーネント初期化
        _moveComponent.Initialize();
        _rotateComponent.Initialize();
        _soundComponent.Initialize();
        _boostComponent.Initialize();

        // プロペラ音再生
        _soundComponent.Play(SoundManager.SE.Propeller, 1, true);
    }

    protected virtual void Awake()
    {
        // コンポーネント取得
        _rigidbody = GetComponent<Rigidbody>();
        _moveComponent = GetComponent<DroneMoveComponent>();
        _rotateComponent = GetComponent<DroneRotateComponent>();
        _soundComponent = GetComponent<DroneSoundComponent>();
        _boostComponent = GetComponent<DroneBoostComponent>();
    }

    protected virtual void Update()
    {
        // 入力情報更新
        _input.UpdateInput();

        // ブースト開始
        if (_input.DownedKeys.Contains(KeyCode.Space))
        {
            _boostComponent.StartBoost();
        }
        // ブースト停止
        if (_input.UppedKeys.Contains(KeyCode.Space))
        {
            _boostComponent.StopBoost();
        }
    }

    protected virtual void FixedUpdate()
    {
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
    }
}
