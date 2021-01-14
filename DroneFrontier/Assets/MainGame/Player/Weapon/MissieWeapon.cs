using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MissieWeapon : BaseWeapon
{
    [SerializeField] MissileBullet missile = null;  //複製する弾丸
    [SyncVar] GameObject settingFirstMissile = null;
    [SyncVar] GameObject settingSecondMissile = null;
    bool useFirstMissile = true;

    //弾丸のパラメータ
    [SerializeField] float speedPerSecond = 13.0f;  //1秒間に進む量
    [SerializeField] float destroyTime = 2.0f;      //発射してから消えるまでの時間(射程)
    [SerializeField] float trackingPower = 2.3f;    //追従力
    [SerializeField] float shotPerSecond = 1.0f;    //1秒間に発射する弾数


    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsLocalPlayer) return;
        CmdCreateMissile();
    }

    protected override void Start()
    {
        Recast = 10.0f;
        ShotInterval = 1.0f / shotPerSecond;
        ShotCountTime = ShotInterval;
        BulletsNum = 3;
        BulletsRemain = BulletsNum;
        BulletPower = 20.0f;
    }

    protected override void Update()
    {
        if (!IsLocalPlayer) return;

        //発射間隔のカウント
        GameObject useMissile = null;
        if (useFirstMissile)
        {
            useMissile = settingFirstMissile;
        }
        else
        {
            useMissile = settingSecondMissile;
        }

        if (useMissile == null)
        {
            ShotCountTime += Time.deltaTime;
            if (ShotCountTime > ShotInterval)
            {
                ShotCountTime = ShotInterval;
                if (BulletsRemain > 0)  //弾丸が残っていない場合は処理しない
                {
                    if (MainGameManager.IsMulti)
                    {
                        CmdCreateMissile();
                    }
                }
            }
        }

        //リキャスト時間経過したら弾数を1個補充
        if (BulletsRemain < BulletsNum)     //最大弾数持っていたら処理しない
        {
            RecastCountTime += Time.deltaTime;
            if (RecastCountTime >= Recast)
            {
                BulletsRemain++;        //弾数を回復
                RecastCountTime = 0;    //リキャストのカウントをリセット


                //デバッグ用
                Debug.Log("ミサイルの弾丸が1回分補充されました");
            }
        }
    }

    public override void Init(bool isLocalPlayer)
    {
        IsLocalPlayer = isLocalPlayer;
    }

    MissileBullet CreateMissile()
    {
        MissileBullet m = Instantiate(missile);    //ミサイルの複製
        m.parentTransform = transform;

        //弾丸のパラメータ設定
        m.Shooter = Shooter;    //撃ったプレイヤーを登録
        m.SpeedPerSecond = speedPerSecond;  //スピード
        m.DestroyTime = destroyTime;        //射程
        m.TrackingPower = trackingPower;    //誘導力
        m.Power = BulletPower;              //威力

        return m;
    }

    [Command(ignoreAuthority = true)]
    void CmdCreateMissile()
    {
        MissileBullet m = CreateMissile();
        NetworkServer.Spawn(m.gameObject, connectionToClient);
        if (useFirstMissile)
        {
            settingFirstMissile = m.gameObject;
        }
        else
        {
            settingSecondMissile = m.gameObject;
        }
    }


    public override void Shot(GameObject target = null)
    {
        //前回発射して発射間隔分の時間が経過していなかったら撃たない
        if (ShotCountTime < ShotInterval) return;

        GameObject useMissile = null;
        if (useFirstMissile)
        {
            useMissile = settingFirstMissile;
        }
        else
        {
            useMissile = settingSecondMissile;
        }

        //バグ防止
        if (useMissile == null) return;

        //残り弾数が0だったら撃たない
        if (BulletsRemain <= 0) return;


        //ミサイルスクリプトを有効にして親子関係を外す
        if (MainGameManager.IsMulti)
        {
            CmdShot(useMissile, target);
        }
        else
        {
            useMissile.transform.parent = null;
            useMissile.GetComponent<MissileBullet>().Shot();
            useMissile.GetComponent<MissileBullet>().Target = target;
        }
        useFirstMissile = !useFirstMissile;


        if (BulletsRemain == BulletsNum)
        {
            RecastCountTime = 0;
        }
        BulletsRemain--;    //残り弾数を減らす
        ShotCountTime = 0;  //発射間隔のカウントをリセット


        //デバッグ用
        Debug.Log("ミサイル発射 残り弾数: " + BulletsRemain);
    }

    [Command]
    void CmdShot(GameObject useMissile, GameObject target)
    {
        MissileBullet m = useMissile.GetComponent<MissileBullet>();
        m.myTransform.parent = null;
        m.Shot();
        m.Target = target;
    }
}