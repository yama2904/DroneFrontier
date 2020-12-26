using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

/*
 * 公開変数
 * float HP                  ローンのHP
 * float MoveSpeed           移動速度
 * float MaxSpeed            最高速度
 * Barrier Barrier           プレイヤーのバリア
 * 
 * 公開メソッド
 * void Damage(float power)  プレイヤーにダメージを与える
 */
public class Player : BasePlayer
{
    public const string PLAYER_TAG = "Player";  //タグ名    
    [SerializeField] Barrier barrier = null;    //バリア
    Transform cacheTransform = null;            //キャッシュ用

    bool[] isUsingWeapons;    //使用中の武器

    //ブースト用変数
    const float BOOST_POSSIBLE_MIN = 0.2f;       //ブースト可能な最低ゲージ量
    Image boostImage;
    [SerializeField] float boostAccele = 2.0f;  //ブーストの加速度
    [SerializeField] float maxBoostTime = 5.0f; //ブーストできる最大の時間
    [SerializeField] float boostRecastTime = 6.0f;  //ブーストのリキャスト時間
    bool isBoost;

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

    protected override void Start()
    {
        cacheTransform = transform;
        _rigidbody = GetComponent<Rigidbody>();
        Barrier = barrier;

        HP = 10;
        MoveSpeed = 20.0f;
        MaxSpeed = 30.0f;

        //武器の初期化
        weapons = new AtackBase[(int)Weapon.NONE];
        isUsingWeapons = new bool[(int)Weapon.NONE];
        for (int i = 0; i < (int)Weapon.NONE; i++)
        {
            isUsingWeapons[i] = false;
        }

        //メインウェポンの処理
        AtackManager.CreateAtack(out GameObject main, AtackManager.Weapon.GATLING);    //Gatlingの生成
        Transform mainTransform = main.transform;   //キャッシュ
        mainTransform.parent = cacheTransform;      //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        mainTransform.localPosition = new Vector3(0, 0, 0);
        mainTransform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase abM = main.GetComponent<AtackBase>(); //名前省略
        abM.notHitObject = gameObject;    //自分をヒットさせない
        weapons[(int)Weapon.MAIN] = abM;


        //サブウェポンの処理
        AtackManager.CreateAtack(out GameObject sub, AtackManager.Weapon.SHOTGUN);    //Shotgunの作成
        Transform subTransform = sub.transform; //キャッシュ
        subTransform.parent = cacheTransform;   //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        subTransform.localPosition = new Vector3(0, 0, 0);
        subTransform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase abS = sub.GetComponent<AtackBase>();
        abS.notHitObject = gameObject;    //自分をヒットさせない
        weapons[(int)Weapon.SUB] = abS;

        items = new Item[(int)ItemNum.NONE];
        boostImage = GameObject.Find("BoostGauge").GetComponent<Image>();
        boostImage.fillAmount = 1;
        isBoost = false;


        //デバッグ用
        initPos = cacheTransform.position;
    }

    protected override void Update()
    {
        //デバッグ用
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                isQ = !isQ;
                Debug.Log("移動処理切り替え");
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
        if (Input.GetKeyDown(KeyCode.Q))
        {
            //ブーストが使用可能なゲージ量ならブースト使用
            if (boostImage.fillAmount >= BOOST_POSSIBLE_MIN)
            {
                //バトルモードの場合
                if (MainGameManager.Mode == MainGameManager.GameMode.BATTLE)
                {

                }

                //レースモードの場合
                else if (MainGameManager.Mode == MainGameManager.GameMode.RACE)
                {

                }

                ModifySpeed(boostAccele);
                isBoost = true;


                //デバッグ用
                Debug.Log("ブースト使用");
            }
        }
        //ブースト使用中の処理
        if (isBoost)
        {
            //キーを押し続けている間はゲージ消費
            if (Input.GetKey(KeyCode.Q))
            {
                boostImage.fillAmount -= 1.0f / maxBoostTime * Time.deltaTime;

                //ゲージが空になったらブースト停止
                if (boostImage.fillAmount <= 0)
                {
                    boostImage.fillAmount = 0;

                    ModifySpeed(1 / boostAccele);
                    isBoost = false;


                    //デバッグ用
                    Debug.Log("ブースト終了");
                }
            }
            //キーを離したらブースト停止
            if (Input.GetKeyUp(KeyCode.Q))
            {
                ModifySpeed(1 / boostAccele);
                isBoost = false;


                //デバッグ用
                Debug.Log("ブースト終了");
            }
        }

        //ブースト未使用時にゲージ回復
        if (!isBoost)
        {
            if (boostImage.fillAmount < 1.0f)
            {
                boostImage.fillAmount += 1.0f / boostRecastTime * Time.deltaTime;
                if (boostImage.fillAmount >= 1.0f)
                {
                    boostImage.fillAmount = 1;
                }
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
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            LockOn.TrackingSpeed *= 0.1f;
            PlayerCameraController.RotateSpeed *= 0.1f;
            MoveSpeed *= 0.1f;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            LockOn.TrackingSpeed *= 10;
            PlayerCameraController.RotateSpeed *= 10;
            MoveSpeed *= 10;
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            cacheTransform.position = initPos;
        }
    }

    //移動速度、最大速度、移動する方向
    protected override void Move(float speed, float _maxSpeed, Vector3 direction)
    {
        if (!isQ)
        {
            //最大速度に達していなかったら移動処理
            if (_rigidbody.velocity.sqrMagnitude < Mathf.Pow(_maxSpeed, 2))
            {
                _rigidbody.AddForce(direction * speed, ForceMode.Force);


                //デバッグ用
                //Debug.Log(Mathf.Pow(_maxSpeed, 2));
            }
        }
        else
        {
            _rigidbody.AddForce(direction * speed + (direction * speed - _rigidbody.velocity), ForceMode.Force);
        }


        //デバッグ用
        //Debug.Log(_rigidbody.velocity.sqrMagnitude);
    }

    //攻撃
    protected override void UseWeapon(Weapon weapon)
    {
        weapons[(int)weapon].Shot(LockOn.Target);
    }

    //スピードを変更する
    void ModifySpeed(float speedMgnf)
    {
        MoveSpeed *= speedMgnf;
        MaxSpeed *= speedMgnf;
    }

    //ブースト使用
    protected override IEnumerator UseBoost(float speedMgnf, float time)
    {
        ModifySpeed(speedMgnf);
        isBoost = true;


        //デバッグ用
        Debug.Log("ブースト使用");

        if (time >= 0)
        {
            //time秒後に速度を戻す
            yield return new WaitForSeconds(time);
            ModifySpeed(1 / speedMgnf);
            isBoost = false;


            //デバッグ用
            Debug.Log("ブースト終了");
        }
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
    public override void Damage(float power)
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