using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] bool isTime = true;
    float deltaTime;
    int count;

    //カメラ用変数
    [SerializeField] GameObject subCamera = null;
    float MoveSpeed { get; set; } = 3.0f;   //カメラの移動速度
    float ScrollSpeed { get; set; } = 3.0f;
    GameObject mainCamera;
    Vector2 mousePosPrev;   //1フレーム前のマウスの位置
    float scroll;           //マウスのスクロール変数
    bool isMainCamera = true;

    void Start()
    {
        deltaTime = 0;
        count = 0;

        mainCamera = Camera.main.gameObject;
        mousePosPrev = new Vector2(0, 0);
    }
    
    void Update()
    {
        if (isTime)
        {
            if (deltaTime >= 1.0f)
            {
                Debug.Log(++count + "秒");
                deltaTime = 0;
            }
        }

        if(Input.GetKeyDown(KeyCode.K))
        {
            isMainCamera = !isMainCamera;
            if (isMainCamera)
            {
                Camera.main.depth = 1;
                subCamera.GetComponent<Camera>().depth = 0;
            }
            else
            {
                Camera.main.depth = 0;
                subCamera.GetComponent<Camera>().depth = 1;
            }
        }

        if (!isMainCamera)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                subCamera.transform.Translate(0, 0, 0.01f);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                subCamera.transform.Translate(-0.01f, 0, 0);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                subCamera.transform.Translate(0, 0, -0.01f);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                subCamera.transform.Translate(0.01f, 0, 0);
            }
            if (Input.GetKey(KeyCode.RightShift))
            {
                subCamera.transform.Translate(0, 0.01f, 0);
            }
            if (Input.GetKey(KeyCode.RightControl))
            {
                subCamera.transform.Translate(0, -0.01f, 0);
            }
        }


            //if (Input.GetMouseButtonDown(0))
            //{
            //    Debug.Log("左クリック押");
            //}

            //if (Input.GetMouseButtonUp(0))
            //{
            //    Debug.Log("左クリック離");
            //}


            //if (Input.GetMouseButtonDown(1))
            //{
            //    Debug.Log("右クリック押");
            //}

            //if (Input.GetMouseButtonUp(1))
            //{
            //    Debug.Log("右クリック離");
            //}

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

            ////カメラのズーム
            //scroll = Input.GetAxis("Mouse ScrollWheel");
            //mainCamera.transform.position += mainCamera.transform.forward * scroll * ScrollSpeed;

            deltaTime += Time.deltaTime;
    }
}
