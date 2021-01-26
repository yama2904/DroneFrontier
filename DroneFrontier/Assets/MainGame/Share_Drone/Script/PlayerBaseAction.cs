using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerBaseAction : NetworkBehaviour
{
    public bool IsLocalPlayer { get { return isLocalPlayer; } }

    //コンポーネント用
    Rigidbody _rigidbody = null;
    Transform cacheTransform = null;  //キャッシュ用

    //ドローンが移動した際にオブジェクトが傾く処理用
    [SerializeField] Transform droneObject = null;

    //回転用
    [SerializeField, Tooltip("上下の回転制限角度")] float limitCameraTiltX = 40f;

    //カメラ
    [SerializeField] Camera _camera = null;


    #region Init

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsLocalPlayer)
        {
            GetComponent<AudioListener>().enabled = false;
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
    }

    void Start() { }

    #endregion


    void Update()
    {
        if (!isLocalPlayer) return;
    }


    //移動処理
    public void Move(float speed, Vector3 direction)
    {
        _rigidbody.AddForce(direction * speed + (direction * speed - _rigidbody.velocity), ForceMode.Force);
    }

    #region RotaetDroneObject

    [Command]
    public void CmdCallRotateDroneObject(Quaternion rotate, float speed)
    {
        RpcRotateDroneObject(rotate, speed);
    }

    [ClientRpc]
    void RpcRotateDroneObject(Quaternion rotate, float speed)
    {
        droneObject.localRotation = Quaternion.Slerp(droneObject.localRotation, rotate, speed);
    }

    #endregion

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
