using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    namespace Player
    {
        public class DroneStatus : MonoBehaviour
        {
            /// <summary>
            /// 変化中のステータスリスト
            /// </summary>
            public List<IDroneStatus> Statuses { get; private set; } = new List<IDroneStatus>();

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
            DroneSoundComponent soundAction = null;

            //バリア用
            DroneBarrierComponent barrier = null;

            //スタン用
            [SerializeField] StunScreenMask stunScreenMask = null;
            StunScreenMask createdStunScreenMask = null;

            //ジャミング用
            DroneLockOnAction lockOn = null;
            DroneRadarAction radar = null;
            int jammingSoundId = -1;

            //スピードダウン用
            DroneMoveComponent baseAction = null;
            int speedDownSoundId = 0;
            int speedDownCount = 0;


            void Start()
            {
                baseAction = GetComponent<DroneMoveComponent>();
                soundAction = GetComponent<DroneSoundComponent>();
                barrier = GetComponent<DroneBarrierComponent>();
                lockOn = GetComponent<DroneLockOnAction>();
                radar = GetComponent<DroneRadarAction>();
                createdStunScreenMask = Instantiate(stunScreenMask);
            }

            void Update()
            {
                //フラグの更新
                if (barrier != null)
                {
                    //isStatus[(int)Status.BARRIER_STRENGTH] = barrier.IsStrengthenAAA;
                    //isStatus[(int)Status.BARRIER_WEAK] = barrier.IsWeak;
                }
                if (createdStunScreenMask != null)
                {
                    isStatus[(int)Status.STUN] = createdStunScreenMask.IsStun;
                }
            }

            /// <summary>
            /// ドローンにステータス変化を追加する
            /// </summary>
            /// <param name="status">追加するステータス変化</param>
            /// <param name="status">パラメータ</param>
            /// <returns>true:成功, false:失敗</returns>
            public bool AddStatus(IDroneStatus status, params object[] parameters)
            {
                // ステータス変化実行
                bool success = status.Invoke(gameObject, parameters);
                if (!success) return false;

                // ステータス終了イベントを設定してリストに追加
                status.StatusEndEvent += StatusEndEvent;
                Statuses.Add(status);

                // ToDo:ステータス変化アイコンを表示

                return true;
            }

            public bool GetIsStatus(Status status)
            {
                return isStatus[(int)status];
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
                jammingSoundId = soundAction.PlayLoopSE(SoundManager.SE.JAMMING_NOISE, SoundManager.SEVolume);

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
                isStatus[(int)Status.SPEED_DOWN] = true;
                baseAction.MoveSpeed *= (1 - downPercent);
                speedDownCount++;

                //アイコン表示
                speedDownIcon.enabled = true;

                //SE再生
                speedDownSoundId = soundAction.PlayLoopSE(SoundManager.SE.MAGNETIC_AREA, SoundManager.SEVolume);
            }

            //スピードダウン解除
            public void UnSetSpeedDown(float downPercent)
            {
                baseAction.MoveSpeed *= 1 / (1 - downPercent);

                //スピードダウンがすべて解除されたらフラグも解除
                if (--speedDownCount <= 0)
                {
                    isStatus[(int)Status.SPEED_DOWN] = false;
                }

                //アイコン非表示
                speedDownIcon.enabled = false;

                //SE停止
                soundAction.StopLoopSE(speedDownSoundId);
            }

            /// <summary>
            /// ステータス変化終了イベント
            /// </summary>
            /// <param name="sender">イベントオブジェクト</param>
            /// <param name="e">イベント引数</param>
            private void StatusEndEvent(object sender, EventArgs e)
            {
                // ステータスリストから除去
                Statuses.Remove(sender as IDroneStatus);
            }
        }
    }
}