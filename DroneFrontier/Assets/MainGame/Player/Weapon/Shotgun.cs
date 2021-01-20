using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Shotgun : BaseWeapon
{
    //ショットガンのパラメータ
    [SerializeField] Bullet bullet = null;    //弾のオブジェクト
    [SerializeField, Tooltip("拡散力")] float angle = 10.0f;     //拡散力
    [SerializeField, Tooltip("拡散力のランダム値")] float angleDiff = 3.0f;  //角度の変動量
    AudioSource audioSource = null;

    //弾丸のパラメータ
    [SerializeField, Tooltip("1秒間に進む距離")] float speedPerSecond = 10.0f;  //1秒間に進む量
    [SerializeField, Tooltip("射程")] float destroyTime = 0.3f;      //発射してから消えるまでの時間(射程)
    float trackingPower = 0;       //追従力
    [SerializeField, Tooltip("1秒間に発射する弾数")] float shotPerSecond = 2.0f;    //1秒間に発射する弾数

    [SerializeField, Tooltip("リキャスト時間")] float _recast = 2f;
    [SerializeField, Tooltip("ストック可能な弾数")] int _bulletsNum = 5;
    [SerializeField, Tooltip("威力")] float _power = 8f;

    public override void OnStartClient()
    {
        base.OnStartClient();
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.SHOTGUN);
        audioSource.volume = SoundManager.BaseSEVolume;
    }

    protected override void Start()
    {
        Recast = _recast;
        ShotInterval = 1.0f / shotPerSecond;
        ShotCountTime = ShotInterval;
        BulletsNum = _bulletsNum;
        BulletsRemain = BulletsNum;
        BulletPower = _power;
        
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

    public override void Init()
    {
    }

    public override void UpdateMe()
    {
    }

    public override void Shot(GameObject target = null)
    {
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
                CmdCreateBullet(shotPos.position, transform.rotation, angle * i, angle * j, target);
            }
        }
        //残り弾丸がMAXで撃つと一瞬で弾丸が1個回復するので
        //残り弾丸がMAXで撃った場合のみリキャストを0にする
        if (BulletsRemain == BulletsNum)
        {
            RecastCountTime = 0;
        }
        BulletsRemain--;    //残り弾数を減らす
        ShotCountTime = 0;  //発射間隔のカウントをリセット


        //デバッグ用
        Debug.Log("残り弾数: " + BulletsRemain);
    }

    Bullet CreateBullet(Vector3 pos, Quaternion rotation, float angleX, float angleY, GameObject target)
    {
        Bullet b = Instantiate(bullet, pos, rotation);    //弾丸の複製

        //弾丸のパラメータ設定
        b.Shooter = Shooter;    //撃ったプレイヤーを登録
        b.Target = target;      //ロックオン中の敵
        b.SpeedPerSecond = speedPerSecond;  //スピード
        b.DestroyTime = destroyTime;        //射程
        b.TrackingPower = trackingPower;    //誘導力
        b.Power = BulletPower;              //威力

        //弾丸の進む方向を変えて散らす処理
        Transform t = b.transform;  //キャッシュ
        float rotateX = angleX + Random.Range(angleDiff * -1, angleDiff);  //左右の角度
        float rotateY = angleY + Random.Range(angleDiff * -1, angleDiff);   //上下の角度
        t.RotateAround(t.position, t.right, rotateY);
        t.RotateAround(t.position, t.up, rotateX);

        return b;
    }

    [Command]
    void CmdCreateBullet(Vector3 pos, Quaternion rotation, float angleX, float angleY, GameObject target)
    {
        Bullet b = CreateBullet(pos, rotation, angleX, angleY, target);
        NetworkServer.Spawn(b.gameObject, connectionToClient);
        RpcPlaySE();
    }

    [ClientRpc]
    void RpcPlaySE()
    {
        audioSource.Play();
    }
}