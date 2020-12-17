using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 公開変数
 * static string ObjectName  ドローンのオブジェクト名(Findとか用)
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
    public const string PLAYER_TAG = "Player";       //タグ名
    public float HP { get; private set; } = 10;      //HP
    public float MoveSpeed { get; set; } = 20.0f;    //移動速度
    public float MaxSpeed { get; set; } = 30.0f;     //最高速度

    Rigidbody _rigidbody = null;


    //武器
    enum Weapon
    {
        MAIN,   //メイン武器
        SUB,    //サブ武器

        NONE
    }
    AtackBase[] weapons;  //ウェポン群

    //バリア
    [SerializeField] GameObject barrierObject = null;
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

    //オブジェクトの名前
    public static string ObjectName { get; private set; } = "";

    //デバッグ用
    int atackType = (int)AtackManager.Weapon.SHOTGUN;
    bool isQ = false;

    void Start()
    {
        ObjectName = name;
        _rigidbody = GetComponent<Rigidbody>();
        weapons = new AtackBase[(int)Weapon.NONE];
        Barrier = barrierObject.GetComponent<Barrier>();


        //メインウェポンの処理
        AtackManager.CreateAtack(out GameObject main, AtackManager.Weapon.GATLING);    //Gatlingの生成
        main.transform.parent = transform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        main.transform.localPosition = new Vector3(0, 0, 0);
        main.transform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase abM = main.GetComponent<AtackBase>(); //名前省略
        abM.OwnerName = name;    //所持者の名前を設定
        weapons[(int)Weapon.MAIN] = abM;


        //サブウェポンの処理
        AtackManager.CreateAtack(out GameObject sub, AtackManager.Weapon.SHOTGUN);    //Shotgunの作成
        sub.transform.parent = transform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        sub.transform.localPosition = new Vector3(0, 0, 0);
        sub.transform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase abS = sub.GetComponent<AtackBase>();
        abS.OwnerName = name;    //所持者の名前を設定
        weapons[(int)Weapon.SUB] = abS;

        items = new Item[(int)ItemNum.NONE];
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
        Move(MoveSpeed, MaxSpeed);

        //攻撃処理
        UseWeapon(Weapon.MAIN);     //メインウェポン攻撃
        UseWeapon(Weapon.SUB);      //サブウェポン攻撃

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
        if (Input.GetKeyUp(KeyCode.Space))
        {

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
            o.transform.parent = transform;

            //位置と角度の初期設定
            o.transform.localPosition = new Vector3(0, 0, 0);
            o.transform.localRotation = Quaternion.Euler(0, 0, 0);

            AtackBase ab = o.GetComponent<AtackBase>();
            ab.OwnerName = name;
            weapons[(int)Weapon.SUB] = ab;
        }
    }


    void Move(float speed, float _maxSpeed)
    {
        float velocityDistance = 0;   //今移動している向きに移動した場合の距離
        float maxDistance = 0;        //最大速度で移動時の距離
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
                //最大速度に達していなかったら移動処理
                if (velocityDistance < maxDistance)
                {
                    _rigidbody.AddForce(transform.forward * MoveSpeed, ForceMode.Force);
                }
            }
            else
            {
                _rigidbody.AddForce(transform.forward * MoveSpeed + (transform.forward * MoveSpeed - _rigidbody.velocity), ForceMode.Force);
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
                //最大速度に達していなかったら移動処理
                if (velocityDistance < maxDistance)
                {
                    _rigidbody.AddForce(left * MoveSpeed, ForceMode.Force);
                }
            }
            else
            {
                _rigidbody.AddForce(left * MoveSpeed + (left * MoveSpeed - _rigidbody.velocity), ForceMode.Force);
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
                //最大速度に達していなかったら移動処理
                if (velocityDistance < maxDistance)
                {
                    _rigidbody.AddForce(backward * MoveSpeed, ForceMode.Force);
                }
            }
            else
            {
                _rigidbody.AddForce(backward * MoveSpeed + (backward * MoveSpeed - _rigidbody.velocity), ForceMode.Force);
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
                //最大速度に達していなかったら移動処理
                if (velocityDistance < maxDistance)
                {
                    _rigidbody.AddForce(right * MoveSpeed, ForceMode.Force);
                }
            }
            else
            {
                //Vector3 diff = right * moveSpeed - _rigidbody.velocity;
                _rigidbody.AddForce(right * MoveSpeed + (right * MoveSpeed - _rigidbody.velocity), ForceMode.Force);
            }
        }

        //上下の移動
        //float scroll = Input.GetAxis("Mouse ScrollWheel");

        //デバッグ用
        float s = MoveSpeed * 2;
        float ms = _maxSpeed * 2;    //maxspeed
        //

        Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
        Vector3 upward = upAngle.normalized * Vector3.forward;
        velocityDistance = Vector3.Distance(transform.position, transform.position + _rigidbody.velocity);
        maxDistance = Vector3.Distance(transform.position, transform.position + (upward * ms));

        if (!isQ)
        {
            //最大速度に達していなかったら移動処理
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
            //左クリックでメインウェポン攻撃
            if (Input.GetMouseButton(0))
            {
                weapons[(int)Weapon.MAIN].Shot(transform, LockOn.Target);
            }

            //攻撃中は移動速度と回転速度低下
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
                    MoveSpeed *= 0.5f;
                }
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
                    MoveSpeed *= 2;
                }
            }
        }

        //サブウェポン攻撃
        else if (weapon == Weapon.SUB)
        {
            //右クリックでサブウェポン攻撃
            if (Input.GetMouseButton(1))
            {
                weapons[(int)Weapon.SUB].Shot(transform, LockOn.Target);
            }

            //攻撃中は移動速度と回転速度低下
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
                    MoveSpeed *= 0.5f;
                }
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
                    MoveSpeed *= 2;
                }
            }
        }
    }

    //自分の周囲にTriggerを張って範囲内にアイテムがあったら探知
    private void OnTriggerStay(Collider other)
    {
        //Eキーでアイテム取得
        if (Input.GetKey(KeyCode.E))
        {
            if (other.tag == Item.ITEM_TAG)
            {
                //アイテム所持枠に空きがあるか調べる
                int num = 0;
                for (; num < (int)ItemNum.NONE; num++)
                {
                    if (items[num] == null)
                    {
                        break;
                    }
                }
                //空きがなかったら取得しない
                if (num >= (int)ItemNum.NONE)
                {
                    return;
                }

                items[num] = other.GetComponent<Item>();
                other.gameObject.SetActive(false);  //アイテムを取得したらオブジェクトを非表示


                //デバッグ用
                Debug.Log("アイテム取得");
            }
        }
    }

    //プレイヤーにダメージを与える
    public void Damage(float power)
    {
        //バリアが破壊されていなかったらバリアにダメージを肩代わりさせる
        if (Barrier.HP > 0)
        {
            Barrier.Damage(power);
        }
        //バリアが破壊されていたらドローンが直接ダメージを受ける
        else
        {
            HP -= power;
            if (HP < 0)
            {
                HP = 0;
            }
            Debug.Log("playerに" + power + "のダメージ\n残りHP: " + HP);
        }
    }
}