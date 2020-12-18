using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Laser : AtackBase
{
    const int MAX_RATE_OVER_TIME = 128;         //チャージのパーティクルのrateOverTime最大値
    const float ONE_SCALE_LINE_LENGTH = 1.3f;   //1スケールごとのLineの長さ
    const float SHOT_POSSIBLE_MIN = 0.2f;       //発射可能な最低ゲージ量

    //角度の初期値
    const float INITIAL_ROTATION_X = 4.0f;
    const float INITIAL_ROTATION_Y = 0;
    const float INITIAL_ROTATION_Z = 0;

    //チャージ用変数
    [SerializeField] float chargeTime = 3.0f;     //チャージする時間
    ParticleSystem charge;
    ParticleSystem.EmissionModule chargeEmission;
    ParticleSystem.MinMaxCurve minMaxCurve;
    float rateovertimeAddAmout;    //割り算は重いので先に計算させる用
    bool isCharged;     //チャージし終わったらtrue

    //レーザー用変数
    [SerializeField] float lineRadius = 0.01f;      //レーザーの半径
    [SerializeField] float lineRange = 4.0f;        //レーザーの射程
    [SerializeField] float maxShotTime = 5;         //最大何秒発射できるか
    GameObject line;
    ParticleSystem lineParticle;
    

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

        //チャージ用変数
        charge = transform.Find("Charge").GetComponent<ParticleSystem>();   //チャージのオブジェクトの取得
        chargeEmission = charge.emission;   //チャージのパーティクルのemission構造体を取得
        rateovertimeAddAmout = MAX_RATE_OVER_TIME / chargeTime;  //1秒間で増加するRateOverTime量

        //レーザー用変数
        line = transform.Find("Line").gameObject;   //レーザーのオブジェクトの取得
        lineParticle = line.GetComponent<ParticleSystem>();
        
        isShots = new bool[(int)ShotFlag.NONE];
        StopShot();


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


    public override void Shot(Transform t, GameObject target = null)
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
            minMaxCurve.constant += rateovertimeAddAmout * Time.deltaTime;
            chargeEmission.rateOverTime = minMaxCurve;

            //MAX_RATE_OVER_TIME経ったら発射
            if (chargeEmission.rateOverTime.constant > MAX_RATE_OVER_TIME)
            {
                //チャージを止めてレーザーを発射
                charge.Stop();
                lineParticle.Play();

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
                .Select(h => h.transform.gameObject)        //GameObject型で取り出す
                .Where(h => h.tag == Player.PLAYER_TAG || h.tag == CPUController.CPU_TAG)     //プレイヤーとCPUのタグのみ判定
                .Where(h => h.name != OwnerName)            //当たり判定に所持者がいたらスルー
                .ToList();  //リスト化

            GameObject hit = SearchNearestObject(hits);
            
            //ヒット処理
            if (hit != null)
            {
                if(hit.tag == Player.PLAYER_TAG)
                {
                    hit.GetComponent<Player>().Damage(BulletPower);
                }
                if(hit.tag == CPUController.CPU_TAG)
                {
                    hit.GetComponent<CPUController>().Damage(BulletPower);
                }
                ShotCountTime = 0;  //発射間隔のカウントをリセット
            }
        }
    }

    //リスト内で最も距離が近いオブジェクトを返す
    GameObject SearchNearestObject(List<GameObject> objects)
    {
        GameObject o = null;

        float minTargetDistance = float.MaxValue;   //初期化
        for (int i = 0; i < objects.Count; i++)
        {
            //レーザーの発射地点とオブジェクトの距離を計算
            float distance = Vector3.Distance(line.transform.position, objects[i].transform.position);

            //距離が最小だったら更新
            if (distance < minTargetDistance)
            {
                minTargetDistance = distance;
                o = objects[i];
            }
        }
        return o;
    }

    //チャージとレーザーを止める
    void StopShot()
    {
        charge.Stop();  //チャージを止める

        //チャージのパーティクルの発生量の初期化
        minMaxCurve.constant = 0;   
        chargeEmission.rateOverTime = minMaxCurve;

        //レーザーを止める
        lineParticle.Stop();    

        //フラグの初期化
        isCharged = false;
        for (int i = 0; i < (int)ShotFlag.NONE; i++)
        {
            isShots[i] = false;
        }
    }
}