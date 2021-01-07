using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDroneScript : MonoBehaviour
{
    protected Rigidbody _Rigidbody = null;
    protected Transform cacheTransform = null;  //キャッシュ用

    //移動用
    [SerializeField] float MoveSpeed = 20;                  //移動速度
    [SerializeField] float MaxSpeed = 30;     //最高速度

    //回転用
    [SerializeField] float RotateSpeed = 3.0f;
    [SerializeField] float LimitCameraTiltX = 40.0f;

    //ブースト用
    [SerializeField] float boostAccele = 2.0f;      //ブーストの加速度

    //マウスのカーソルをロックしているか
    bool isCursorLock = true;


    void Start()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        cacheTransform = transform;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        //カメラのカーソルロックの変更
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCursorLock = !isCursorLock;
            if (isCursorLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Debug.Log("カメラロック");
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("カメラロック解除");
            }
        }


        //回転処理
        if (isCursorLock)
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            Rotate(x, y, RotateSpeed);
        }


        //移動処理
        if (Input.GetKey(KeyCode.W))
        {
            Move(MoveSpeed, MaxSpeed, cacheTransform.forward);
        }
        if (Input.GetKey(KeyCode.A))
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * cacheTransform.forward;
            Move(MoveSpeed, MaxSpeed, left);
        }
        if (Input.GetKey(KeyCode.S))
        {
            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * cacheTransform.forward;
            Move(MoveSpeed, MaxSpeed, backward);
        }
        if (Input.GetKey(KeyCode.D))
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * cacheTransform.forward;
            Move(MoveSpeed, MaxSpeed, right);
        }
        if (Input.mouseScrollDelta.y != 0)
        {
            Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
            Vector3 upward = upAngle.normalized * Vector3.forward;
            Move(MoveSpeed * 4 * Input.mouseScrollDelta.y, MaxSpeed * 4, upward);
        }


        //ブースト使用
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ModifySpeed(boostAccele);
            Debug.Log("ブースト使用");
        }
        if (Input.GetKeyUp(KeyCode.Q))
        {
            ModifySpeed(1 / boostAccele);
            Debug.Log("ブースト解除");
        }
    }


    //移動処理
    void Move(float speed, float _maxSpeed, Vector3 direction)
    {
        _Rigidbody.AddForce(direction * speed + (direction * speed - _Rigidbody.velocity), ForceMode.Force);
    }

    //回転処理
    void Rotate(float valueX, float valueY, float speed)
    {
        if (MainGameManager.IsCursorLock)
        {
            Vector3 angle = new Vector3(valueX * speed, valueY * speed, 0);

            //カメラの左右回転
            cacheTransform.RotateAround(cacheTransform.position, Vector3.up, angle.x);

            //カメラの上下の回転に制限をかける
            Vector3 localAngle = cacheTransform.localEulerAngles;
            localAngle.x += angle.y * -1;
            if (localAngle.x > LimitCameraTiltX && localAngle.x < 180)
            {
                localAngle.x = LimitCameraTiltX;
            }
            if (localAngle.x < 360 - LimitCameraTiltX && localAngle.x > 180)
            {
                localAngle.x = 360 - LimitCameraTiltX;
            }
            cacheTransform.localEulerAngles = localAngle;
        }
    }

    //スピードを変更する
    void ModifySpeed(float speedMgnf)
    {
        MoveSpeed *= speedMgnf;
        MaxSpeed *= speedMgnf;
    }
}
