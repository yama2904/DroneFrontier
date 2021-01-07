using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class LaserWeapon : BaseWeapon
{
    const float SHOT_POSSIBLE_MIN = 0.2f;       //発射可能な最低ゲージ量
    [SerializeField] LaserBullet laserBullet = null;
    [SerializeField] float maxShotTime = 5;     //最大何秒発射できるか
    [SerializeField] float hitPerSecond = 5.0f;     //1秒間にヒットする回数

    Image laserGaugeImage = null;
    float gaugeAmout = 1.0f;

    //攻撃中のフラグ
    enum ShotFlag
    {
        SHOT_START,     //攻撃を始めたらtrue
        SHOT_SHOTING,   //攻撃中は常に更新させる

        NONE
    }
    bool[] isShots = new bool[(int)ShotFlag.NONE];

    protected override void Start()
    {
        //スーパークラスの変数
        Recast = 8.0f;
        ShotInterval = 1.0f / hitPerSecond;
        ShotCountTime = ShotInterval;
        BulletPower = 5.0f;

        laserBullet = Instantiate(laserBullet).GetComponent<LaserBullet>();
        laserBullet.transform.SetParent(transform);
        laserBullet.transform.localPosition = shotPos.localPosition;
        laserBullet.transform.localRotation = shotPos.localRotation;
        laserBullet.ShotInterval = ShotInterval;

        laserGaugeImage = GameObject.Find("LaserGauge").GetComponent<Image>();
        laserGaugeImage.fillAmount = 1.0f;
        gaugeAmout = 1.0f;
    }

    protected override void Update()
    {
        //撃っていない間はリキャストの管理
        if (!isShots[(int)ShotFlag.SHOT_START])
        {
            //処理が無駄なのでゲージがMAXならスキップ
            if (gaugeAmout < 1.0f)
            {
                //ゲージを回復
                gaugeAmout += 1.0f / Recast * Time.deltaTime;
                if (gaugeAmout > 1.0f)
                {
                    gaugeAmout = 1.0f;


                    //デバッグ用
                    Debug.Log("ゲージMAX");
                }
            }
        }


        //あとで直す
        //プレイヤーならUIのゲージを変動させる
        if (Shooter.CompareTag(Player.PLAYER_TAG))
        {
            laserGaugeImage.fillAmount = gaugeAmout;
        }
    }

    void LateUpdate()
    {
        //攻撃中かどうか
        if (isShots[(int)ShotFlag.SHOT_START])
        {
            //攻撃中は常にSHOT_SHOTINGの更新をかける
            if (isShots[(int)ShotFlag.SHOT_SHOTING])
            {
                isShots[(int)ShotFlag.SHOT_SHOTING] = false;
            }
            else
            {
                isShots[(int)ShotFlag.SHOT_START] = false;
                isShots[(int)ShotFlag.SHOT_SHOTING] = false;
            }
        }
    }


    public override void Shot(GameObject target = null)
    {
        //発射に必要な最低限のゲージがないと発射しない
        if (!isShots[(int)ShotFlag.SHOT_START])
        {
            if (gaugeAmout < SHOT_POSSIBLE_MIN)
            {
                return;
            }
            isShots[(int)ShotFlag.SHOT_START] = true;
        }

        laserBullet.Shot(Shooter, BulletPower);
        isShots[(int)ShotFlag.SHOT_SHOTING] = true;

        //撃っている間はゲージを減らす
        if (laserBullet.IsShotBeam)
        {
            //ゲージを減らす
            gaugeAmout -= 1.0f / maxShotTime * Time.deltaTime;
            if (gaugeAmout <= 0)    //ゲージがなくなったらレーザーを止める
            {
                gaugeAmout = 0;
                isShots[(int)ShotFlag.SHOT_SHOTING] = false;
            }
        }
    }
}