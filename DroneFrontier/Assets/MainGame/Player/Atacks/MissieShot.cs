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

    List<MissileBullet> missiles;
    float shotInterval; //1発ごとの間隔
    float deltaTime;

    protected override void Start()
    {
        missiles = new List<MissileBullet>();
        shotPerSecond = 1.0f;
        shotInterval = 1 / shotPerSecond;
        deltaTime = 0;
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

        deltaTime += Time.deltaTime;
    }

    public override void Shot(Transform t, GameObject target = null)
    {
        if (deltaTime > shotInterval)
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
            deltaTime = 0;
        }
    }
}
