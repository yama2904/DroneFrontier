using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] GameObject player = null;
    GameObject mainCamera;

    public static float RotateSpeed { get; set; } = 3.0f;   //カメラの回転速度
    [SerializeField] float limitCameraTiltX = 40.0f;        //カメラのX軸の傾き上限

    Vector2 mousePosPrev;   //1フレーム前のマウスの位置
    Vector3 screenPos;
    float scroll;           //マウスのスクロール変数


    //デバッグ用
    bool isRotate = true;


    void Start()
    {
        mainCamera = Camera.main.gameObject;
        mousePosPrev = new Vector2(0, 0);
        screenPos = new Vector3(0, 0, 0);
    }

    void Update()
    {
        if (MainGameManager.IsConfig)
        {
            return;
        }

        ////カメラの移動
        //if (Input.GetMouseButtonDown(0))
        //{
        //    mousePosPrev = Input.mousePosition;
        //}
        //if (Input.GetMouseButton(0))
        //{
        //    float x = (mousePosPrev.x - Input.mousePosition.x) / Screen.width;
        //    float y = (mousePosPrev.y - Input.mousePosition.y) / Screen.height;

        //    //クリックしている間mousePosPrevの更新
        //    mousePosPrev = Input.mousePosition;

        //    //カメラの向きに合わせて移動させる
        //    Vector3 move = mainCamera.transform.rotation.normalized * new Vector2(x, y) * MoveSpeed;
        //    mainCamera.transform.position += move;
        //}

        //カメラの回転
        if (isRotate)
        {
            Vector3 angle = new Vector3(Input.GetAxis("Mouse X") * RotateSpeed, Input.GetAxis("Mouse Y") * RotateSpeed, 0);

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

        ////カメラのズーム
        //scroll = Input.GetAxis("Mouse ScrollWheel");
        //mainCamera.transform.position += mainCamera.transform.forward * scroll * scrollSpeed;


        //デバッグ用
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isRotate = !isRotate;
        }
    }
}