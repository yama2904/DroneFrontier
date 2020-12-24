using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * 公開変数
 * float HP                  ドローンのHP
 * float MoveSpeed           移動速度
 * float MaxSpeed            最高速度
 * Barrier Barrier           プレイヤーのバリア
 * 
 * 公開メソッド
 * void Damage(float power)  プレイヤーにダメージを与える
 */
public class Player : MonoBehaviour
{
    public const string PLAYER_TAG = "Player";   //タグ名    

    public float HP { get; private set; } = 10;      //HP
    public float MoveSpeed = 50.0f;                  //移動速度
    public float MaxSpeed { get; set; } = 30.0f;     //最高速度

    Rigidbody _rigidbody = null;
    Transform cacheTransform = null;


    //武器
    enum Weapon
    {
        MAIN,   //メイン武器
        SUB,    //サブ武器

        NONE
    }
    AtackBase[] weapons;      //ウェポン群
    bool[] isUsingWeapons;    //使用中の武器

    //バリア
    [SerializeField] Barrier barrier = null;
    public Barrier Barrier { get; private set; } = null;

    //アイテム
    enum ItemNum
    {
        ITEM_1,   //アイテム枠1
        ITEM_2,   //アイテム枠2

        NONE
    }
    Item[] items;


    ////状態異常
    //public enum Abnormal
    //{
    //    STUN,
    //    JAMMING,
    //    SPEED_DOWN,
    //    BARRIER_WEAK,

    //    NONE
    //}
    //bool[] isAbnormals;   //状態異常が付与されているか


    //デバッグ用
    int atackType = (int)AtackManager.Weapon.SHOTGUN;
    bool isQ = true;
    Vector3 initPos;

    void Start()
    {
        cacheTransform = transform;
        _rigidbody = GetComponent<Rigidbody>();
        Barrier = barrier;

        //武器の初期化
        weapons = new AtackBase[(int)Weapon.NONE];
        isUsingWeapons = new bool[(int)Weapon.NONE];
        for (int i = 0; i < (int)Weapon.NONE; i++)
        {
            isUsingWeapons[i] = false;
        }

        //メインウェポンの処理
        AtackManager.CreateAtack(out GameObject main, AtackManager.Weapon.GATLING);    //Gatlingの生成
        main.transform.parent = cacheTransform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        main.transform.localPosition = new Vector3(0, 0, 0);
        main.transform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase abM = main.GetComponent<AtackBase>(); //名前省略
        abM.notHitObject = gameObject;    //自分をヒットさせない
        weapons[(int)Weapon.MAIN] = abM;


        //サブウェポンの処理
        AtackManager.CreateAtack(out GameObject sub, AtackManager.Weapon.SHOTGUN);    //Shotgunの作成
        sub.transform.parent = cacheTransform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        sub.transform.localPosition = new Vector3(0, 0, 0);
        sub.transform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase abS = sub.GetComponent<AtackBase>();
        abS.notHitObject = gameObject;    //自分をヒットさせない
        weapons[(int)Weapon.SUB] = abS;

        items = new Item[(int)ItemNum.NONE];


        //デバッグ用
        initPos = cacheTransform.position;
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

        //レーダー使用
        if (Input.GetKey(KeyCode.Space))
        {
            Radar.StartRadar();
        }
        //レーダー使用
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Radar.ReleaseRadar();
        }


        //設定画面中はここより下の処理は行わない
        if (MainGameManager.IsConfig)
        {
            return;
        }


        //攻撃処理しか使わない簡易メソッド
        Action<float> ModifySpeeds = (x) =>
        {
            MoveSpeed *= x;
            PlayerCameraController.RotateSpeed *= x;
            LockOn.TrackingSpeed *= x;
        };

        //メイン武器攻撃
        if (Input.GetMouseButtonDown(0))
        {
            //サブ武器を使用していない場合は移動速度と回転速度とロックオンの追従速度を下げる
            if (!isUsingWeapons[(int)Weapon.SUB])
            {
                ModifySpeeds(0.5f);
                isUsingWeapons[(int)Weapon.MAIN] = true;
            }
        }
        if (Input.GetMouseButton(0))
        {
            UseWeapon(Weapon.MAIN);     //メインウェポン攻撃
        }
        if (Input.GetMouseButtonUp(0))
        {
            //メインもサブも使用していないなら速度を戻す
            if (!isUsingWeapons[(int)Weapon.SUB])
            {
                ModifySpeeds(2f);
            }
            isUsingWeapons[(int)Weapon.MAIN] = false;
        }

        //サブ武器攻撃
        if (Input.GetMouseButtonDown(1))
        {
            //メイン武器を使用していない場合は移動速度と回転速度とロックオンの追従速度を下げる
            if (!isUsingWeapons[(int)Weapon.MAIN])
            {
                ModifySpeeds(0.5f);
                isUsingWeapons[(int)Weapon.SUB] = true;
            }
        }
        if (Input.GetMouseButton(1))
        {
            UseWeapon(Weapon.SUB);      //サブウェポン攻撃
        }
        //メインもサブも使用していないなら速度を戻す
        if (Input.GetMouseButtonUp(1))
        {
            if (!isUsingWeapons[(int)Weapon.MAIN])
            {
                ModifySpeeds(2f);
            }
            isUsingWeapons[(int)Weapon.SUB] = false;
        }


        //ブースト使用
        if (Input.GetKeyUp(KeyCode.Q))
        {
            //バトルモードの場合
            if (MainGameManager.Mode == MainGameManager.GameMode.BATTLE)
            {

            }

            //レースモードの場合
            else if (MainGameManager.Mode == MainGameManager.GameMode.RACE)
            {

            }
        }

        //アイテム使用
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            int num = (int)ItemNum.ITEM_1;  //名前省略

            //アイテム枠1にアイテムを持っていたら使用
            if (items[num] != null)
            {
                items[num].UseItem(this);


                //デバッグ用
                Debug.Log("アイテム使用");
            }
        }
        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            int num = (int)ItemNum.ITEM_2;  //名前省略

            //アイテム枠2にアイテムを持っていたら使用
            if (items[num] != null)
            {
                items[num].UseItem(this);


                //デバッグ用
                Debug.Log("アイテム使用");
            }
        }

        //デバッグ用
        //武器切り替え
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //今持っているサブ武器を削除
            Destroy(weapons[(int)Weapon.SUB].gameObject);

            //次の武器に切り替える
            if (++atackType >= (int)AtackManager.Weapon.NONE)
            {
                atackType = 0;
            }
            AtackManager.CreateAtack(out GameObject o, (AtackManager.Weapon)atackType);

            //Playerの子オブジェクトに設定
            o.transform.parent = cacheTransform;

            //位置と角度の初期設定
            o.transform.localPosition = new Vector3(0, 0, 0);
            o.transform.localRotation = Quaternion.Euler(0, 0, 0);

            AtackBase ab = o.GetComponent<AtackBase>();
            ab.notHitObject = gameObject;
            weapons[(int)Weapon.SUB] = ab;
        }

        //デバッグ用
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    LockOn.TrackingSpeed *= 0.1f;
        //    PlayerCameraController.RotateSpeed *= 0.1f;
        //    MoveSpeed *= 0.1f;
        //}
        //if (Input.GetKeyUp(KeyCode.Space))
        //{
        //    LockOn.TrackingSpeed *= 10;
        //    PlayerCameraController.RotateSpeed *= 10;
        //    MoveSpeed *= 10;
        //}
        if (Input.GetKeyDown(KeyCode.P))
        {
            cacheTransform.position = initPos;
        }
    }

    //移動速度、最大速度、移動する方向
    void Move(float speed, float _maxSpeed, Vector3 direction)
    {
        if (!isQ)
        {
            //最大速度に達していなかったら移動処理
            if (_rigidbody.velocity.sqrMagnitude < Mathf.Pow(_maxSpeed, 2))
            {
                _rigidbody.AddForce(direction * speed, ForceMode.Force);


                //デバッグ用
                Debug.Log(Mathf.Pow(_maxSpeed, 2));
            }
        }
        else
        {
            _rigidbody.AddForce(direction * speed + (direction * speed - _rigidbody.velocity), ForceMode.Force);
        }


        //デバッグ用
        Debug.Log(_rigidbody.velocity.sqrMagnitude);
    }

    //攻撃
    void UseWeapon(Weapon weapon)
    {
        weapons[(int)weapon].Shot(LockOn.Target);
    }

    private void OnTriggerStay(Collider other)
    {
        //Eキーでアイテム取得
        if (Input.GetKey(KeyCode.E))
        {
            if (other.CompareTag(Item.ITEM_TAG))
            {
                //アイテム所持枠に空きがあるか調べる
                for (int num = 0; num < (int)ItemNum.NONE; num++)
                {
                    //空きがある
                    if (items[num] == null)
                    {
                        items[num] = other.GetComponent<Item>();
                        other.gameObject.SetActive(false);  //アイテムを取得したらオブジェクトを非表示
                        break;
                    }
                }


                //デバッグ用
                Debug.Log("アイテム取得");
            }
        }
    }


    //プレイヤーにダメージを与える
    public void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power, 1);  //小数点第2以下切り捨て

        //バリアが破壊されていなかったらバリアにダメージを肩代わりさせる
        if (Barrier.HP > 0)
        {
            Barrier.Damage(p);
        }
        //バリアが破壊されていたらドローンが直接ダメージを受ける
        else
        {
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
            }


            //デバッグ用
            Debug.Log("playerに" + p + "のダメージ\n残りHP: " + HP);
        }
    }
}