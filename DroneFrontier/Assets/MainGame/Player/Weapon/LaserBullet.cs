using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class LaserBullet : NetworkBehaviour
{
    //パラメータ
    public float ShotInterval { private get; set; } = 0;
    float shotCountTime = 0;
    [SerializeField] float chargeTime = 3.0f;     //チャージする時間
    [SerializeField] float lineRadius = 0.01f;    //レーザーの半径
    [SerializeField] float lineRange = 4.0f;      //レーザーの射程
    public bool IsShotBeam { get; private set; } = false;


    //Charge用変数
    const int MAX_RATE_OVER_TIME = 128;   //チャージのパーティクルのrateOverTime最大値
    [SerializeField] ParticleSystem charge = null;
    ParticleSystem.EmissionModule chargeEmission;
    ParticleSystem.MinMaxCurve chargeMinmaxcurve;
    float rateovertimeAddAmout;    //割り算は重いので先に計算させる用
    bool isCharged;     //チャージし終わったらtrue

    //Start用変数
    [SerializeField] Transform startObjcectTransform = null;
    ParticleSystem[] startChilds;

    //Midway用変数
    [SerializeField] Transform midwayObjectTransform = null;
    [SerializeField] ParticleSystem lineParticle = null;
    [SerializeField] ParticleSystem thunderParticle = null;
    Transform lineTransform;
    Transform thunderTransform;
    float initThunderScaleZ;    //初期のthunderの長さ(敵にレーザーが当たった際に使う)
    float initThunderPosZ;      //初期のthunderのz座標

    //End用変数
    [SerializeField] Transform endObjectTransform = null;
    ParticleSystem[] endChilds;


    //攻撃中のフラグ
    enum ShotFlag
    {
        SHOT_START,     //攻撃を始めたらtrue
        SHOT_SHOTING,   //攻撃中は常に更新させる

        NONE
    }
    bool[] isShots = new bool[(int)ShotFlag.NONE];


    void Start()
    {
        Transform cacheTransform = transform;   //処理の軽量化用キャッシュ

        //Charge用処理//
        chargeEmission = charge.emission;
        chargeMinmaxcurve = chargeEmission.rateOverTime;
        rateovertimeAddAmout = MAX_RATE_OVER_TIME / chargeTime;  //1秒間で増加するRateOverTime量

        //Lineオブジェクト
        lineTransform = lineParticle.transform;

        //thunderオブジェクト
        thunderTransform = thunderParticle.transform;
        initThunderScaleZ = thunderTransform.localScale.z;   //初期の長さを保存
        initThunderPosZ = thunderTransform.localPosition.z;  //初期のz座標を保存


        //Start用処理//
        startChilds = new ParticleSystem[startObjcectTransform.childCount];
        for (int i = 0; i < startObjcectTransform.childCount; i++)
        {
            startChilds[i] = startObjcectTransform.GetChild(i).GetComponent<ParticleSystem>();
        }
        startObjcectTransform.localRotation = midwayObjectTransform.localRotation;  //Midwayと同じ向き


        //End用処理//
        endChilds = new ParticleSystem[endObjectTransform.childCount];
        for (int i = 0; i < endObjectTransform.childCount; i++)
        {
            endChilds[i] = endObjectTransform.GetChild(i).GetComponent<ParticleSystem>();
        }
        //初期座標の保存
        endObjectTransform.localRotation = midwayObjectTransform.localRotation;   //Midwayと同じ向き


        ModifyLaserLength(lineRange);   //Laserの長さを設定した長さに変更
        StopShot(); //開始時は発射させない
    }

    void Update()
    {
        //発射間隔の管理
        shotCountTime += Time.deltaTime;
        if (shotCountTime > ShotInterval)
        {
            shotCountTime = ShotInterval;
        }
    }

    private void LateUpdate()
    {
        //攻撃中かどうか
        if (isShots[(int)ShotFlag.SHOT_START])
        {
            //攻撃中は常にSHOT_SHOTINGの更新をかける
            if (isShots[(int)ShotFlag.SHOT_SHOTING])
            {
                isShots[(int)ShotFlag.SHOT_SHOTING] = false;
            }
            //フラグの更新が止まっていたら攻撃をストップ
            else
            {
                StopShot();
            }
        }
    }

    //リストから必要な要素だけ抜き取る
    List<RaycastHit> FilterTargetRaycast(List<RaycastHit> hits, GameObject shooter)
    {
        //不要な要素を除外する
        return hits.Where(h => !h.transform.CompareTag(TagNameManager.ITEM))      //アイテム除外
                   .Where(h => !h.transform.CompareTag(TagNameManager.BULLET))  //弾丸除外
                   .Where(h =>  //撃ったプレイヤーは当たり判定から除外
                   {
                       return !ReferenceEquals(h.transform.gameObject, shooter);
                   })
                   .Where(h =>  //ジャミングボットを生成したプレイヤーと打ったプレイヤーが同じなら除外
                   {
                       if (h.transform.CompareTag(TagNameManager.JAMMING_BOT))
                       {
                           return !ReferenceEquals(h.transform.GetComponent<JammingBot>().Creater, shooter);
                       }
                       return true;
                   })
                   .ToList();  //リスト化 
    }

    //リスト内で最も距離が近いRaycastHitを返す
    void SearchNearestObject(out RaycastHit hit, List<RaycastHit> hits)
    {
        hit = hits[0];
        float minTargetDistance = float.MaxValue;   //初期化
        foreach (RaycastHit h in hits)
        {
            //距離が最小だったら更新
            if (h.distance < minTargetDistance)
            {
                minTargetDistance = h.distance;
                hit = h;
            }
        }
    }

    //レーザーの長さを変える
    void ModifyLaserLength(float length)
    {
        //Lineオブジェクト
        Vector3 lineScale = lineTransform.localScale;
        lineTransform.localScale = new Vector3(length, length, lineScale.z);

        //Thunderオブジェクト
        Vector3 thunderScale = thunderTransform.localScale;
        thunderTransform.localScale = new Vector3(thunderScale.x, thunderScale.y, initThunderScaleZ * length);
        Vector3 thunderPos = thunderTransform.localPosition;
        thunderTransform.localPosition = new Vector3(thunderPos.x, thunderPos.y, initThunderPosZ * length);
    }

    //チャージとレーザーを止める
    void StopShot()
    {
        charge.Stop();  //Chargeを止める

        //Chargeのパーティクルの発生量の初期化
        chargeMinmaxcurve.constant = 0;
        chargeEmission.rateOverTime = chargeMinmaxcurve;


        //Startを止める
        foreach (ParticleSystem p in startChilds)
        {
            p.Stop();
        }

        //Midwayを止める
        lineParticle.Stop();
        thunderParticle.Stop();

        //Endを止める
        foreach (ParticleSystem p in endChilds)
        {
            p.Stop();
        }

        //フラグの初期化
        isCharged = false;
        IsShotBeam = false;
        for (int i = 0; i < (int)ShotFlag.NONE; i++)
        {
            isShots[i] = false;
        }
    }


    public void Shot(GameObject shooter, float power)
    {
        isShots[(int)ShotFlag.SHOT_SHOTING] = true;

        //チャージ処理
        if (!isCharged)
        {
            //攻撃開始時
            if (!isShots[(int)ShotFlag.SHOT_START])
            {
                isShots[(int)ShotFlag.SHOT_START] = true;
                charge.Play();
            }

            //徐々にチャージのエフェクトを増す
            chargeMinmaxcurve.constant += rateovertimeAddAmout * Time.deltaTime;
            chargeEmission.rateOverTime = chargeMinmaxcurve;

            //MAX_RATE_OVER_TIME経ったら発射
            if (chargeEmission.rateOverTime.constant > MAX_RATE_OVER_TIME)
            {
                //チャージを止めてレーザーを発射
                charge.Stop();

                //Startの再生
                foreach (ParticleSystem p in startChilds)
                {
                    p.Play();
                }

                //Midwayの再生
                lineParticle.Play();
                thunderParticle.Play();

                //Endの再生
                foreach (ParticleSystem p in endChilds)
                {
                    p.Play();
                }

                isCharged = true;
            }
        }
        else
        {
            IsShotBeam = true;

            //前回ヒットして発射間隔分の時間が経過していなかったら当たり判定を行わない
            if (shotCountTime < ShotInterval)
            {
                return;
            }

            //レーザーの射線上にヒットした全てのオブジェクトを調べる
            var hits = Physics.SphereCastAll(
                lineTransform.position,    //レーザーの発射座標
                lineRadius,                //レーザーの半径
                lineTransform.forward,     //レーザーの正面
                lineRange)                 //射程
                .ToList();  //リスト化  

            hits = FilterTargetRaycast(hits, shooter);
            float lineLength = lineRange;   //レーザーの長さ

            //ヒット処理
            if (hits.Count > 0)
            {
                SearchNearestObject(out RaycastHit hit, hits);
                GameObject o = hit.transform.gameObject;    //名前省略

                if (o.CompareTag(TagNameManager.PLAYER) || o.CompareTag(TagNameManager.CPU))
                {
                    o.GetComponent<Player>().Damage(power);
                }
                else if (o.CompareTag(TagNameManager.JAMMING_BOT))
                {
                    o.GetComponent<JammingBot>().Damage(power);
                }

                //ヒットしたオブジェクトの距離とレーザーの長さを合わせる
                lineLength = hit.distance;

                //ヒットした場所にEndオブジェクトを移動させる
                endObjectTransform.position = hit.point;


                shotCountTime = 0;  //発射間隔のカウントをリセット
            }
            else
            {
                //レーザーの末端にEndオブジェクトを移動
                endObjectTransform.position = lineTransform.position + (lineTransform.forward * lineRange);
            }
            //レーザーの長さに応じてオブジェクトの座標やサイズを変える
            ModifyLaserLength(lineLength);
        }
    }
}
