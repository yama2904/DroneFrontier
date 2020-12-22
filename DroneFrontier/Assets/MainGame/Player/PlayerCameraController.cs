using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] GameObject player = null;

    public static float RotateSpeed { get; set; } = 3.0f;   //カメラの回転速度
    [SerializeField] float limitCameraTiltX = 40.0f;        //カメラのX軸の傾き上限
    


    void Start()
    {
        //デバッグ用
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        //設定画面を開いているときはカメラ操作を行わない
        if (MainGameManager.IsConfig)
        {
            return;
        }

        //カメラの回転
        if (MainGameManager.IsCursorLock)
        {
            Vector3 angle = new Vector3(Input.GetAxis("Mouse X") * RotateSpeed, Input.GetAxis("Mouse Y") * RotateSpeed, 0);

            //カメラの左右回転
            player.transform.RotateAround(player.transform.position, Vector3.up, angle.x);
            
            //カメラの上下の回転に制限をかける
            Vector3 localAngle = player.transform.localEulerAngles;
            localAngle.x += angle.y * -1;
            if(localAngle.x > limitCameraTiltX && localAngle.x < 180)
            {
                localAngle.x = limitCameraTiltX;
            }
            if(localAngle.x < 360 - limitCameraTiltX && localAngle.x > 180)
            {
                localAngle.x = 360 - limitCameraTiltX;
            }
            player.transform.localEulerAngles = localAngle;
        }
    }
}