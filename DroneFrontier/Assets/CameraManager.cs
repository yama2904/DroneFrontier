using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    const float MAX_CAMERA_SPEED = 2.0f;    //カメラ感度の最高値
    const float MIN_CAMERA_SPEED = 0.2f;    //カメラ感度の最低値
    static float baseSpeed = 1.0f;        //カメラ感度(内部値)

    //ノーマルなら1
    //リバースなら-1
    static short reverseX = 1;  //x軸
    static short reverseY = 1;  //ÿ軸
    public static short ReverseX
    {
        get { return reverseX; }
    }
    public static short ReverseY
    {
        get { return reverseY; }
    }

    //シーン間をまたいでもCameraManagerオブジェクトが消えない処理
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        GameObject manager = GameObject.Instantiate(Resources.Load("CameraManager")) as GameObject;
        GameObject.DontDestroyOnLoad(manager);
    }

    void Start()
    {        
    }
    
    void Update()
    {
    }

    //カメラの感度を設定
    public static void SetBaseSpeed(float x)
    {
        if(x < 0)
        {
            x = 0;
        }
        if(x > 1.0f)
        {
            x = 1.0f;
        }
        baseSpeed = x;
    }

    //SetBaseSpeedで設定したカメラ感度を取得
    public static float GetBaseSpeed()
    {
        return baseSpeed;
    }

    //実際のカメラ感度を取得
    //基本的にゲーム中で使うのはこっち
    public static float GetCameraSpeed
    {
        get { return (baseSpeed + MIN_CAMERA_SPEED) + MAX_CAMERA_SPEED; }
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
            reverseX = -1;
        }
        else
        {
            reverseX = 1;
        }

        if (y)
        {
            reverseY = -1;
        }
        else
        {
            reverseY = 1;
        }
    }
}
