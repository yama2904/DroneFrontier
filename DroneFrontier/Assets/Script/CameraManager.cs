using UnityEngine;

public class CameraManager : MonoBehaviour
{
    /// <summary>
    /// 設定したカメラ感度の調整用倍率
    /// </summary>
    private const float CAMERA_SPEED_SCALE = 4f;

    public static short ReverseX { get; private set; } = 1;
    public static short ReverseY { get; private set; } = 1;

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
    private static float _cameraSpeed = 1f;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    //カメラをリバースモードにするならtrue
    //ノーマルモードはfalse
    //引数1がx軸
    //引数2がy軸
    //デフォルトはノーマル
    public static void ReverseCamera(bool x, bool y)
    {
        if (x)
        {
            ReverseX = -1;
        }
        else
        {
            ReverseX = 1;
        }

        if (y)
        {
            ReverseY = -1;
        }
        else
        {
            ReverseY = 1;
        }
    }
}
