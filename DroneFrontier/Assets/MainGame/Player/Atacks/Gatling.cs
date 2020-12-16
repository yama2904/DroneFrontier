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

    List<Bullet> bullets;
    float shotInterval; //1発ごとの間隔
    float deltaTime;

    protected override void Start()
    {
        bullets = new List<Bullet>();
        shotPerSecond = 5.0f;
        shotInterval = 1 / shotPerSecond;
        deltaTime = 0;
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
        
        deltaTime += Time.deltaTime;
    }

    public override void Shot(Transform t, GameObject target = null)
    {
        //throw new System.NotImplementedException();
        
        if (deltaTime > shotInterval)
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
            deltaTime = 0;
        }
    }
}
