using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : AtackBase
{
    const int MAX_RATE_OVER_TIME = 128;
    const float ONE_SCALE_LINE_LENGTH = 1.3f;   //1スケールごとのLineの長さ

    //角度の初期値
    const float INITIAL_ROTATION_X = 4.0f;
    const float INITIAL_ROTATION_Y = 0;
    const float INITIAL_ROTATION_Z = 0;

    [SerializeField] float chargeTime = 3.0f;       //チャージする時間
    [SerializeField] float lineRadius = 0.01f;      //レーザーの半径
    [SerializeField] float lineRange = 4.0f;        //レーザーの射程
    [SerializeField] float trackingSpeed = 0.1f;    //ロックオン時の追従スピード

    ParticleSystem charge;
    ParticleSystem.EmissionModule chargeEmission;
    ParticleSystem.MinMaxCurve minMaxCurve;
    bool isCharge;

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

    float rateOverTimeAdd;    //割り算は重いので先に計算させる用

    protected override void Start()
    {
        charge = transform.Find("Charge").GetComponent<ParticleSystem>();
        charge.Stop();  //チャージエフェクトを解除
        chargeEmission = charge.emission;
        minMaxCurve.constant = 0;
        chargeEmission.rateOverTime = minMaxCurve;

        line = transform.Find("Line").gameObject;
        lineParticle = line.GetComponent<ParticleSystem>();
        lineParticle.Stop();

        rateOverTimeAdd = MAX_RATE_OVER_TIME / chargeTime;  //1秒間で増加するRateOverTime量
        isCharge = false;

        isShots = new bool[(int)ShotFlag.NONE];
        for (int i = 0; i < (int)ShotFlag.NONE; i++)
        {
            isShots[i] = false;
        }
    }

    protected override void Update()
    {
        //デバッグ用
        if (Input.GetKeyDown(KeyCode.Q))
        {
            lineParticle.Play();
            isCharge = true;
        }
        if (Input.GetKeyUp(KeyCode.Q))
        {
            lineParticle.Stop();
            isCharge = false;
        }
    }

    private void LateUpdate()
    {
        //攻撃中かどうか
        if (isShots[(int)ShotFlag.SHOT_START])
        {
            //フラグの更新が止まっていたら攻撃をストップ
            if (isShots[(int)ShotFlag.SHOT_SHOTING])
            {
                isShots[(int)ShotFlag.SHOT_SHOTING] = false;
            }
            else
            {
                charge.Stop();
                minMaxCurve.constant = 0;
                chargeEmission.rateOverTime = minMaxCurve;
                lineParticle.Stop();
                isCharge = false;

                isShots[(int)ShotFlag.SHOT_START] = false;
            }
        }
    }

    public override void Shot(Transform t)
    {
        //チャージ処理
        if (!isCharge)
        {
            //攻撃開始時
            if (!isShots[(int)ShotFlag.SHOT_START])
            {
                isShots[(int)ShotFlag.SHOT_START] = true;
                charge.Play();
            }

            //徐々にチャージのエフェクトを増す
            minMaxCurve.constant += rateOverTimeAdd * Time.deltaTime;
            chargeEmission.rateOverTime = minMaxCurve;

            //MAX_RATE_OVER_TIME経ったら発射
            if (chargeEmission.rateOverTime.constant > MAX_RATE_OVER_TIME)
            {
                charge.Stop();
                lineParticle.Play();

                Debug.Log("発射");
                isCharge = true;
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

            //レーザーが敵に当たったか走査
            bool isHit = Physics.SphereCast(line.transform.position, lineRadius, line.transform.forward, out RaycastHit hit, lineRange);
            if (isHit)
            {
                GameObject o = hit.collider.gameObject;
                if (o.tag == Player.PLAYER_TAG)
                {
                    if (o.name != Player.ObjectName)
                    {
                        Debug.Log(o.name + "にhit");
                    }
                }
            }
        }
        isShots[(int)ShotFlag.SHOT_SHOTING] = true;
    }
}