using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Barrier : NetworkBehaviour, IBarrier, IBarrierStatus
{
    const float MAX_HP = 100;
    [SyncVar] float syncHP = MAX_HP;
    public float HP { get { return syncHP; } }

    [SyncVar] bool syncIsStrength = false;
    [SyncVar] bool syncIsWeak = false;
    public bool IsStrength { get { return syncIsStrength; } }
    public bool IsWeak { get { return syncIsWeak; } }

    //バリアの回復用変数
    [SerializeField] float regeneStartTime = 8.0f;   //バリアが回復しだす時間
    [SerializeField] float regeneInterval = 1.0f;    //回復する間隔
    [SerializeField] float regeneValue = 5.0f;       //バリアが回復する量
    [SerializeField] float resurrectBarrierTime = 15.0f;   //バリアが破壊されてから修復される時間
    [SerializeField] float resurrectBarrierHP = 10.0f;     //バリアが復活した際のHP
    [SyncVar] float regeneCountTime;    //計測用
    [SyncVar] bool isRegene;    //回復中か

    [SyncVar] float damagePercent;    //ダメージ倍率
    [SyncVar, HideInInspector] public uint parentNetId = 0;


    public override void OnStartClient()
    {
        base.OnStartClient();
        GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
        transform.SetParent(parent.transform);
        transform.localPosition = new Vector3(0, 0, 0);
    }

    void Start()
    {
        damagePercent = 1;
        regeneCountTime = 0;
        isRegene = true;    //ゲーム開始時はHPMAXで回復の必要がないのでtrue
    }

    void Update()
    {
        //バリア弱体化中は回復処理を行わない
        if (syncIsWeak)
        {
            return;
        }

        //バリアが破壊されていたら修復処理
        if (syncHP <= 0)
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
                if (syncHP < MAX_HP)
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
        syncHP += regeneValue;
        if (syncHP >= MAX_HP)
        {
            syncHP = MAX_HP;
            Debug.Log("バリアHPMAX: " + syncHP);
        }
        //デバッグ用
        else
        {
            Debug.Log("リジェネ後バリアHP: " + syncHP);
        }
    }

    //バリアを復活させる
    void ResurrectBarrier(float resurrectHP)
    {
        if (syncHP > 0)
        {
            return;
        }
        syncHP = resurrectHP;

        //修復したら回復処理に移る
        isRegene = true;
        regeneCountTime = 0;


        //デバッグ用
        Debug.Log("バリア修復");
    }


    //バリアに引数分のダメージを与える
    [Command(ignoreAuthority = true)]
    public void CmdDamage(float power)
    {
        float p = Useful.DecimalPointTruncation(power * damagePercent, 1);  //小数点第2以下切り捨て
        syncHP -= p;
        if (syncHP < 0)
        {
            syncHP = 0;
        }
        regeneCountTime = 0;
        isRegene = false;


        Debug.Log("バリアに" + p + "のダメージ\n残りHP: " + syncHP);
    }

    /*
     * バリアの受けるダメージを軽減する
     * 引数1: 軽減する割合(0～1)
     * 引数2: 軽減する時間(秒数)
     */
     [Command(ignoreAuthority = true)]
    public void CmdBarrierStrength(float strengthPrercent, float time)
    {
        damagePercent = 1 - strengthPrercent;
        Invoke(nameof(EndStrength), time);
        syncIsStrength = true;


        //デバッグ用
        Debug.Log("バリア強化");
    }
    //time秒後にバリア強化を終了させる
    void EndStrength()
    {
        if (syncIsWeak)
        {
            return;
        }
        damagePercent = 1;
        syncIsStrength = false;


        //デバッグ用
        Debug.Log("バリア強化解除");
    }

    //バリア弱体化
    [Command(ignoreAuthority = true)]
    public void CmdBarrierWeak()
    {
        //デバッグ用
        Debug.Log("バリア弱体化");


        if (syncIsStrength)
        {
            damagePercent = 1;
            syncIsStrength = false;


            //デバッグ用
            Debug.Log("バリア強化解除");
        }
        else
        {
            syncHP = Useful.DecimalPointTruncation((syncHP *= 0.5f), 1);


            //デバッグ用
            Debug.Log("バリアHP: " + syncHP);
        }

        isRegene = false;
        regeneCountTime = 0;

        syncIsWeak = true;
    }

    //バリア弱体化解除
    [Command(ignoreAuthority = true)]
    public void CmdReleaseBarrierWeak()
    {
        if (syncHP <= 0)
        {
            ResurrectBarrier(resurrectBarrierHP);
        }

        syncIsWeak = false;


        //デバッグ用
        Debug.Log("バリア弱体化解除");
    }
}
