using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] GameObject shotgun = null;

    public const string PLAYER_TAG = "Player";
    [SerializeField] float moveSpeed = 0.1f;    //移動速度

    Rigidbody _rigidbody;
    AtackBase atack;    //攻撃技

    //オブジェクトの名前
    public static string ObjectName { get; private set; } = "";

    //デバッグ用
    int atackType;

    void Start()
    {
        ObjectName = name;
        _rigidbody = GetComponent<Rigidbody>();

        AtackManager.CreateAtack(out GameObject o, AtackManager.Weapon.GATLING); //Gatlingの作成
        o.transform.parent = transform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        o.transform.localPosition = new Vector3(0, 0, 0);
        o.transform.localRotation = Quaternion.Euler(0, 0, 0); ;

        //コンポーネントの取得
        atack = o.GetComponent<Gatling>();

        //デバッグ用
        atackType = (int)AtackManager.Weapon.GATLING;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            _rigidbody.transform.Translate(0, 0, moveSpeed);
        }
        if (Input.GetKey(KeyCode.A))
        {
            _rigidbody.transform.Translate(moveSpeed * -1, 0, 0);
        }
        if (Input.GetKey(KeyCode.S))
        {
            _rigidbody.transform.Translate(0, 0, moveSpeed * -1);
        }
        if (Input.GetKey(KeyCode.D))
        {
            _rigidbody.transform.Translate(moveSpeed, 0, 0);
        }

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

            o.transform.parent = transform;

            //位置と角度の初期設定
            o.transform.localPosition = new Vector3(0, 0, 0);
            o.transform.localRotation = Quaternion.Euler(0, 0, 0); ;

            //コンポーネントの取得
            if (atackType == (int)AtackManager.Weapon.GATLING)
            {
                atack = o.GetComponent<Gatling>();
            }
            else if (atackType == (int)AtackManager.Weapon.MISSILE)
            {
                atack = o.GetComponent<MissieShot>();
            }
            else if (atackType == (int)AtackManager.Weapon.LASER)
            {
                atack = o.GetComponent<Laser>();
            }
        }


        //デバッグ用
        if (Input.GetKey(KeyCode.E))
        {
            //ショットガン
            shotgun.GetComponent<Shotgun>().Shot(transform);
        }
    }
}
