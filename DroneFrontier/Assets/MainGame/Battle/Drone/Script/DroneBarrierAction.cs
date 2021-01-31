using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DroneBarrierAction : NetworkBehaviour
{
    [SerializeField] GameObject barrierObject = null;
    BattleDrone drone = null;

    const float MAX_HP = 100;
    [SyncVar] float syncHP = MAX_HP;
    public float HP { get { return syncHP; } }
    Material material = null;
    const float TRANS_COLOR = 0.5f;

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
    [SyncVar] float syncRegeneCountTime;    //計測用
    [SyncVar] bool syncIsRegene;    //回復中か

    [SyncVar] float syncDamagePercent;    //ダメージ倍率
    [SyncVar, HideInInspector] public uint syncParentNetId = 0;

    void Awake()
    {
        drone = GetComponent<BattleDrone>();
    }
    void Start() { }

    public override void OnStartClient()
    {
        base.OnStartClient();

        drone = GetComponent<BattleDrone>();
        material = barrierObject.GetComponent<Renderer>().material;
        CmdInit();
    }

    [ServerCallback]
    void Update()
    {
        //バリア弱体化中は回復処理を行わない
        if (syncIsWeak) return;

        //ドローンが破壊されていたら回復処理を行わない
        if (drone.IsDestroy) return;

        //バリアが破壊されていたら修復処理
        if (syncHP <= 0)
        {
            if (syncRegeneCountTime >= resurrectBarrierTime)
            {
                ResurrectBarrier(resurrectBarrierHP);
                syncRegeneCountTime = 0;
            }
        }
        //バリアが回復を始めるまで待つ
        else if (!syncIsRegene)
        {
            if (syncRegeneCountTime >= regeneStartTime)
            {
                syncIsRegene = true;
                syncRegeneCountTime = 0;
            }
        }
        //バリアの回復処理
        else
        {
            if (syncRegeneCountTime >= regeneInterval)
            {
                if (syncHP < MAX_HP)
                {
                    Regene(regeneValue);
                }
                syncRegeneCountTime = 0;
            }
        }
        syncRegeneCountTime += Time.deltaTime;
    }

    [Command(ignoreAuthority = true)]
    public void CmdInit()
    {
        syncHP = MAX_HP;
        syncRegeneCountTime = 0;
        syncDamagePercent = 1;
        syncIsRegene = true;
        syncIsStrength = false;
        syncIsWeak = false;
        RpcSetActiveBarrier(true);
        RpcSetBarrierColor(1, false);
    }

    //HPを回復する
    [Server]
    void Regene(float regeneValue)
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

        //バリアの色変え
        float value = syncHP / MAX_HP;
        RpcSetBarrierColor(value, IsStrength);
    }

    //バリアを復活させる
    [Server]
    void ResurrectBarrier(float resurrectHP)
    {
        if (syncHP > 0) return;

        //修復したら回復処理に移る
        syncHP = resurrectHP;
        syncIsRegene = true;

        //バリア復活
        RpcSetActiveBarrier(true);
        float value = syncHP / MAX_HP;
        RpcSetBarrierColor(value, IsStrength);

        //デバッグ用
        Debug.Log("バリア修復");
    }

    #region Damage

    //バリアに引数分のダメージを与える
    [Server]
    public void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power * syncDamagePercent, 1);  //小数点第2以下切り捨て
        syncHP -= p;

        //バリアHPが0になったらバリアを非表示
        if (syncHP <= 0)
        {
            syncHP = 0;
            RpcSetActiveBarrier(false);
            RpcPlaySE(SoundManager.SE.DESTROY_BARRIER);
        }
        syncRegeneCountTime = 0;
        syncIsRegene = false;
        RpcPlaySE(SoundManager.SE.BARRIER_DAMAGE);

        //バリアの色変え
        float value = syncHP / MAX_HP;
        RpcSetBarrierColor(value, IsStrength);

        Debug.Log("バリアに" + p + "のダメージ\n残りHP: " + syncHP);
    }

    #endregion

    #region BarrierStrength

    /*
     * バリアの受けるダメージを軽減する
     * 引数1: 軽減する割合(0～1)
     * 引数2: 軽減する時間(秒数)
     */
    [Command(ignoreAuthority = true)]
    public void CmdBarrierStrength(float strengthPrercent, float time)
    {
        syncDamagePercent = 1 - strengthPrercent;
        Invoke(nameof(CmdEndStrength), time);
        syncIsStrength = true;

        //バリアの色変え
        float value = syncHP / MAX_HP;
        RpcSetBarrierColor(value, IsStrength);


        //デバッグ用
        Debug.Log("バリア強化");
    }
    //バリア強化を終了させる
    [Command(ignoreAuthority = true)]
    void CmdEndStrength()
    {
        if (syncIsWeak)
        {
            return;
        }
        syncDamagePercent = 1;
        syncIsStrength = false;

        //バリアの色変え
        float value = syncHP / MAX_HP;
        RpcSetBarrierColor(value, IsStrength);


        //デバッグ用
        Debug.Log("バリア強化解除");
    }

    #endregion

    #region BarrierWeak

    //バリア弱体化
    [Command(ignoreAuthority = true)]
    public void CmdBarrierWeak()
    {
        //デバッグ用
        Debug.Log("バリア弱体化");


        if (syncIsStrength)
        {
            syncDamagePercent = 1;
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

        //バリアの色変え
        float value = syncHP / MAX_HP;
        RpcSetBarrierColor(value, IsStrength);

        syncIsRegene = false;
        syncRegeneCountTime = 0;

        syncIsWeak = true;
    }

    //バリア弱体化解除
    [Command(ignoreAuthority = true)]
    public void CmdStopBarrierWeak()
    {
        if (syncHP <= 0)
        {
            ResurrectBarrier(resurrectBarrierHP);
        }
        syncIsWeak = false;

        //デバッグ用
        Debug.Log("バリア弱体化解除");
    }

    #endregion

    [ClientRpc]
    void RpcPlaySE(SoundManager.SE se)
    {
        drone.PlayOneShotSE(se, SoundManager.BaseSEVolume);
    }

    [ClientRpc]
    void RpcSetBarrierColor(float value, bool isStrength)
    {
        if (!isStrength)
        {
            material.color = new Color(1 - value, value, 0, value * TRANS_COLOR);
        }
        else
        {
            material.color = new Color(1 - value, 0, value, value * TRANS_COLOR);
        }
    }

    [ClientRpc]
    void RpcSetActiveBarrier(bool flag)
    {
        barrierObject.SetActive(flag);
    }
}
