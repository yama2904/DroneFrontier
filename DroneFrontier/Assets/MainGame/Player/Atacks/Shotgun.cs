using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : AtackBase
{
    //ショットガンのパラメータ
    [SerializeField] GameObject bullet = null;      //弾のオブジェクト
    [SerializeField] float diffusionPower = 10.0f;  //拡散力
    [SerializeField] float angleDiff = 3.0f;        //角度の変動量

    //弾丸のパラメータ
    [SerializeField] float speedPerSecond = 10.0f;  //1秒間に進む量
    [SerializeField] float destroyTime = 0.3f;      //発射してから消えるまでの時間(射程)
    [SerializeField] float trackingPower = 0;    //追従力

    List<Bullet> bullets;
    float shotInterval; //1発ごとの間隔

    protected override void Start()
    {
        recast = 2.0f;
        shotPerSecond = 2.0f;
        deltaTime = 0;

        bullets = new List<Bullet>();
        shotInterval = 1 / shotPerSecond;

        //乱数のシード値の設定
        Random.InitState(System.DateTime.Now.Millisecond);
    }

    protected override void Update()
    {
        //消滅した弾丸がないか走査
        for (int i = 0; i < bullets.Count; i++)
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
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    GameObject o = Instantiate(bullet, t.position, t.rotation) as GameObject;    //弾丸の複製
                    Bullet b = o.GetComponent<Bullet>();    //名前省略

                    //弾丸のパラメータ設定
                    b.OwnerName = OwnerName;
                    b.Target = target;
                    b.SpeedPerSecond = speedPerSecond;
                    b.DestroyTime = destroyTime;
                    b.TrackingPower = trackingPower;


                    //弾丸の進む方向を変えて散らす処理
                    float rotateX = (diffusionPower * i) + Random.Range(angleDiff * -1, angleDiff);
                    float rotateY = (diffusionPower * j) + Random.Range(angleDiff * -1, angleDiff);
                    o.transform.RotateAround(o.transform.position, o.transform.right, rotateY);
                    o.transform.RotateAround(o.transform.position, o.transform.up, rotateX);

                    bullets.Add(b);
                }
            }
            deltaTime = 0;
        }
    }
}
