using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : AtackBase
{
    //ショットガンのパラメータ
    [SerializeField] Bullet bullet = null;      //弾のオブジェクト
    [SerializeField] float diffusionPower = 10.0f;  //拡散力
    [SerializeField] float angleDiff = 3.0f;        //角度の変動量

    //弾丸のパラメータ
    [SerializeField] float speedPerSecond = 10.0f;  //1秒間に進む量
    [SerializeField] float destroyTime = 0.3f;      //発射してから消えるまでの時間(射程)
    [SerializeField] float trackingPower = 0;       //追従力
    [SerializeField] float shotPerSecond = 2.0f;    //1秒間に発射する弾数


    protected override void Start()
    {
        Recast = 2.0f;
        ShotInterval = 1.0f / shotPerSecond;
        ShotCountTime = ShotInterval;
        BulletsNum = 5;
        BulletsRemain = BulletsNum;
        BulletPower = 8.0f;

        //乱数のシード値の設定
        Random.InitState(System.DateTime.Now.Millisecond);
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


                //デバッグ用
                Debug.Log("ショットガンの弾丸が1回分補充されました");
            }
        }
    }

    public override void Shot(GameObject target = null)
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

        //弾を散らす
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Bullet b = Instantiate(bullet, transform.position, transform.rotation);    //弾丸の複製
                Transform t = b.transform;  //キャッシュ

                //弾丸のパラメータ設定
                b.Shooter = Shooter;    //撃ったプレイヤーを登録
                b.Target = target;          //ロックオン中の敵
                b.SpeedPerSecond = speedPerSecond;   //スピード
                b.DestroyTime = destroyTime;         //射程
                b.TrackingPower = trackingPower;     //誘導力
                b.Power = BulletPower;               //威力


                //弾丸の進む方向を変えて散らす処理
                float rotateX = (diffusionPower * i) + Random.Range(angleDiff * -1, angleDiff); 　//左右の角度
                float rotateY = (diffusionPower * j) + Random.Range(angleDiff * -1, angleDiff);   //上下の角度
                t.RotateAround(t.position, t.right, rotateY);
                t.RotateAround(t.position, t.up, rotateX);
            }
        }
        //残り弾丸がMAXで撃つと一瞬で弾丸が1個回復するので
        //残り弾丸がMAXで撃った場合のみリキャストを0にする
        if(BulletsRemain == BulletsNum)
        {
            RecastCountTime = 0;
        }
        BulletsRemain--;    //残り弾数を減らす
        ShotCountTime = 0;  //発射間隔のカウントをリセット


        //デバッグ用
        Debug.Log("残り弾数: " + BulletsRemain);
    }
}