using System.Collections.Generic;
using UnityEngine;

public class DroneMoveComponent : MonoBehaviour, IDroneComponent
{
    /// <summary>
    /// 回転速度
    /// </summary>
    private const float ROTATE_SPEED = 4f;

    public enum Direction
    {
        /// <summary>
        /// 前
        /// </summary>
        Forward,

        /// <summary>
        /// 左
        /// </summary>
        Left,

        /// <summary>
        /// 右
        /// </summary>
        Right,

        /// <summary>
        /// 後ろ
        /// </summary>
        Backwad,

        /// <summary>
        /// 上
        /// </summary>
        Up,

        /// <summary>
        /// 下
        /// </summary>
        Down,

        None
    }

    /// <summary>
    /// 移動速度
    /// </summary>
    public float MoveSpeed => _moveSpeed;

    /// <summary>
    /// 初期速度
    /// </summary>
    public float InitSpeed { get; private set; } = 0f;

    [SerializeField, Tooltip("移動速度")]
    internal float _moveSpeed = 800;

    [SerializeField, Tooltip("移動時のドローン回転速度")]
    internal float _rotateSpeed = 2f;

    [SerializeField, Tooltip("上下の角度上限")]
    private float _maxRotateX = 40f;

    /// <summary>
    /// 移動フラグ
    /// </summary>
    private bool[] _movingDirs = new bool[(int)Direction.None];

    /// <summary>
    /// 移動速度変更時の採番値
    /// </summary>
    private int _numbering = 0;

    /// <summary>
    /// 変更した移動スピード
    /// </summary>
    private Dictionary<int, float> _changedSpeeds = new Dictionary<int, float>();

    // 各コンポーネント
    private Rigidbody _rigidbody = null;
    private Transform _transform = null;
    private DroneRotateComponent _rotateComponent = null;

    public void Initialize() { }

    /// <summary>
    /// 指定された方向へ移動
    /// </summary>
    /// <param name="dir">移動する方向</param>
    public void Move(Direction dir)
    {
        // 指定された方向に応じて移動先の向き設定
        Vector3 force = Vector3.zero;
        switch (dir)
        {
            case Direction.Forward:
                force = _transform.forward;
                break;

            case Direction.Left:
                force = _transform.right * -1;
                break;

            case Direction.Right:
                force = _transform.right;
                break;

            case Direction.Backwad:
                force = _transform.forward * -1;
                break;

            case Direction.Up:
                force = _transform.up;
                break;

            case Direction.Down:
                force = _transform.up * -1;
                break;
        }

        // 移動
        force *= _moveSpeed;
        _rigidbody.AddForce(force + (force - _rigidbody.velocity), ForceMode.Force);

        // 移動中の方向更新
        _movingDirs[(int)dir] = true;
    }

    /// <summary>
    /// ドローンを回転させて進行方向を変える
    /// </summary>
    /// <param name="vertical">左右方向の回転量（0～1）</param>
    /// <param name="horizontal">上下方向の回転量（0～1）</param>
    public void RotateDir(float vertical, float horizontal)
    {
        // 上下の角度制限をつける
        Vector3 localAngle = _transform.localEulerAngles;
        localAngle.x += horizontal * ROTATE_SPEED * -1;
        if (localAngle.x > _maxRotateX && localAngle.x < 180)
        {
            localAngle.x = _maxRotateX;
        }
        if (localAngle.x < 360 - _maxRotateX && localAngle.x > 180)
        {
            localAngle.x = 360 - _maxRotateX;
        }
        _transform.localEulerAngles = localAngle;

        // 左右回転
        Vector3 angle = _transform.eulerAngles;
        angle.y += vertical * ROTATE_SPEED;
        _transform.eulerAngles = angle;
    }

    /// <summary>
    /// パーセンテージで指定して移動速度を変更
    /// </summary>
    /// <param name="percent">1を現在の速度とした変更速度</param>
    /// <returns>変更ID</returns>
    public int ChangeMoveSpeedPercent(float percent)
    {
        // 変更適用
        _moveSpeed *= percent;

        // 変更一覧に追加
        int id = _numbering++;
        _changedSpeeds.Add(id, percent);

        // IDを返す
        return id;
    }

    /// <summary>
    /// 変更した移動速度を戻す
    /// </summary>
    /// <param name="id">変更時に発行したID</param>
    public void ResetMoveSpeed(int id)
    {
        // ID有効チェック
        if (!_changedSpeeds.ContainsKey(id)) return;

        // 変更値取得
        float per = _changedSpeeds[id];
        _changedSpeeds.Remove(id);

        // 変更を戻す
        if (_changedSpeeds.Count == 0)
        {
            _moveSpeed = InitSpeed;
        }
        else
        {
            _moveSpeed *= 1 / per;
        }
    }

    private void Awake()
    {
        // コンポーネント取得
        _rigidbody = GetComponent<Rigidbody>();
        _transform = transform;
        _rotateComponent = GetComponent<DroneRotateComponent>();

        // 初期速度保存
        InitSpeed = _moveSpeed;
    }

    private void LateUpdate()
    {
        // 移動している方向に傾ける
        Quaternion rotate = Quaternion.identity;
        for (int i = 0; i < _movingDirs.Length; i++)
        {
            // 移動フラグが立っていない場合はスキップ
            if (!_movingDirs[i]) continue;

            // 移動方向へ傾ける
            switch ((Direction)i)
            {
                case Direction.Forward:
                    rotate *= Quaternion.Euler(25, 0, 0);
                    break;

                case Direction.Left:
                    rotate *= Quaternion.Euler(0, 0, 30);
                    break;

                case Direction.Right:
                    rotate *= Quaternion.Euler(0, 0, -30);
                    break;

                case Direction.Backwad:
                    rotate *= Quaternion.Euler(-35, 0, 0);
                    break;
            }
        }
        _rotateComponent.Rotate(rotate, _rotateSpeed * Time.deltaTime);

        // フラグ初期化
        for (int i = 0; i < _movingDirs.Length; i++)
        {
            _movingDirs[i] = false;
        }
    }
}