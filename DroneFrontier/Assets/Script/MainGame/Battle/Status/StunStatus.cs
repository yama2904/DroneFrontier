using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Battle.Status
{
    public class StunStatus : IDroneStatusChange
    {
        public event EventHandler OnStatusEnd;

        public Image InstantiateIcon()
        {
            return null;
        }

        public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
        {
            // プレイヤーの場合はスタンによるマスク生成
            if (drone.CompareTag(TagNameConst.PLAYER))
            {
                StunMask mask = Addressables.InstantiateAsync("StunMask").WaitForCompletion().GetComponent<StunMask>();
                mask.OnStunEnd += OnStunEnd;
                mask.Run(drone.GetComponent<IBattleDrone>().Canvas, statusSec);
            }

            // CPUの場合はロックオン停止
            if (drone.CompareTag(TagNameConst.CPU))
            {
                // スタンの間ロックオン機能停止
                DroneLockOnComponent lockon = drone.GetComponent<DroneLockOnComponent>();
                lockon.SetEnableLockOn(false);

                // スタン終了タイマー設定
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(statusSec));
                    lockon.SetEnableLockOn(true);
                    OnStatusEnd?.Invoke(this, EventArgs.Empty);
                });
            }

            return true;
        }

        /// <summary>
        /// スタン終了イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnStunEnd(object sender, EventArgs e)
        {
            // ステータス終了イベント発火
            OnStatusEnd?.Invoke(this, EventArgs.Empty);

            // イベント削除してオブジェクト破棄
            StunMask mask = sender as StunMask;
            mask.OnStunEnd -= OnStunEnd;
            UnityEngine.Object.Destroy(mask.gameObject);
        }
    }
}