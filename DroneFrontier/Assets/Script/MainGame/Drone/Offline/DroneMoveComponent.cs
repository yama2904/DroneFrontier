using UnityEngine;

namespace Offline
{
    public class DroneMoveComponent : MonoBehaviour
    {
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
        public float MoveSpeed 
        {
            get { return _moveSpeed; } 
            set 
            { 
                _moveSpeed = value > 0 ? value : 0f; 
            }
        }

        /// <summary>
        /// 初期速度
        /// </summary>
        public float InitSpeed { get; private set; } = 0f;

        [SerializeField, Tooltip("移動速度")] 
        private float _moveSpeed = 800;

        [SerializeField, Tooltip("移動時に回転させるオブジェクト")] 
        private Transform _rotateObject = null;

        [SerializeField, Tooltip("移動時のドローン回転速度")] 
        private float _rotateSpeed = 2f;

        [SerializeField, Tooltip("上下のカメラ角度上限")] 
        private float _maxCameraRotateX = 40f;

        /// <summary>
        /// 移動フラグ
        /// </summary>
        private bool[] _movingDirs = new bool[(int)Direction.None];

        /// <summary>
        /// 回転中であるか
        /// </summary>
        private bool _rotating = false;

        // 各コンポーネント
        private Rigidbody _rigidbody = null;
        private Transform _transform = null;

        private void Awake()
        {
            // コンポーネント取得
            _rigidbody = GetComponent<Rigidbody>();
            _transform = transform;

            // 初期速度保存
            InitSpeed = _moveSpeed;
        }

        private void Start() { }

        private void Update()
        {
            if (!_rotating)
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
                _rotateObject.localRotation = Quaternion.Slerp(_rotateObject.localRotation, rotate, _rotateSpeed * Time.deltaTime);
            }

            // フラグ初期化
            _rotating = false;
            for (int i = 0; i < _movingDirs.Length; i++)
            {
                _movingDirs[i] = false;
            }
        }

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
        /// 指定した方向へ移動
        /// </summary>
        /// <param name="vec">移動する方向</param>
        public void Move(Vector3 vec)
        {
            Vector3 force = vec * _moveSpeed;
            _rigidbody.AddForce(force + (force - _rigidbody.velocity), ForceMode.Force);
        }

        /// <summary>
        /// 指定した角度と回転量でドローンを回転
        /// </summary>
        /// <param name="rotate">回転先角度</param>
        /// <param name="value">回転量（0～1）</param>
        public void Rotate(Quaternion rotate, float value)
        {
            _rotateObject.localRotation = Quaternion.Slerp(_rotateObject.localRotation, rotate, value);
            _rotating = true;
        }

        /// <summary>
        /// カメラの向きを回転
        /// </summary>
        /// <param name="vertical">左右方向の回転量</param>
        /// <param name="horizontal">上下方向の回転量</param>
        public void RotateCamera(float vertical, float horizontal)
        {
            // 上下の角度制限をつけて上下回転を適用させる
            Vector3 localAngle = _transform.localEulerAngles;
            localAngle.x += horizontal * -1;
            if (localAngle.x > _maxCameraRotateX && localAngle.x < 180)
            {
                localAngle.x = _maxCameraRotateX;
            }
            if (localAngle.x < 360 - _maxCameraRotateX && localAngle.x > 180)
            {
                localAngle.x = 360 - _maxCameraRotateX;
            }
            _transform.localEulerAngles = localAngle;
            // 候補
            //_transform.localRotation = Quaternion.Euler(localAngle);

            // 左右回転
            Vector3 angle = _transform.eulerAngles;
            angle.y += vertical;
            _transform.eulerAngles = angle;
            // 候補
            //Vector3 angle = _transform.eulerAngles;
            //angle.y += vertical;
            //_transform.rotation = Quaternion.Euler(angle);
        }
    }
}