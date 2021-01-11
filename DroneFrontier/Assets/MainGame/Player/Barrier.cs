using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrier : MonoBehaviour, IBarrier, IBarrierStatus
{
    const float MAX_HP = 100;
    public float HP { get; private set; } = MAX_HP;

    public bool IsStrength { get; private set; } = false;
    public bool IsWeak { get; private set; } = false;

    //バリアの回復用変数
    [SerializeField] float regeneStartTime = 8.0f;   //バリアが回復しだす時間
    [SerializeField] float regeneInterval = 1.0f;    //回復する間隔
    [SerializeField] float regeneValue = 5.0f;       //バリアが回復する量
    [SerializeField] float resurrectBarrierTime = 15.0f;   //バリアが破壊されてから修復される時間
    [SerializeField] float resurrectBarrierHP = 10.0f;     //バリアが復活した際のHP
    float regeneCountTime;    //計測用
    bool isRegene;      //回復中か

    float damagePercent;    //ダメージ倍率

    void Start()
    {
        HP = MAX_HP;
        damagePercent = 1;
        regeneCountTime = 0;
        isRegene = true;    //ゲーム開始時はHPMAXで回復の必要がないのでtrue
    }

    void Update()
    {
        //バリア弱体化中は回復処理を行わない
        if (IsWeak)
        {
            return;
        }

        //バリアが破壊されていたら修復処理
        if (HP <= 0)
        {
            if (regeneCountTime >= resurrectBarrierTime)
            {
                ResurrectBarrier(resurrectBarrierHP);
            }
        }
        //バリアが回復を始めるまで待つ
        else if (!isRegene)
        {
            if (regeneCountTime >= regeneStartTime)
            {
                isRegene = true;
                regeneCountTime = 0;
            }
        }
        //バリアの回復処理
        else
        {
            if (regeneCountTime >= regeneInterval)
            {
                if (HP < MAX_HP)
                {
                    Regene(regeneValue);
                }
                regeneCountTime = 0;
            }
        }
        regeneCountTime += Time.deltaTime;
    }

    //HPを回復する
    void Regene(float value)
    {
        HP += regeneValue;
        if (HP >= MAX_HP)
        {
            HP = MAX_HP;
            Debug.Log("バリアHPMAX: " + HP);
        }
        //デバッグ用
        else
        {
            Debug.Log("リジェネ後バリアHP: " + HP);
        }
    }

    //バリアを復活させる
    void ResurrectBarrier(float resurrectHP)
    {
        if (HP > 0)
        {
            return;
        }
        HP = resurrectHP;

        //修復したら回復処理に移る
        isRegene = true;
        regeneCountTime = 0;


        //デバッグ用
        Debug.Log("バリア修復");
    }


    //バリアに引数分のダメージを与える
    public void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power * damagePercent, 1);  //小数点第2以下切り捨て
        HP -= p;
        if (HP < 0)
        {
            HP = 0;
        }
        regeneCountTime = 0;
        isRegene = false;


        Debug.Log("バリアに" + p + "のダメージ\n残りHP: " + HP);
    }

    /*
     * バリアの受けるダメージを軽減する
     * 引数1: 軽減する割合(0～1)
     * 引数2: 軽減する時間(秒数)
     */
    public void BarrierStrength(float strengthPrercent, float time)
    {
        damagePercent = 1 - strengthPrercent;
        Invoke(nameof(EndStrength), time);

        IsStrength = true;


        //デバッグ用
        Debug.Log("バリア強化");
    }
    //time秒後にバリア強化を終了させる
    void EndStrength()
    {
        if (IsWeak)
        {
            return;
        }
        damagePercent = 1;
        IsStrength = false;


        //デバッグ用
        Debug.Log("バリア強化解除");
    }

    //バリア弱体化
    public void BarrierWeak()
    {
        //デバッグ用
        Debug.Log("バリア弱体化");


        if (IsStrength)
        {
            damagePercent = 1;
            IsStrength = false;


            //デバッグ用
            Debug.Log("バリア強化解除");
        }
        else
        {
            HP *= 0.5f;


            //デバッグ用
            Debug.Log("バリアHP: " + HP);
        }

        isRegene = false;
        regeneCountTime = 0;

        IsWeak = true;
    }

    //バリア弱体化解除
    public void ReleaseBarrierWeak()
    {
        if (HP <= 0)
        {
            ResurrectBarrier(resurrectBarrierHP);
        }

        IsWeak = false;


        //デバッグ用
        Debug.Log("バリア弱体化解除");
    }
}
