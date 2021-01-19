using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MissieWeapon : BaseWeapon
{
    [SerializeField] MissileBullet missile = null;  //複製する弾丸
    SyncList<GameObject> settingBullets = new SyncList<GameObject>();
    [SyncVar] int useMissile = -1;

    //弾丸のパラメータ
    [SerializeField] float speedPerSecond = 13.0f;  //1秒間に進む量
    [SerializeField] float destroyTime = 2.0f;      //発射してから消えるまでの時間(射程)
    [SerializeField] float trackingPower = 2.3f;    //追従力
    [SerializeField] float shotPerSecond = 1.0f;    //1秒間に発射する弾数


    public override void OnStartClient()
    {
        base.OnStartClient();
        Recast = 10.0f;
        ShotInterval = 1.0f / shotPerSecond;
        ShotCountTime = ShotInterval;
        BulletsNum = 3;
        BulletsRemain = BulletsNum;
        BulletPower = 20.0f;
    }

    protected override void Start() { }
    protected override void Update() { }

    public override void Init()
    {
        BulletPower = 20.0f;
        CmdCreateMissile();
    }

    public override void UpdateMe()
    {
        //発射間隔のカウント
        if (useMissile <= -1)
        {
            ShotCountTime += Time.deltaTime;
            if (ShotCountTime > ShotInterval)
            {
                ShotCountTime = ShotInterval;
                if (BulletsRemain > 0)  //弾丸が残っていない場合は処理しない
                {
                    CmdCreateMissile();


                    //デバッグ用
                    Debug.Log("ミサイルの弾丸が1回分補充されました");
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
            }
        }
    }

    #region CreateMissile

    MissileBullet CreateMissile()
    {
        MissileBullet m = Instantiate(missile);    //ミサイルの複製
        m.parentNetId = netId;

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
        if (useMissile >= 0)
        {
            return;
        }
        MissileBullet m = CreateMissile();
        NetworkServer.Spawn(m.gameObject, connectionToClient);

        settingBullets.Add(m.gameObject);
        useMissile = settingBullets.Count - 1;
    }

    #endregion

    public override void Shot(GameObject target = null)
    {
        //前回発射して発射間隔分の時間が経過していなかったら撃たない
        if (ShotCountTime < ShotInterval) return;

        //バグ防止
        if (useMissile == -1) return;

        //残り弾数が0だったら撃たない
        if (BulletsRemain <= 0) return;


        //ミサイル発射
        CmdShot(target);


        if (BulletsRemain == BulletsNum)
        {
            RecastCountTime = 0;
        }
        BulletsRemain--;    //残り弾数を減らす
        ShotCountTime = 0;  //発射間隔のカウントをリセット


        //デバッグ用
        Debug.Log("ミサイル発射 残り弾数: " + BulletsRemain);
    }

    [Command(ignoreAuthority = true)]
    void CmdShot(GameObject target)
    {
        MissileBullet m = settingBullets[useMissile].GetComponent<MissileBullet>();
        m.CmdShot(target);

        settingBullets.RemoveAt(useMissile);
        useMissile = -1;
    }
}