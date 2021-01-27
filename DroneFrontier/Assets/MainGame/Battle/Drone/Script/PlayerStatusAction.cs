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

    //スピードダウン用
    const int NOT_USE_VALUE = 0;
    List<float> speedDownList = new List<float>();
    float maxSpeed = 0;
    float minSpeed = 0;


    void Start() { }

    public void Init(Barrier barrier, LockOn lockOn, Radar radar, float minSpeed, float maxSpeed)
    {
        //配列初期化
        for (int i = 0; i < (int)Status.NONE; i++)
        {
            isStatus.Add(false);
        }

        this.barrier = barrier;
        this.lockOn = lockOn;
        this.radar = radar;
        this.minSpeed = minSpeed;
        this.maxSpeed = maxSpeed;
        createdStunScreenMask = Instantiate(stunScreenMask);
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

        //リストを使っていなかったらクリア
        bool useList = false;
        foreach (float value in speedDownList)
        {
            if (value != NOT_USE_VALUE)
            {
                useList = true;
                break;
            }
        }
        if (!useList)
        {
            speedDownList.Clear();
        }
    }

    public void ResetStatus()
    {
        for(int i = 0; i < (int)Status.NONE; i++)
        {
            isStatus[i] = false;
        }
        createdStunScreenMask.UnSetStun();
        speedDownList.Clear();
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


    //スピードダウン
    public int SetSpeedDown(ref float speed, float downPercent)
    {
        float speedPercent = 1 - downPercent;
        float tempSpeed = speed;
        speed *= speedPercent;

        if (speed > maxSpeed)
        {
            speed = maxSpeed;
            speedPercent = maxSpeed / tempSpeed;
        }
        if (speed < minSpeed)
        {
            speed = minSpeed;
            speedPercent = minSpeed / tempSpeed;
        }

        speedDownList.Add(speedPercent);
        return speedDownList.Count - 1;
    }

    //スピードダウン解除
    public void UnSetSpeedDown(ref float speed, int id)
    {
        speed /= speedDownList[id];
        speedDownList[id] = NOT_USE_VALUE;
    }
}
