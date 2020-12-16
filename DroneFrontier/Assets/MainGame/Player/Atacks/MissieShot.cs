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
    [SerializeField] float _recast = 3.0f;          //リキャスト時間
    [SerializeField] float shotPerSecond = 1.0f;    //1秒間に発射する数
    [SerializeField] int bulletsNum = 3;            //弾数
    int bulletsRemain;    //残り弾数

    List<MissileBullet> missiles;

    protected override void Start()
    {
        InitValue(_recast, shotPerSecond);

        missiles = new List<MissileBullet>();
        bulletsRemain = bulletsNum;
    }

    protected override void Update()
    {
        //消滅した弾丸がないか走査
        for (int i = 0; i < missiles.Count; i++)
        {
            if (missiles[i] == null)
            {
                missiles.RemoveAt(i);
            }
        }

        base.Update();
    }

    public override void Shot(Transform t, GameObject target = null)
    {
        if (shotCount >= shotInterval)
        {
            GameObject o = Instantiate(missile, t.position, t.rotation) as GameObject;    //ミサイルの複製
            MissileBullet m = o.GetComponent<MissileBullet>();  //名前省略

            //弾丸のパラメータ設定
            m.OwnerName = OwnerName;
            m.Target = target;
            m.SpeedPerSecond = speedPerSecond;
            m.DestroyTime = destroyTime;
            m.TrackingPower = trackingPower;

            missiles.Add(o.GetComponent<MissileBullet>());
            shotCount = 0;
        }
    }
}
