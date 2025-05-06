using UnityEngine;

namespace Common
{
    public class CameraManager : MonoBehaviour
    {
        /// <summary>
        /// カメラ感度の初期値
        /// </summary>
        private const float INIT_CAMERA_SPEED = 0.5f;

        public static int ReverseX { get; private set; } = 1;
        public static int ReverseY { get; private set; } = 1;

        /// <summary>
        /// カメラ感度
        /// </summary>
        public static float CameraSpeed
        {
            get
            {
                return _cameraSpeed;
            }
            set
            {
                _cameraSpeed = value;
                if (_cameraSpeed < 0)
                {
                    _cameraSpeed = 0;
                }
                if (_cameraSpeed > 1.0f)
                {
                    _cameraSpeed = 1.0f;
                }
            }
        }
        private static float _cameraSpeed = INIT_CAMERA_SPEED;

        /// <summary>
        /// オブジェクト生成済みであるか
        /// </summary>
        private static bool _isCreated = false;

        /// <summary>
        /// リバースモードの設定
        /// </summary>
        /// <param name="x">X軸</param>
        /// <param name="y">Y軸</param>
        public static void ReverseCamera(bool x, bool y)
        {
            ReverseX = x ? -1 : 1;
            ReverseY = y ? -1 : 1;
        }

        private void Start()
        {
            if (_isCreated)
            {
                Destroy(gameObject);
                return;
            }
            _isCreated = true;
            DontDestroyOnLoad(gameObject);
        }
    }
}