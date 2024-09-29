using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Online
{
    public class DroneStatusAction : NetworkBehaviour
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
        bool[] isStatus = new bool[(int)Status.NONE];  //状態異常が付与されているか

        //アイコン
        [SerializeField] Image barrierWeakIcon = null;
        [SerializeField] Image jammingIcon = null;
        [SerializeField] Image speedDownIcon = null;

        //サウンド
        DroneSoundAction soundAction = null;

        //バリア用
        DroneBarrierAction barrier = null;

        //スタン用
        [SerializeField] StunScreenMask stunScreenMask = null;
        StunScreenMask createdStunScreenMask = null;

        //ジャミング用
        DroneLockOnAction lockOn = null;
        DroneRadarAction radar = null;
        int jammingSoundId = -1;
        int jammingCount = 0;

        //スピードダウン用
        DroneBaseAction baseAction = null;
        int speedDownSoundId = 0;
        int speedDownCount = 0;


        public override void OnStartClient()
        {
            base.OnStartClient();

            baseAction = GetComponent<DroneBaseAction>();
            barrier = GetComponent<DroneBarrierAction>();
            soundAction = GetComponent<DroneSoundAction>();
            lockOn = GetComponent<DroneLockOnAction>();
            radar = GetComponent<DroneRadarAction>();
            createdStunScreenMask = Instantiate(stunScreenMask);
        }

        void Update()
        {
            if (!isLocalPlayer) return;

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

        public void ResetStatus()
        {
            for (int i = 0; i < (int)Status.NONE; i++)
            {
                isStatus[i] = false;
            }
            barrierWeakIcon.enabled = false;
            jammingIcon.enabled = false;
            speedDownIcon.enabled = false;
            createdStunScreenMask.UnSetStun();
            speedDownCount = 0;
        }

        public bool GetIsStatus(Status status)
        {
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

            //アイコン表示
            barrierWeakIcon.enabled = true;
        }

        //バリア弱体化解除
        public void UnSetBarrierWeak()
        {
            if (barrier == null) return;

            barrier.CmdStopBarrierWeak();
            isStatus[(int)Status.BARRIER_WEAK] = false;

            //アイコン非表示
            barrierWeakIcon.enabled = false;
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
            if (++jammingCount > 1) return;  //既にジャミングにかかっている場合は無駄なので処理しない

            lockOn.StopLockOn();
            radar.StopRadar();
            isStatus[(int)Status.JAMMING] = true;

            //SE再生
            jammingSoundId = soundAction.PlayLoopSE(SoundManager.SE.JAMMING_NOISE, SoundManager.SEVolume);

            //アイコン表示
            jammingIcon.enabled = true;
        }

        //ジャミング解除
        public void UnSetJamming()
        {
            //複数のジャミングに同時にかかっている場合は解除しない
            if (--jammingCount > 0) return;

            isStatus[(int)Status.JAMMING] = false;
            soundAction.StopLoopSE(jammingSoundId); //SE停止

            //アイコン非表示
            jammingIcon.enabled = false;
        }


        //スピードダウン
        public void SetSpeedDown(float downPercent)
        {
            baseAction.ModifySpeed(1 - downPercent);
            if (++speedDownCount > 1) return;  //既にスピードダウンにかかっている場合は無駄なので処理しない

            //フラグを立てる
            isStatus[(int)Status.SPEED_DOWN] = true;

            //アイコン表示
            speedDownIcon.enabled = true;

            //SE再生
            speedDownSoundId = soundAction.PlayLoopSE(SoundManager.SE.MAGNETIC_AREA, SoundManager.SEVolume);
        }

        //スピードダウン解除
        public void UnSetSpeedDown(float downPercent)
        {
            baseAction.ModifySpeed(1 / (1 - downPercent));

            //同時にスピードダウンにかかっている場合は解除しない
            if (--speedDownCount > 0) return;

            //フラグ解除
            isStatus[(int)Status.SPEED_DOWN] = false;

            //アイコン非表示
            speedDownIcon.enabled = false;

            //SE停止
            soundAction.StopLoopSE(speedDownSoundId);
        }
    }
}