using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissieShot : AtackBase
{
    [SerializeField] GameObject missile = null;

    //弾丸のパラメータ
    [SerializeField] float speedPerSecond = 13.0f;  //1秒間に進む量
    [SerializeField] float destroyTime = 2.0f;      //発射してから消えるまでの時間(射程)
    [SerializeField] float trackingPower = 2.3f;    //追従力

    ////撃った弾丸を全て格納する
    //List<MissileBullet> missiles;

    protected override void Start()
    {
        //リキャスト、1秒間に発射する数、弾数
        InitValue(3.0f, 1.0f, 3);

        //missiles = new List<MissileBullet>();
    }

    protected override void Update()
    {
        ////消滅した弾丸がないか走査
        //for (int i = 0; i < missiles.Count; i++)
        //{
        //    if (missiles[i] == null)
        //    {
        //        missiles.RemoveAt(i);
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


                //デバッグ用
                Debug.Log("ミサイルの弾丸が1回分補充されました");
            }
        }
    }

    public override void Shot(Transform t, GameObject target = null)
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

        GameObject o = Instantiate(missile, t.position, t.rotation) as GameObject;    //ミサイルの複製
        MissileBullet m = o.GetComponent<MissileBullet>();  //名前省略

        //弾丸のパラメータ設定
        m.OwnerName = OwnerName;
        m.Target = target;
        m.SpeedPerSecond = speedPerSecond;
        m.DestroyTime = destroyTime;
        m.TrackingPower = trackingPower;

        //missiles.Add(o.GetComponent<MissileBullet>());
        if (BulletsRemain == BulletsNum)
        {
            RecastCountTime = 0;
        }
        BulletsRemain--;    //残り弾数を減らす
        ShotCountTime = 0;


        //デバッグ用
        Debug.Log("残り弾数: " + BulletsRemain);
    }
}