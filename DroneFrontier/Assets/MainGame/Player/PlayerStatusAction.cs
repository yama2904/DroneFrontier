using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerStatusAction : NetworkBehaviour
{
    //弱体や強化などの状態
    public enum Status
    {
        BARRIER_STRENGTH,   //バリア強化
        BARRIER_WEAK,       //バリア弱体化
        STUN,               //スタン
        JAMMING,            //ジャミング
        SPEED_DOWN,         //スピードダウン

        NONE
    }
    List<bool> isStatus = new List<bool>();   //状態異常が付与されているか

    //バリア用
    Barrier barrier = null;

    //スタン用
    [SerializeField] StunScreenMask stunScreenMask = null;
    StunScreenMask createdStunScreenMask = null;

    //ジャミング用
    LockOn lockOn = null;
    Radar radar = null;


    void Start()
    {

    }

    void Update()
    {
        //フラグの更新
        if (barrier != null)
        {
            isStatus[(int)Status.BARRIER_STRENGTH] = barrier.IsStrength;
            isStatus[(int)Status.BARRIER_WEAK] = barrier.IsWeak;
        }
        if (createdStunScreenMask != null)
        {
            isStatus[(int)Status.STUN] = createdStunScreenMask.IsStun;
        }
    }

    public void Init(Barrier barrier, LockOn lockOn, Radar radar)
    {
        //配列初期化
        for (int i = 0; i < (int)Status.NONE; i++)
        {
            isStatus.Add(false);
        }

        this.barrier = barrier;
        this.lockOn = lockOn;
        this.radar = radar;
        createdStunScreenMask = Instantiate(stunScreenMask);
    }

    public bool GetIsStatus(Status status)
    {
        if (isStatus.Count <= 0) return false;  //バグ防止
        return isStatus[(int)status];
    }


    //バリア強化
    public bool SetBarrierStrength(float strengthPercent, float time)
    {
        if (barrier == null) return false;
        if (barrier.IsStrength) return false;
        if (barrier.IsWeak) return false;
        if (barrier.HP <= 0) return false;

        barrier.CmdBarrierStrength(strengthPercent, time);
        isStatus[(int)Status.BARRIER_STRENGTH] = true;
        return true;
    }

    [Command]
    void CmdSetBarrierStrength(float strengthPercent, float time)
    {
        
    }


    //バリア弱体化
    public void SetBarrierWeak()
    {
        if (barrier == null) return;
        if (barrier.IsWeak) return;

        barrier.CmdBarrierWeak();
        isStatus[(int)Status.BARRIER_WEAK] = true;
    }

    //バリア弱体化解除
    public void UnSetBarrierWeak()
    {
        if (barrier == null) return;

        barrier.CmdReleaseBarrierWeak();
        isStatus[(int)Status.BARRIER_WEAK] = false;
    }


    //スタン
    public void SetStun(float time)
    {
        if (createdStunScreenMask == null) return;
        createdStunScreenMask.SetStun(time);
    }


    //ジャミング
    public void SetJamming()
    {
        if (lockOn == null) return;
        if (radar == null) return;

        lockOn.ReleaseLockOn();
        radar.ReleaseRadar();
        isStatus[(int)Status.JAMMING] = true;
    }

    //ジャミング解除
    public void UnSetJamming()
    {
        isStatus[(int)Status.JAMMING] = false;
    }
}
