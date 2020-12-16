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
    [SerializeField] float _recast = 0;             //リキャスト時間
    [SerializeField] float shotPerSecond = 5.0f;    //1秒間に発射する数
    [SerializeField] int bulletsNum = 10;            //弾数
    int bulletsRemain;    //残り弾数

    List<Bullet> bullets;

    protected override void Start()
    {
        InitValue(_recast, shotPerSecond);

        bullets = new List<Bullet>();
        bulletsRemain = bulletsNum;
    }

    protected override void Update()
    {
        //消滅した弾丸がないか走査
        for(int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i] == null)
            {
                bullets.RemoveAt(i);
            }
        }

        base.Update();
    }

    public override void Shot(Transform t, GameObject target = null)
    {
        //throw new System.NotImplementedException();
        
        if (shotCount >= shotInterval)
        {
            GameObject o = Instantiate(bullet, t.position, t.rotation) as GameObject;    //弾丸の複製
            Bullet b = o.GetComponent<Bullet>();    //名前省略

            //弾丸のパラメータ設定
            b.OwnerName = OwnerName;
            b.Target = target;
            b.SpeedPerSecond = speedPerSecond;
            b.DestroyTime = destroyTime;
            b.TrackingPower = trackingPower;

            bullets.Add(b); 
            shotCount = 0;
        }
    }
}
