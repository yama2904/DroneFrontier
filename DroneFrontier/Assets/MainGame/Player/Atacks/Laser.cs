using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Laser : AtackBase
{
    const float SHOT_POSSIBLE_MIN = 0.2f;       //発射可能な最低ゲージ量

    ////角度の初期値
    //const float INITIAL_ROTATION_X = 4.0f;
    //const float INITIAL_ROTATION_Y = 0;
    //const float INITIAL_ROTATION_Z = 0;

    //Charge用変数
    const string CHARGE_OBJECT_NAME = "Charge";
    const int MAX_RATE_OVER_TIME = 128;         //チャージのパーティクルのrateOverTime最大値
    [SerializeField] float chargeTime = 3.0f;     //チャージする時間
    ParticleSystem charge;
    float rateovertimeAddAmout;    //割り算は重いので先に計算させる用
    bool isCharged;     //チャージし終わったらtrue

    //Start用変数
    const string START_OBJECT_NAME = "Start";
    GameObject start;

    //Midway用変数
    const string MIDWAY_OBJECT_NAME = "Midway";
    const string LINE_OBJECT_NAME = "Line";
    const string THUNDER_CONTROLLER_OBJECT_NAME = "thunderController";
    [SerializeField] float lineRadius = 0.01f;      //レーザーの半径
    [SerializeField] float lineRange = 4.0f;        //レーザーの射程
    [SerializeField] float maxShotTime = 5;         //最大何秒発射できるか
    GameObject line;
    GameObject thunderController;
    float initThunderScaleZ;    //初期のthunderの長さ(敵にレーザーが当たった際に使う)
    float initThunderPosZ;      //初期のthunderのz座標

    //End用変数
    const string END_OBJECT_NAME = "End";
    const float END_POS_DIFF = -0.2f;
    GameObject end;


    //攻撃中のフラグ
    bool[] isShots;
    enum ShotFlag
    {
        SHOT_START,     //攻撃を始めたらtrue
        SHOT_SHOTING,   //攻撃中は常に更新させる

        NONE
    }


    //デバッグ用
    Image image;
    float gaugeAmout;   //ゲージ量


    protected override void Start()
    {
        //リキャスト、1秒間にヒットする回数、弾数、威力
        InitValue(8.0f, 5.0f, 0, 5);     //レーザーは弾数ではなくゲージ量で管理するので弾数の引数は0

        //Charge用処理//
        charge = transform.Find("Charge").GetComponent<ParticleSystem>();   //チャージのオブジェクトの取得
        rateovertimeAddAmout = MAX_RATE_OVER_TIME / chargeTime;  //1秒間で増加するRateOverTime量


        //Midway用処理//
        GameObject midway = transform.Find(MIDWAY_OBJECT_NAME).gameObject;

        //Lineオブジェクト
        line = midway.transform.Find(LINE_OBJECT_NAME).gameObject;

        //thunderオブジェクト
        thunderController = midway.transform.Find(THUNDER_CONTROLLER_OBJECT_NAME).gameObject;
        initThunderScaleZ = thunderController.transform.GetChild(0).localScale.z;   //初期の長さを保存
        initThunderPosZ = thunderController.transform.GetChild(0).localPosition.z;  //初期のz座標を保存


        //Start用処理//
        start = transform.Find(START_OBJECT_NAME).gameObject;
        start.transform.localRotation = midway.transform.localRotation;  //Midwayと同じ向き


        //End用処理//
        end = transform.Find(END_OBJECT_NAME).gameObject;

        //初期座標の保存
        end.transform.localRotation = midway.transform.localRotation;   //Midwayと同じ向き


        ModifyLaserLength(lineRange);   //Laserの長さを設定した長さに変更
        isShots = new bool[(int)ShotFlag.NONE];
        StopShot(); //開始時は発射させない


        //デバッグ用
        image = GameObject.Find("LaserGauge").GetComponent<Image>();
        gaugeAmout = 1;
    }

    protected override void Update()
    {
        ////デバッグ用
        //if (Input.GetKeyDown(KeyCode.Q))
        //{
        //    lineParticle.Play();
        //    isCharged = true;
        //}
        //if (Input.GetKeyUp(KeyCode.Q))
        //{
        //    lineParticle.Stop();
        //    isCharged = false;
        //}

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


        //デバッグ用
        image.fillAmount = gaugeAmout;
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
            if (gaugeAmout < SHOT_POSSIBLE_MIN)
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
            ParticleSystem.EmissionModule emission = charge.emission;
            ParticleSystem.MinMaxCurve minMaxCurve = emission.rateOverTime;
            minMaxCurve.constant += rateovertimeAddAmout * Time.deltaTime;
            emission.rateOverTime = minMaxCurve;

            //MAX_RATE_OVER_TIME経ったら発射
            if (emission.rateOverTime.constant > MAX_RATE_OVER_TIME)
            {
                //チャージを止めてレーザーを発射
                charge.Stop();

                //Startの再生
                foreach (Transform child in start.transform)
                {
                    child.GetComponent<ParticleSystem>().Play();
                }

                //Midwayの再生
                line.GetComponent<ParticleSystem>().Play();
                thunderController.transform.GetChild(0).GetComponent<ParticleSystem>().Play();

                //Endの再生
                foreach (Transform child in end.transform)
                {
                    child.GetComponent<ParticleSystem>().Play();
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
            gaugeAmout -= 1.0f / maxShotTime * Time.deltaTime;
            if (gaugeAmout <= 0)    //ゲージがなくなったらレーザーを止める
            {
                gaugeAmout = 0;
                StopShot();
            }

            //前回ヒットして発射間隔分の時間が経過していなかったら当たり判定を行わない
            if (ShotCountTime < ShotInterval)
            {
                return;
            }

            //レーザーの射線上にヒットした全てのオブジェクトを調べる
            var hits = Physics.SphereCastAll(
                line.transform.position,    //レーザーの発射座標
                lineRadius,                 //レーザーの半径
                line.transform.forward,     //レーザーの正面
                lineRange)                  //射程
                .Where(h => h.transform.gameObject.name != OwnerName)        //当たり判定に所持者がいたらスルー
                .Where(h => h.transform.gameObject.tag != Item.ITEM_TAG)     //アイテムもスルー
                .Where(h => h.transform.gameObject.tag != Bullet.BULLET_TAG) //弾丸もスルー
                .ToList();  //リスト化  

            float lineLength = lineRange;   //レーザーの長さ
            //ヒット処理
            if (hits.Count > 0)
            {
                SearchNearestObject(out RaycastHit hit, hits);
                GameObject o = hit.transform.gameObject;

                if (o.tag == Player.PLAYER_TAG)
                {
                    o.GetComponent<Player>().Damage(BulletPower);
                }
                if (o.tag == CPUController.CPU_TAG)
                {

                    o.GetComponent<CPUController>().Damage(BulletPower);
                }
                //ヒットしたオブジェクトの距離をレーザーの長さにする
                lineLength = hit.distance;

                //ヒットした場所にEndオブジェクトを移動させる
                end.transform.position = hit.point;

                Debug.Log(lineLength);

                ShotCountTime = 0;  //発射間隔のカウントをリセット
            }
            else
            {
                //レーザーの末端にEndオブジェクトを移動
                end.transform.transform.position = line.transform.position + (line.transform.forward * lineRange);
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
        Vector3 lineScale = line.transform.localScale;
        line.transform.localScale = new Vector3(length, length, lineScale.z);

        //Thunderオブジェクト
        Vector3 thunderScale = thunderController.transform.GetChild(0).localScale;
        thunderController.transform.GetChild(0).localScale = new Vector3(thunderScale.x, thunderScale.y, initThunderScaleZ * length);
        Vector3 thunderPos = thunderController.transform.GetChild(0).localPosition;
        thunderController.transform.GetChild(0).localPosition = new Vector3(thunderPos.x, thunderPos.y, initThunderPosZ * length);
    }

    //チャージとレーザーを止める
    void StopShot()
    {
        charge.Stop();  //Chargeを止める

        //Chargeのパーティクルの発生量の初期化
        ParticleSystem.EmissionModule emission = charge.emission;
        ParticleSystem.MinMaxCurve minMaxCurve = emission.rateOverTime;
        minMaxCurve.constant = 0;
        emission.rateOverTime = minMaxCurve;

        //Startを止める
        foreach (Transform child in start.transform)
        {
            child.GetComponent<ParticleSystem>().Stop();
        }

        //Midwayを止める
        line.GetComponent<ParticleSystem>().Stop();
        thunderController.transform.GetChild(0).GetComponent<ParticleSystem>().Stop();

        //Endを止める
        foreach (Transform child in end.transform)
        {
            child.GetComponent<ParticleSystem>().Stop();
        }

        //フラグの初期化
        isCharged = false;
        for (int i = 0; i < (int)ShotFlag.NONE; i++)
        {
            isShots[i] = false;
        }
    }
}