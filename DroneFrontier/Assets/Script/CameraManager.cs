using UnityEngine;

public class CameraManager : MonoBehaviour
{
    //const float MAX_CAMERA_SPEED = 2.0f;    //カメラ感度の最高値
    private const float MIN_CAMERA_SPEED = 0.3f;    //カメラ感度の最低値

    public static short ReverseX { get; private set; } = 1;
    public static short ReverseY { get; private set; } = 1;

    //内部のカメラ感度
    static float baseSpeed = 1.0f;
    public static float BaseSpeed
    {
        get { return baseSpeed; }
        set
        {
            float s = value;
            if (value < 0)
            {
                s = 0;
            }
            if (value > 1.0f)
            {
                s = 1.0f;
            }
            baseSpeed = s;
        }
    }
    
    //実際のカメラ感度を取得
    //基本的にゲーム中で使うのはこっち
    public static float CameraSpeed
    {
        get { return (BaseSpeed + MIN_CAMERA_SPEED); }
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
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
