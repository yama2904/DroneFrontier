using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Player : BasePlayer
{
    public const string PLAYER_TAG = "Player";  //タグ名
    bool[] isUsingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器

    [SerializeField] Radar radar = null;


    //ブースト用変数
    Image boostImage = null;


    //デバッグ用
    int atackType = (int)BaseWeapon.Weapon.SHOTGUN;
    Vector3 initPos;


    protected override void Start()
    {
        HP = 30;
        MoveSpeed = 20.0f;
        MaxSpeed = 30.0f;

        //配列初期化
        for (int i = 0; i < (int)Weapon.NONE; i++)
        {
            isUsingWeapons[i] = false;
        }

        boostImage = GameObject.Find("BoostGauge").GetComponent<Image>();
        boostImage.fillAmount = 1;


        //デバッグ用
        initPos = cacheTransform.position;
    }

    protected override void Update()
    {
        base.Update();

        if (MainGameManager.IsCursorLock)
        {
            //回転処理
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


        //ロックオン
        if (Input.GetKey(KeyCode.LeftShift))
        {
            ILockOn l = _LockOn;
            l.StartLockOn(LockOnTrackingSpeed);
        }
        //ロックオン解除
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            ILockOn l = _LockOn;
            l.ReleaseLockOn();
        }

        //レーダー使用
        if (Input.GetKey(KeyCode.Space))
        {
            IRadar r = radar;
            r.StartRadar();
        }
        //レーダー使用
        if (Input.GetKeyUp(KeyCode.Space))
        {
            IRadar r = radar;
            r.ReleaseRadar();
        }


        //
        //設定画面中はここより下の処理は行わない
        if (MainGameManager.IsConfig)
        {
            return;
        }
        //
        //


        //攻撃処理しか使わない簡易メソッド
        Action<float> ModifySpeeds = (x) =>
        {
            MoveSpeed *= x;
            RotateSpeed *= x;
            LockOnTrackingSpeed *= x;
        };

        //メイン武器攻撃
        if (Input.GetMouseButtonDown(0))
        {
            //サブ武器を使用していない場合は移動速度と回転速度とロックオンの追従速度を下げる
            if (!isUsingWeapons[(int)Weapon.SUB])
            {
                ModifySpeeds(AtackingDecreaseSpeed);
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
                ModifySpeeds(1 / AtackingDecreaseSpeed);
            }
            isUsingWeapons[(int)Weapon.MAIN] = false;
        }

        //サブ武器攻撃
        if (Input.GetMouseButtonDown(1))
        {
            //メイン武器を使用していない場合は移動速度と回転速度とロックオンの追従速度を下げる
            if (!isUsingWeapons[(int)Weapon.MAIN])
            {
                ModifySpeeds(AtackingDecreaseSpeed);
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
                ModifySpeeds(1 / AtackingDecreaseSpeed);
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
            UseItem(ItemNum.ITEM_1);
        }
        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            UseItem(ItemNum.ITEM_2);
        }


        //デバッグ用
        //武器切り替え
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //今持っているサブ武器を削除
            Destroy(weapons[(int)Weapon.SUB].gameObject);

            //次の武器に切り替える
            if (++atackType >= (int)BaseWeapon.Weapon.NONE)
            {
                atackType = 0;
            }

            GameObject o = BaseWeapon.CreateWeapon(gameObject, (BaseWeapon.Weapon)atackType);     //武器の作成
            Transform t = o.transform;  //キャッシュ用
            t.SetParent(transform);     //作成した武器を子オブジェクトにする

            //位置と角度の初期設定
            t.localPosition = new Vector3(0, 0, 0);
            t.localRotation = Quaternion.Euler(0, 0, 0);

            weapons[(int)Weapon.SUB] = o.GetComponent<BaseWeapon>();
        }

        //デバッグ用
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            LockOnTrackingSpeed *= 0.1f;
            RotateSpeed *= 0.1f;
            MoveSpeed *= 0.1f;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            LockOnTrackingSpeed *= 10;
            RotateSpeed *= 10;
            MoveSpeed *= 10;
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            cacheTransform.position = initPos;
        }
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
}