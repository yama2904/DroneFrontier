using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Laser : AtackBase
{
    const float SHOT_POSSIBLE_MIN = 0.2f;       //発射可能な最低ゲージ量
    Image laserImage;
    [SerializeField] float maxShotTime = 5;         //最大何秒発射できるか

    //Charge用変数
    const int MAX_RATE_OVER_TIME = 128;         //チャージのパーティクルのrateOverTime最大値
    [SerializeField] float chargeTime = 3.0f;     //チャージする時間
    ParticleSystem charge;
    ParticleSystem.EmissionModule chargeEmission;
    ParticleSystem.MinMaxCurve chargeMinmaxcurve;
    float rateovertimeAddAmout;    //割り算は重いので先に計算させる用
    bool isCharged;     //チャージし終わったらtrue

    //Start用変数
    GameObject start;
    ParticleSystem[] startChilds;

    //Midway用変数
    [SerializeField] float lineRadius = 0.01f;      //レーザーの半径
    [SerializeField] float lineRange = 4.0f;        //レーザーの射程
    [SerializeField] float hitPerSecond = 5.0f;     //1秒間にヒットする回数
    ParticleSystem lineParticle;
    Transform lineTransform;
    ParticleSystem thunderParticle;
    Transform thunderTransform;
    float initThunderScaleZ;    //初期のthunderの長さ(敵にレーザーが当たった際に使う)
    float initThunderPosZ;      //初期のthunderのz座標

    //End用変数
    const string END_OBJECT_NAME = "End";
    const float END_POS_DIFF = -0.2f;
    GameObject end;
    Transform endTransform;
    ParticleSystem[] endChilds;


    //攻撃中のフラグ
    bool[] isShots;
    enum ShotFlag
    {
        SHOT_START,     //攻撃を始めたらtrue
        SHOT_SHOTING,   //攻撃中は常に更新させる

        NONE
    }


    protected override void Start()
    {
        Recast = 8.0f;
        ShotInterval = 1.0f / hitPerSecond;
        ShotCountTime = ShotInterval;
        BulletPower = 5.0f;

        Transform cacheTransform = transform;   //処理の軽量化用キャッシュ

        //Charge用処理//
        charge = cacheTransform.Find("Charge").GetComponent<ParticleSystem>();   //チャージのオブジェクトの取得
        chargeEmission = charge.emission;
        chargeMinmaxcurve = chargeEmission.rateOverTime;
        rateovertimeAddAmout = MAX_RATE_OVER_TIME / chargeTime;  //1秒間で増加するRateOverTime量


        //Midway用処理//
        Transform midwayTransform = cacheTransform.Find("Midway").transform;

        //Lineオブジェクト
        lineParticle = midwayTransform.Find("Line").GetComponent<ParticleSystem>();
        lineTransform = lineParticle.transform;

        //thunderオブジェクト
        thunderParticle = midwayTransform.Find("thunderController/thunder").GetComponent<ParticleSystem>();
        thunderTransform = thunderParticle.transform;
        initThunderScaleZ = thunderTransform.localScale.z;   //初期の長さを保存
        initThunderPosZ = thunderTransform.localPosition.z;  //初期のz座標を保存


        //Start用処理//
        start = cacheTransform.Find("Start").gameObject;
        Transform startTransform = start.transform;
        startChilds = new ParticleSystem[startTransform.childCount];
        for (int i = 0; i < startTransform.childCount; i++)
        {
            startChilds[i] = startTransform.GetChild(i).GetComponent<ParticleSystem>();
        }
        startTransform.localRotation = midwayTransform.localRotation;  //Midwayと同じ向き


        //End用処理//
        end = cacheTransform.Find(END_OBJECT_NAME).gameObject;
        endTransform = end.transform;
        endChilds = new ParticleSystem[endTransform.childCount];
        for (int i = 0; i < endTransform.childCount; i++)
        {
            endChilds[i] = endTransform.GetChild(i).GetComponent<ParticleSystem>();
        }
        endTransform.localRotation = midwayTransform.localRotation;

        //初期座標の保存
        endTransform.localRotation = midwayTransform.localRotation;   //Midwayと同じ向き


        ModifyLaserLength(lineRange);   //Laserの長さを設定した長さに変更
        isShots = new bool[(int)ShotFlag.NONE];
        StopShot(); //開始時は発射させない


        //デバッグ用
        laserImage = GameObject.Find("LaserGauge").GetComponent<Image>();
        laserImage.fillAmount = 1;
    }

    protected override void Update()
    {
        //発射間隔の管理
        ShotCountTime += Time.deltaTime;
        if (ShotCountTime > ShotInterval)
        {
            ShotCountTime = ShotInterval;
        }

        //撃っていない間はリキャストの管理
        if (!isShots[(int)ShotFlag.SHOT_START])
        {
            //処理が無駄なのでゲージがMAXならスキップ
            if (laserImage.fillAmount < 1.0f)
            {
                //ゲージを回復
                laserImage.fillAmount += 1.0f / Recast * Time.deltaTime;
                if (laserImage.fillAmount > 1.0f)
                {
                    laserImage.fillAmount = 1.0f;


                    //デバッグ用
                    Debug.Log("ゲージMAX");
                }
            }
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


    public override void Shot(GameObject target = null)
    {
        isShots[(int)ShotFlag.SHOT_SHOTING] = true;

        //チャージ処理
        if (!isCharged)
        {
            //発射に必要な最低限のゲージがないと発射しない
            if (laserImage.fillAmount < SHOT_POSSIBLE_MIN)
            {
                return;
            }

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
            ////ロックオン中はロックオン中のオブジェクトに向けて角度を変える
            //if (LockOn.Target != null)
            //{
            //    Vector3 diff = LockOn.Target.transform.position - line.transform.position;
            //    Quaternion rotation = Quaternion.LookRotation(diff);    //ロックオンしたオブジェクトの方向

            //    //レーザーの角度からtrackingSpeed(0～1)の速度でrotationの角度に向く
            //    line.transform.rotation = Quaternion.Slerp(line.transform.rotation, rotation, trackingSpeed);
            //}
            ////ロックオンしてない場合は初期の角度に戻る
            //else
            //{
            //    Quaternion rotation = Quaternion.Euler(INITIAL_ROTATION_X, INITIAL_ROTATION_Y, INITIAL_ROTATION_Z);

            //    //レーザーの角度からtrackingSpeed(0～1)の速度でrotationの角度に向く
            //    line.transform.localRotation = Quaternion.Slerp(line.transform.localRotation, rotation, trackingSpeed);
            //}


            //ゲージを減らす
            laserImage.fillAmount -= 1.0f / maxShotTime * Time.deltaTime;
            if (laserImage.fillAmount <= 0)    //ゲージがなくなったらレーザーを止める
            {
                laserImage.fillAmount = 0;
                StopShot();
            }

            //前回ヒットして発射間隔分の時間が経過していなかったら当たり判定を行わない
            if (ShotCountTime < ShotInterval)
            {
                return;
            }

            //レーザーの射線上にヒットした全てのオブジェクトを調べる
            var hits = Physics.SphereCastAll(
                lineTransform.position,    //レーザーの発射座標
                lineRadius,                 //レーザーの半径
                lineTransform.forward,     //レーザーの正面
                lineRange)                  //射程
                .ToList();  //リスト化  

            hits = FilterTargetRaycast(hits);

            float lineLength = lineRange;   //レーザーの長さ
            //ヒット処理
            if (hits.Count > 0)
            {
                SearchNearestObject(out RaycastHit hit, hits);
                GameObject o = hit.transform.gameObject;

                if (o.CompareTag(Player.PLAYER_TAG))
                {
                    o.GetComponent<Player>().Damage(BulletPower);
                }
                else if (o.CompareTag(CPUController.CPU_TAG))
                {

                    o.GetComponent<CPUController>().Damage(BulletPower);
                }
                //ヒットしたオブジェクトの距離をレーザーの長さにする
                lineLength = hit.distance;

                //ヒットした場所にEndオブジェクトを移動させる
                endTransform.position = hit.point;


                ShotCountTime = 0;  //発射間隔のカウントをリセット
            }
            else
            {
                //レーザーの末端にEndオブジェクトを移動
                endTransform.position = lineTransform.position + (lineTransform.forward * lineRange);
            }
            //レーザーの長さに応じてオブジェクトの座標やサイズを変える
            ModifyLaserLength(lineLength);
        }
    }

    ////リスト内で最も距離が近いオブジェクトを返す
    //GameObject SearchNearestObject(List<GameObject> objects)
    //{
    //    GameObject o = null;

    //    float minTargetDistance = float.MaxValue;   //初期化
    //    foreach (GameObject _object in objects)
    //    {
    //        //レーザーの発射地点とオブジェクトの距離を計算
    //        float distance = Vector3.Distance(line.transform.position, _object.transform.position);

    //        //距離が最小だったら更新
    //        if (distance < minTargetDistance)
    //        {
    //            minTargetDistance = distance;
    //            o = _object;
    //        }
    //    }
    //    return o;
    //}

    //リストから必要な要素だけ抜き取る
    List<RaycastHit> FilterTargetRaycast(List<RaycastHit> hits)
    {
        //不要な要素を除外する
        return hits.Where(h => !ReferenceEquals(h.transform.gameObject, notHitObject))  //当たり判定を行わないオブジェクトを除外
                   .Where(h => !h.transform.CompareTag(Item.ITEM_TAG))      //アイテム除外
                   .Where(h => !h.transform.CompareTag(Bullet.BULLET_TAG))  //弾丸除外
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
        for (int i = 0; i < (int)ShotFlag.NONE; i++)
        {
            isShots[i] = false;
        }
    }
}