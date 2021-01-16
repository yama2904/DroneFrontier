using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Gatling : BaseWeapon
{
    [SerializeField] Bullet bullet = null; //弾のオブジェクト

    //弾丸のパラメータ
    [SerializeField] float speedPerSecond = 10.0f;  //1秒間に進む量
    [SerializeField] float destroyTime = 1.0f;      //発射してから消えるまでの時間(射程)
    [SerializeField] float trackingPower = 1.2f;    //追従力
    [SerializeField] float shotPerSecond = 5.0f;    //1秒間に発射する弾数


    protected override void Start()
    {
        Recast = 0;
        ShotInterval = 1.0f / shotPerSecond;
        ShotCountTime = ShotInterval;
        BulletsNum = 10;
        BulletsRemain = BulletsNum;
        BulletPower = 3.0f;

        //GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
    }

    protected override void Update()
    {
        //リキャストと発射間隔のカウント
        base.Update();

        //リキャスト時間経過したら弾数を1個補充
        if (RecastCountTime >= Recast)
        {
            //残り弾数が最大弾数に達していなかったら補充
            if (BulletsRemain < BulletsNum)
            {
                BulletsRemain++;        //弾数を回復
                RecastCountTime = 0;    //リキャストのカウントをリセット
            }
        }
    }

    public override void Init(uint netId)
    {
        //parentNetId = netId;

        //NetworkTransform nt = GetComponent<NetworkTransform>();
        //nt.transform.localPosition = weaponLocalPos.localPosition;
        //nt.transform.localRotation = weaponLocalPos.localRotation;
    }

    public override void UpdateMe()
    {
    }

    public override void Shot(GameObject target = null)
    {
        //前回発射して発射間隔分の時間が経過していなかったら撃たない
        if (ShotCountTime < ShotInterval)
        {
            return;
        }

        //残り弾数が0だったら撃たない
        if (BulletsRemain <= 0)
        {
            return;
        }

        CmdCreateBullet(shotPos.position, transform.rotation, target);


        //残り弾丸がMAXで撃つと一瞬で弾丸が1個回復するので
        //残り弾丸がMAXで撃った場合のみリキャストを0にする
        if (BulletsRemain == BulletsNum)
        {
            RecastCountTime = 0;
        }
        BulletsRemain--;    //残り弾数を減らす
        ShotCountTime = 0;  //発射間隔のカウントをリセット
    }

    Bullet CreateBullet(Vector3 pos, Quaternion rotation, GameObject target)
    {
        Bullet b = Instantiate(bullet, pos, rotation);    //弾丸の複製

        //弾丸のパラメータ設定
        b.Shooter = Shooter;    //撃ったプレイヤーを登録
        b.Target = target;      //ロックオン中の敵
        b.SpeedPerSecond = speedPerSecond;  //スピード
        b.DestroyTime = destroyTime;        //射程
        b.TrackingPower = trackingPower;    //誘導力
        b.Power = BulletPower;              //威力

        return b;
    }

    [Command]
    void CmdCreateBullet(Vector3 pos, Quaternion rotation, GameObject target)
    {
        Bullet b = CreateBullet(pos, rotation, target);
        NetworkServer.Spawn(b.gameObject, connectionToClient);
    }
}