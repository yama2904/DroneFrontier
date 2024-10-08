﻿using System.Collections;
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

            //バリア用
            DroneBarrierAction barrier = null;

            //ジャミング用
            DroneLockOnAction lockOn = null;

            //スピードダウン用
            DroneBaseAction baseAction = null;
            int speedDownCount = 0;


            void Start()
            {
                baseAction = GetComponent<DroneBaseAction>();
                barrier = GetComponent<DroneBarrierAction>();
                lockOn = GetComponent<DroneLockOnAction>();
            }

            void Update()
            {
                //フラグの更新
                if (barrier != null)
                {
                    isStatus[(int)Status.BARRIER_STRENGTH] = barrier.IsStrength;
                    isStatus[(int)Status.BARRIER_WEAK] = barrier.IsWeak;
                }
            }

            public void ResetStatus()
            {
                for (int i = 0; i < (int)Status.NONE; i++)
                {
                    isStatus[i] = false;
                }
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
            }

            //バリア弱体化解除
            public void UnSetBarrierWeak()
            {
                if (barrier == null) return;

                barrier.StopBarrierWeak();
                isStatus[(int)Status.BARRIER_WEAK] = false;
            }


            //スタン
            public void SetStun(float time)
            {
            }


            //ジャミング
            public void SetJamming()
            {
                if (lockOn == null) return;

                lockOn.StopLockOn();
                isStatus[(int)Status.JAMMING] = true;
            }

            //ジャミング解除
            public void UnSetJamming()
            {
                isStatus[(int)Status.JAMMING] = false;
            }


            //スピードダウン
            public void SetSpeedDown(float downPercent)
            {
                baseAction.ModifySpeed(1 - downPercent);

                isStatus[(int)Status.SPEED_DOWN] = true;
                speedDownCount++;
           }

            //スピードダウン解除
            public void UnSetSpeedDown(ref float speed)
            {
                //スピードダウンがすべて解除されたらフラグも解除
                if (--speedDownCount <= 0)
                {
                    isStatus[(int)Status.SPEED_DOWN] = false;
                }
            }
        }
    }
}