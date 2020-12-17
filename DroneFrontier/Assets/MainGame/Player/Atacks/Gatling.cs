using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gatling : AtackBase
{
    [SerializeField] GameObject bullet = null; //弾のオブジェクト

    //弾丸のパラメータ
    [SerializeField] float speedPerSecond = 10.0f;  //1秒間に進む量
    [SerializeField] float destroyTime = 1.0f;      //発射してから消えるまでの時間(射程)
    [SerializeField] float trackingPower = 1.2f;    //追従力

    ////撃った弾丸を全て格納する
    //List<Bullet> bullets;

    protected override void Start()
    {
        //リキャスト、1秒間に発射する数、弾数
        InitValue(0, 5.0f, 10);

        //bullets = new List<Bullet>();
    }

    protected override void Update()
    {
        ////消滅した弾丸がないか走査
        //for (int i = 0; i < bullets.Count; i++)
        //{
        //    if (bullets[i] == null)
        //    {
        //        bullets.RemoveAt(i);
        //    }
        //}

        base.Update();

        //リキャスト時間経過したら弾数を1個補充
        if (RecastCountTime >= Recast)
        {
            //残り弾数が最大弾数に達していなかったら補充
            if (BulletsRemain < BulletsNum)
            {
                BulletsRemain++;
                RecastCountTime = 0;
            }
        }
    }

    public override void Shot(Transform t, GameObject target = null)
    {
        //throw new System.NotImplementedException();

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

        GameObject o = Instantiate(bullet, t.position, t.rotation) as GameObject;    //弾丸の複製
        Bullet b = o.GetComponent<Bullet>();    //名前省略

        //弾丸のパラメータ設定
        b.OwnerName = OwnerName;
        b.Target = target;
        b.SpeedPerSecond = speedPerSecond;
        b.DestroyTime = destroyTime;
        b.TrackingPower = trackingPower;

        //bullets.Add(b);
        if (BulletsRemain == BulletsNum)
        {
            RecastCountTime = 0;
        }
        BulletsRemain--;    //残り弾数を減らす
        ShotCountTime = 0;
    }
}