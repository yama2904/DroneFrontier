using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AtackBase : MonoBehaviour
{
    public GameObject notHitObject { get; set; } = null;  //当たり判定を行わないオブジェクト
    protected float RecastCountTime { get; set; } = 0;    //リキャスト時間をカウントする変数
    protected float ShotCountTime { get; set; } = 0;      //1発ごとの間隔をカウントする変数
    protected float BulletPower { get; set; } = -1;       //弾丸の威力

    //プロパティ用
    float recast = 0;
    float shotInterval = 0;
    int bulletsNum = 0;
    int bulletsRemain = 0;

    //リキャスト時間
    protected float Recast
    {
        get
        {
            return recast;
        }
        set
        {
            if (value >= 0) recast = value;
        }
    }

    //1発ごとの間隔
    protected float ShotInterval
    {
        get
        {
            return shotInterval;
        }
        set
        {
            if (value >= 0) shotInterval = value;
        }
    }

    //弾数
    protected int BulletsNum
    {
        get
        {
            return bulletsNum;
        }
        set
        {
            if (value >= 0) bulletsNum = value;

        }
    }

    //残り弾数
    protected int BulletsRemain
    {
        get
        {
            return bulletsRemain;
        }
        set
        {
            if (value >= 0) bulletsRemain = value;
        }
    }

    protected abstract void Start();

    //リキャスト時間と発射間隔を管理する
    protected virtual void Update()
    {
        RecastCountTime += Time.deltaTime;
        if (RecastCountTime > recast)
        {
            RecastCountTime = recast;
        }

        ShotCountTime += Time.deltaTime;
        if (ShotCountTime > shotInterval)
        {
            ShotCountTime = shotInterval;
        }
    }

    public abstract void Shot(GameObject target = null);
}
