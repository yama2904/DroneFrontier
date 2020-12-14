using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public const string PLAYER_TAG = "Player";
    float moveSpeed = 20.0f;    //移動速度
    float maxSpeed = 30.0f;   //最高速度

    Rigidbody _rigidbody;

    enum Weapon
    {
        MAIN,
        SUB,

        NONE
    }
    AtackBase[] weapons;  //ウェポン群

    //オブジェクトの名前
    public static string ObjectName { get; private set; } = "";

    //デバッグ用
    int atackType;
    bool isQ = false;

    void Start()
    {
        ObjectName = name;
        _rigidbody = GetComponent<Rigidbody>();
        weapons = new AtackBase[(int)Weapon.NONE];

        //メインウェポンの処理
        AtackManager.CreateAtack(out GameObject o, AtackManager.Weapon.GATLING);    //Gatlingの生成
        o.transform.parent = transform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        o.transform.localPosition = new Vector3(0, 0, 0);
        o.transform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        weapons[(int)Weapon.MAIN] = o.GetComponent<AtackBase>();


        //サブウェポンの処理
        AtackManager.CreateAtack(out o, AtackManager.Weapon.SHOTGUN);    //Shotgunの作成
        o.transform.parent = transform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        o.transform.localPosition = new Vector3(0, 0, 0);
        o.transform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        weapons[(int)Weapon.SUB] = o.GetComponent<AtackBase>();


        //デバッグ用
        atackType = (int)AtackManager.Weapon.SHOTGUN;
    }

    void Update()
    {
        //デバッグ用
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                isQ = !isQ;
            }
        }

        if (MainGameManager.IsConfig)
        {
            return;
        }

        //移動処理
        Move(moveSpeed, maxSpeed);

        //攻撃処理
        UseWeapon(Weapon.MAIN);
        UseWeapon(Weapon.SUB);

        //ロックオン
        if (Input.GetKey(KeyCode.LeftShift))
        {
            LockOn.StartLockOn();
        }
        //ロックオン解除
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            LockOn.ReleaseLockOn();
        }

        //デバッグ用
        //武器切り替え
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Destroy(weapons[(int)Weapon.SUB].gameObject);

            if (++atackType >= (int)AtackManager.Weapon.NONE)
            {
                atackType = 0;
            }
            AtackManager.CreateAtack(out GameObject o, (AtackManager.Weapon)atackType);
            weapons[(int)Weapon.SUB] = o.GetComponent<AtackBase>();

            //Playerの子オブジェクトに設定
            o.transform.parent = transform;

            //位置と角度の初期設定
            o.transform.localPosition = new Vector3(0, 0, 0);
            o.transform.localRotation = Quaternion.Euler(0, 0, 0); ;
        }
    }

    void Move(float speed, float _maxSpeed)
    {
        float velocityDistance = 0;
        float maxDistance = 0;
        if (Input.GetKey(KeyCode.W))
        {
            ////あとで
            //Vector3 move;
            //if (!isQ)
            //{
            //    move = transform.forward * moveSpeed;
            //}
            //else
            //{
            //    move = transform.forward * moveSpeed + (transform.forward * moveSpeed - _rigidbody.velocity);
            //}

            //velocityDistance = Vector3.Distance(transform.position, transform.position + move + _rigidbody.velocity);
            //maxDistance = Vector3.Distance(transform.position, transform.position + (transform.forward * _maxSpeed));
            //if (velocityDistance < maxDistance)
            //{
            //    _rigidbody.AddForce(move, ForceMode.Force);
            //}

            //Debug.Log("velocity: " + velocityDistance);
            //Debug.Log("max: " + maxDistance);
            //Debug.Log("move: " + move.normalized);


            velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
            maxDistance = Vector3.Distance(transform.position, transform.position + (transform.forward * _maxSpeed));
            if (!isQ)
            {
                if (velocityDistance < maxDistance)
                {
                    _rigidbody.AddForce(transform.forward * moveSpeed, ForceMode.Force);
                }
            }
            else
            {
                {
                    _rigidbody.AddForce(transform.forward * moveSpeed + (transform.forward * moveSpeed - _rigidbody.velocity), ForceMode.Force);
                }
            }

        }
        if (Input.GetKey(KeyCode.A))
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * transform.forward;
            velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
            maxDistance = Vector3.Distance(transform.position, transform.position + (left * _maxSpeed));

            if (!isQ)
            {
                if (velocityDistance < maxDistance)
                {
                    _rigidbody.AddForce(left * moveSpeed, ForceMode.Force);
                }
            }
            else
            {
                _rigidbody.AddForce(left * moveSpeed + (left * moveSpeed - _rigidbody.velocity), ForceMode.Force);
            }
        }
        if (Input.GetKey(KeyCode.S))
        {
            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * transform.forward;
            velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
            maxDistance = Vector3.Distance(transform.position, transform.position + (backward * _maxSpeed));

            if (!isQ)
            {
                if (velocityDistance < maxDistance)
                {
                    _rigidbody.AddForce(backward * moveSpeed, ForceMode.Force);
                }
            }
            else
            {
                _rigidbody.AddForce(backward * moveSpeed + (backward * moveSpeed - _rigidbody.velocity), ForceMode.Force);
            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * transform.forward;
            velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
            maxDistance = Vector3.Distance(transform.position, transform.position + (right * _maxSpeed));

            if (!isQ)
            {
                if (velocityDistance < maxDistance)
                {
                    _rigidbody.AddForce(right * moveSpeed, ForceMode.Force);
                }
            }
            else
            {
                //Vector3 diff = right * moveSpeed - _rigidbody.velocity;
                _rigidbody.AddForce(right * moveSpeed + (right * moveSpeed - _rigidbody.velocity), ForceMode.Force);
            }
        }

        //上下の移動
        //float scroll = Input.GetAxis("Mouse ScrollWheel");

        //デバッグ用
        float s = moveSpeed * 2;
        float ms = _maxSpeed * 2;    //maxspeed
        //

        Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
        Vector3 upward = upAngle.normalized * Vector3.forward;
        velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
        maxDistance = Vector3.Distance(transform.position, transform.position + (upward * ms));

        if (!isQ)
        {
            if (velocityDistance < maxDistance)
            {
                _rigidbody.AddForce(upward * s * Input.mouseScrollDelta.y, ForceMode.Force);
            }
        }
        else
        {
            Vector3 diff = upward * s - _rigidbody.velocity;
            _rigidbody.AddForce(upward * s * Input.mouseScrollDelta.y + (upward * s * Input.mouseScrollDelta.y - _rigidbody.velocity), ForceMode.Force);
        }
    }


    void UseWeapon(Weapon weapon)
    {
        //メインウェポン攻撃
        if (weapon == Weapon.MAIN)
        {
            //攻撃を始めたら移動速度を下げる
            if (Input.GetMouseButtonDown(0))
            {
                if (Input.GetMouseButton(1))
                {
                    //サブウェポンを使用中なら処理を行わない
                }
                else
                {
                    LockOn.TrackingSpeed *= 0.5f;
                    PlayerCameraController.RotateSpeed *= 0.5f;
                    moveSpeed *= 0.5f;
                }
            }
            //左クリックでメインウェポン攻撃
            if (Input.GetMouseButton(0))
            {
                weapons[(int)weapon].Shot(transform);
            }
            //攻撃をやめたら移動速度を元に戻す
            if (Input.GetMouseButtonUp(0))
            {
                if (Input.GetMouseButton(1))
                {
                    //サブウェポンを使用中なら処理を行わない
                }
                else
                {
                    LockOn.TrackingSpeed *= 2;
                    PlayerCameraController.RotateSpeed *= 2;
                    moveSpeed *= 2;
                }
            }
        }

        //サブウェポン攻撃
        else if (weapon == Weapon.SUB)
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (Input.GetMouseButton(0))
                {
                    //メインウェポンを使用中なら処理を行わない
                }
                else
                {
                    LockOn.TrackingSpeed *= 0.5f;
                    PlayerCameraController.RotateSpeed *= 0.5f;
                    moveSpeed *= 0.5f;
                }
            }
            //右クリックでサブウェポン攻撃
            if (Input.GetMouseButton(1))
            {
                weapons[(int)Weapon.SUB].Shot(transform);
            }
            //攻撃をやめたら移動速度を元に戻す
            if (Input.GetMouseButtonUp(1))
            {
                if (Input.GetMouseButton(0))
                {
                    //メインウェポンを使用中なら処理を行わない
                }
                else
                {
                    LockOn.TrackingSpeed *= 2;
                    PlayerCameraController.RotateSpeed *= 2;
                    moveSpeed *= 2;
                }
            }
        }
    }
}