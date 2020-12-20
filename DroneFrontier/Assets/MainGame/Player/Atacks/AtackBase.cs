using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AtackBase : MonoBehaviour
{
    //武器の所持者のオブジェクト名
    public string OwnerName { get; set; } = "";

    protected float Recast { get; private set; } = -1;       //リキャスト時間
    protected float RecastCountTime { get; set; } = 0;       //リキャスト時間をカウントする変数
    protected float ShotInterval { get; private set; } = -1; //1発ごとの間隔
    protected float ShotCountTime { get; set; } = 0;         //1発ごとの間隔をカウントする変数
    protected int BulletsNum { get; private set; } = -1;     //弾数
    protected int BulletsRemain { get; set; } = 0;           //残り弾数
    protected float BulletPower { get; private set; } = -1;  //弾丸の威力

    protected abstract void Start();

    //リキャスト時間と発射間隔を管理する
    protected virtual void Update()
    {
        RecastCountTime += Time.deltaTime;
        if (RecastCountTime > Recast)
        {
            RecastCountTime = Recast;
        }

        ShotCountTime += Time.deltaTime;
        if (ShotCountTime > ShotInterval)
        {
            ShotCountTime = ShotInterval;
        }
    }

    /*
     * 変数の初期化
     * 引数1: リキャスト時間
     * 引数2: 1秒間に何発発射するか
     * 引数3: 弾数
     * 引数4: 弾丸の威力
     */
    protected void InitValue(float initRecast, float shotParSecond, int bulletsNum, float bulletPower)
    {
        //正常な値を入力されなかったらエラーを吐く
        if(initRecast < 0 || shotParSecond < 0 ||
            bulletsNum < 0 || bulletPower < 0)
        {
            //アプリケーションを落とす
            Application.Quit();
        }
        Recast = initRecast;
        RecastCountTime = 0;
        ShotInterval = 1 / shotParSecond;
        ShotCountTime = ShotInterval;
        BulletsNum = bulletsNum;
        BulletsRemain = bulletsNum;
        BulletPower = bulletPower;
    }

    public abstract void Shot(GameObject target = null);
}
