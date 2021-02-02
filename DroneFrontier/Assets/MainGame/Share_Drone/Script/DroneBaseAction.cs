using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DroneBaseAction : NetworkBehaviour
{
    //コンポーネント用
    Rigidbody _rigidbody = null;
    Transform cacheTransform = null;  //キャッシュ用
    DroneChildObject childObject = null;
    public AudioListener Listener { get; private set; } = null;

    //回転用
    [SerializeField, Tooltip("上下の回転制限角度")] float limitCameraTiltX = 40f;

    //カメラ
    [SerializeField] Camera _camera = null;
    public Camera _Camera { get { return _camera; } }


    #region Init

    public override void OnStartClient()
    {
        base.OnStartClient();

        //AudioListenerの初期化
        Listener = GetComponent<AudioListener>();
        if (!isLocalPlayer)
        {
            Listener.enabled = false;
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _camera.depth++;
    }

    void Awake()
    {
        cacheTransform = transform; //キャッシュ用
        _rigidbody = GetComponent<Rigidbody>();
        cacheTransform = transform;
        childObject = GetComponent<DroneChildObject>();
    }

    void Start() { }

    #endregion


    //移動処理
    public void Move(float speed, Vector3 direction)
    {
        _rigidbody.AddForce(direction * speed + (direction * speed - _rigidbody.velocity), ForceMode.Force);
    }

    //ドローンを徐々に回転させる
    public void RotateDroneObject(Quaternion rotate, float speed)
    {
       childObject.GetChild(DroneChildObject.Child.DRONE_OBJECT).localRotation = Quaternion.Slerp(childObject.GetChild(DroneChildObject.Child.DRONE_OBJECT).localRotation, rotate, speed);
    }

    //回転処理
    public void Rotate(float valueX, float valueY, float speed)
    {
        if (MainGameManager.IsCursorLock)
        {
            Vector3 angle = new Vector3(valueX * speed * CameraManager.ReverseX, valueY * speed * CameraManager.ReverseY, 0);

            //カメラの左右回転
            cacheTransform.RotateAround(cacheTransform.position, Vector3.up, angle.x);

            //カメラの上下の回転に制限をかける
            Vector3 localAngle = cacheTransform.localEulerAngles;
            localAngle.x += angle.y * -1;
            if (localAngle.x > limitCameraTiltX && localAngle.x < 180)
            {
                localAngle.x = limitCameraTiltX;
            }
            if (localAngle.x < 360 - limitCameraTiltX && localAngle.x > 180)
            {
                localAngle.x = 360 - limitCameraTiltX;
            }
            cacheTransform.localEulerAngles = localAngle;
        }
    }

    //スピードを変更する
    public float ModifySpeed(float speed, float min, float max, float speedMgnf)
    {
        speed *= speedMgnf;
        if (speed > max)
        {
            speed = max;
        }
        if (speed < min)
        {
            speed = min;
        }
        return speed;
    }
}
