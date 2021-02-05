using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    namespace CPU
    {
        public class DroneStatusAction : MonoBehaviour
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
            bool[] isStatus = new bool[(int)Status.NONE];   //状態異常が付与されているか

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

            //スピードダウン用
            DroneBaseAction baseAction = null;
            int speedDownSoundId = 0;


            void Start()
            {
                baseAction = GetComponent<DroneBaseAction>();
                soundAction = GetComponent<DroneSoundAction>();
                barrier = GetComponent<DroneBarrierAction>();
                lockOn = GetComponent<DroneLockOnAction>();
                radar = GetComponent<DroneRadarAction>();
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

                //SE停止  
                soundAction.StopLoopSE(jammingSoundId);
                soundAction.StopLoopSE(speedDownSoundId);
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

                barrier.BarrierStrength(strengthPercent, time);
                isStatus[(int)Status.BARRIER_STRENGTH] = true;

                return true;
            }


            //バリア弱体化
            public void SetBarrierWeak()
            {
                if (barrier == null) return;
                if (barrier.IsWeak) return;

                barrier.BarrierWeak();
                isStatus[(int)Status.BARRIER_WEAK] = true;

                //アイコン表示
                barrierWeakIcon.enabled = true;
            }

            //バリア弱体化解除
            public void UnSetBarrierWeak()
            {
                if (barrier == null) return;

                barrier.StopBarrierWeak();
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

                lockOn.StopLockOn();
                radar.StopRadar();
                isStatus[(int)Status.JAMMING] = true;

                //SE再生
                jammingSoundId = soundAction.PlayLoopSE(SoundManager.SE.JAMMING_NOISE, SoundManager.BaseSEVolume);

                //アイコン表示
                jammingIcon.enabled = true;
            }

            //ジャミング解除
            public void UnSetJamming()
            {
                isStatus[(int)Status.JAMMING] = false;

                //SE停止
                soundAction.StopLoopSE(jammingSoundId);

                //アイコン非表示
                jammingIcon.enabled = false;
            }


            //スピードダウン
            public void SetSpeedDown(float downPercent)
            {
                baseAction.ModifySpeed(1 - downPercent);

                isStatus[(int)Status.SPEED_DOWN] = true;

                //アイコン表示
                speedDownIcon.enabled = true;

                //SE再生
                speedDownSoundId = soundAction.PlayLoopSE(SoundManager.SE.MAGNETIC_AREA, SoundManager.BaseSEVolume);
            }

            //スピードダウン解除
            public void UnSetSpeedDown(ref float speed)
            {
                //アイコン非表示
                speedDownIcon.enabled = false;

                //SE停止
                soundAction.StopLoopSE(speedDownSoundId);
            }
        }
    }
}