using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MissieWeapon : BaseWeapon
{
    [SerializeField] MissileBullet missile = null;  //複製する弾丸
    List<MissileBullet> createMissiles = new List<MissileBullet>();
    const short USE_MISSILE_INDEX = 0;

    //弾丸のパラメータ
    [SerializeField] float speedPerSecond = 13.0f;  //1秒間に進む量
    [SerializeField] float destroyTime = 2.0f;      //発射してから消えるまでの時間(射程)
    [SerializeField] float trackingPower = 2.3f;    //追従力
    [SerializeField] float shotPerSecond = 1.0f;    //1秒間に発射する弾数

    protected override void Start()
    {
        Recast = 3.0f;
        ShotInterval = 1.0f / shotPerSecond;
        ShotCountTime = ShotInterval;
        BulletsNum = 3;
        BulletsRemain = BulletsNum;
        BulletPower = 20.0f;

        for (int i = 0; i < BulletsNum; i++)
        {
            CreateMissile();
        }
        createMissiles[USE_MISSILE_INDEX].gameObject.SetActive(true);
    }

    protected override void Update()
    {
        //リキャストのカウント
        RecastCountTime += Time.deltaTime;
        if (RecastCountTime > Recast)
        {
            RecastCountTime = Recast;
        }

        //発射間隔のカウント
        if (ShotCountTime < ShotInterval)
        {
            ShotCountTime += Time.deltaTime;
            if (ShotCountTime > ShotInterval)
            {
                ShotCountTime = ShotInterval;
                if (BulletsRemain > 0)
                {
                    createMissiles[USE_MISSILE_INDEX].gameObject.SetActive(true);
                }
            }
        }


        //リキャスト時間経過したら弾数を1個補充
        if (RecastCountTime >= Recast)
        {
            //残り弾数が最大弾数に達していなかったら補充
            if (BulletsRemain < BulletsNum)
            {
                CreateMissile();
                //弾丸が空だったら生成してすぐに表示する
                if(BulletsRemain <= 0)
                {
                    createMissiles[USE_MISSILE_INDEX].gameObject.SetActive(true);
                }
                BulletsRemain++;        //弾数を回復
                RecastCountTime = 0;    //リキャストのカウントをリセット


                //デバッグ用
                Debug.Log("ミサイルの弾丸が1回分補充されました");
            }
        }
    }

    void CreateMissile()
    {
        MissileBullet m = Instantiate(missile);    //ミサイルの複製
        m.transform.SetParent(transform);
        m.transform.localPosition = new Vector3(0, 0, 0);
        m.InitRotate();

        //弾丸のパラメータ設定
        m.Shooter = Shooter;    //撃ったプレイヤーを登録
        m.SpeedPerSecond = speedPerSecond;  //スピード
        m.DestroyTime = destroyTime;        //射程
        m.TrackingPower = trackingPower;    //誘導力
        m.Power = BulletPower;              //威力

        //発射するまでスクリプト無効
        m.enabled = false;

        //一旦非表示
        m.gameObject.SetActive(false);

        createMissiles.Add(m);
    }


    public override void Shot(GameObject target = null)
    {
        //前回発射して発射間隔分の時間が経過していなかったら撃たない
        if (ShotCountTime < ShotInterval)
        {
            return;
        }

        //バグ防止
        if(createMissiles.Count <= 0 || createMissiles[USE_MISSILE_INDEX] == null)
        {
            return;
        }

        //残り弾数が0だったら撃たない
        if (BulletsRemain <= 0)
        {
            return;
        }

        //ミサイルスクリプトを有効にして親子関係を外す
        createMissiles[USE_MISSILE_INDEX].enabled = true;
        createMissiles[USE_MISSILE_INDEX].transform.parent = null;

        //ロックオン中の敵
        createMissiles[USE_MISSILE_INDEX].Target = target;

        //発射したミサイルをリストから削除
        createMissiles.RemoveAt(USE_MISSILE_INDEX);


        if (BulletsRemain == BulletsNum)
        {
            RecastCountTime = 0;
        }
        BulletsRemain--;    //残り弾数を減らす
        ShotCountTime = 0;  //発射間隔のカウントをリセット


        //デバッグ用
        Debug.Log("残り弾数: " + BulletsRemain);
    }
}