using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public const string PLAYER_TAG = "Player";
    float moveSpeed = 20.0f;    //移動速度
    float maxSpeed = 30.0f;    //最高速度

    Rigidbody _rigidbody;
    AtackBase atack;    //攻撃技

    //オブジェクトの名前
    public static string ObjectName { get; private set; } = "";

    //デバッグ用
    int atackType;
    bool isQ = false;

    void Start()
    {
        ObjectName = name;
        _rigidbody = GetComponent<Rigidbody>();

        AtackManager.CreateAtack(out GameObject o, AtackManager.Weapon.SHOTGUN); //Gatlingの作成
        o.transform.parent = transform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        o.transform.localPosition = new Vector3(0, 0, 0);
        o.transform.localRotation = Quaternion.Euler(0, 0, 0); ;

        //コンポーネントの取得
        atack = o.GetComponent<AtackBase>();

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

        if (Input.GetKey(KeyCode.W))
        {
            //_rigidbody.transform.Translate(0, 0, moveSpeed);

            float velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
            float maxDistance = Vector3.Distance(transform.position, transform.position + (transform.forward * maxSpeed));
            if (velocityDistance < maxDistance)
            {
                _rigidbody.AddForce(transform.forward * moveSpeed, ForceMode.Force);
            }

            if (isQ)
            {
                Vector3 diff = transform.forward * moveSpeed - _rigidbody.velocity;
                {
                    _rigidbody.AddForce(transform.forward * moveSpeed + (transform.forward * moveSpeed - _rigidbody.velocity), ForceMode.Force);
                }
            }
            Debug.Log("velocity: " + velocityDistance);
            Debug.Log("max: " + maxDistance);
        }
        if (Input.GetKey(KeyCode.A))
        {
            //_rigidbody.transform.Translate(moveSpeed * -1, 0, 0);

            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * transform.forward;
            float velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
            float maxDistance = Vector3.Distance(transform.position, transform.position + (left * maxSpeed));
            if (velocityDistance < maxDistance)
            {
                _rigidbody.AddForce(left * moveSpeed, ForceMode.Force);
            }

            if (isQ)
            {
                Vector3 diff = left * moveSpeed - _rigidbody.velocity;
                {
                    _rigidbody.AddForce(transform.forward * moveSpeed + (transform.forward * moveSpeed - _rigidbody.velocity), ForceMode.Force);
                }
            }
        }
        if (Input.GetKey(KeyCode.S))
        {
            //_rigidbody.transform.Translate(0, 0, moveSpeed * -1);

            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * transform.forward;
            float velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
            float maxDistance = Vector3.Distance(transform.position, transform.position + (backward * maxSpeed));
            if (velocityDistance < maxDistance)
            {
                _rigidbody.AddForce(backward * moveSpeed, ForceMode.Force);
            }

            if (isQ)
            {
                Vector3 diff = backward * moveSpeed - _rigidbody.velocity;
                {
                    _rigidbody.AddForce(transform.forward * moveSpeed + (transform.forward * moveSpeed - _rigidbody.velocity), ForceMode.Force);
                }
            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            //_rigidbody.transform.Translate(moveSpeed, 0, 0);

            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * transform.forward;
            float velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
            float maxDistance = Vector3.Distance(transform.position, transform.position + (right * maxSpeed));
            if (velocityDistance < maxDistance)
            {
                _rigidbody.AddForce(right * moveSpeed, ForceMode.Force);
            }

            if (isQ)
            {
                Vector3 diff = right * moveSpeed - _rigidbody.velocity;
                {
                    _rigidbody.AddForce(transform.forward * moveSpeed + (transform.forward * moveSpeed - _rigidbody.velocity), ForceMode.Force);
                }
            }
        }

        //上下の移動
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _rigidbody.transform.Translate(0, Input.mouseScrollDelta.y * moveSpeed, 0);

        //攻撃
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LockOn.TrackingSpeed *= 0.5f;
            PlayerCameraController.RotateSpeed *= 0.5f;
            PlayerCameraController.MoveSpeed *= 0.5f;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            atack.Shot(transform);
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            LockOn.TrackingSpeed *= 2;
            PlayerCameraController.RotateSpeed *= 2;
            PlayerCameraController.MoveSpeed *= 2;
        }

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
            Destroy(atack.gameObject);

            if (++atackType >= (int)AtackManager.Weapon.NONE)
            {
                atackType = 0;
            }
            AtackManager.CreateAtack(out GameObject o, (AtackManager.Weapon)atackType);
            atack = o.GetComponent<AtackBase>();

            //Playerの子オブジェクトに設定
            o.transform.parent = transform;

            //位置と角度の初期設定
            o.transform.localPosition = new Vector3(0, 0, 0);
            o.transform.localRotation = Quaternion.Euler(0, 0, 0); ;
        }
    }
}
