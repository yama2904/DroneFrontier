using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    GameObject mainCamera;

    public static float RotateSpeed { get; set; } = 3.0f;   //カメラの回転速度
    public static float MoveSpeed { get; set; } = 5.0f;     //カメラの移動速度
    [SerializeField] float scrollSpeed = 6.0f;  //カメラのズーム速度

    Vector2 mousePosPrev;   //1フレーム前のマウスの位置
    Vector3 screenPos;
    float scroll;           //マウスのスクロール変数

    void Start()
    {
        mainCamera = Camera.main.gameObject;
        mousePosPrev = new Vector2(0, 0);
        screenPos = new Vector3(0, 0, 0);
    }

    void Update()
    {
        //カメラの移動
        if (Input.GetMouseButtonDown(0))
        {
            mousePosPrev = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            float x = (mousePosPrev.x - Input.mousePosition.x) / Screen.width;
            float y = (mousePosPrev.y - Input.mousePosition.y) / Screen.height;

            //クリックしている間mousePosPrevの更新
            mousePosPrev = Input.mousePosition;

            //カメラの向きに合わせて移動させる
            Vector3 move = mainCamera.transform.rotation.normalized * new Vector2(x, y) * MoveSpeed;
            mainCamera.transform.position += move;
        }

        //カメラの回転
        if (Input.GetMouseButton(1))
        {
            Vector3 angle = new Vector3(Input.GetAxis("Mouse X") * RotateSpeed, Input.GetAxis("Mouse Y") * RotateSpeed, 0);

            mainCamera.transform.RotateAround(mainCamera.transform.position, Vector3.up, angle.x);
            mainCamera.transform.RotateAround(mainCamera.transform.position, mainCamera.transform.right * -1, angle.y);
        }

        //カメラのズーム
        scroll = Input.GetAxis("Mouse ScrollWheel");
        mainCamera.transform.position += mainCamera.transform.forward * scroll * scrollSpeed;
    }
}