using Drone.Battle;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Battle.Status
{
    public class BarrierWeakStatus : IDroneStatusChange
    {
        public event EventHandler OnStatusEnd;

        private DroneBarrierComponent _barrier = null;

        public Image InstantiateIcon()
        {
            return Addressables.InstantiateAsync("BarrierWeakIcon").WaitForCompletion().GetComponent<Image>();
        }

        public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
        {
            // バリア弱体化実行
            _barrier = drone.GetComponent<DroneBarrierComponent>();
            bool success = _barrier.WeakBarrier(statusSec);

            // 弱体化に失敗した場合は失敗で返す
            if (!success) return false;

            // 弱体化時間終了時にステータス変化終了イベント発火
            _barrier.OnWeakEnd += OnWeakEnd;

            return true;
        }

        /// <summary>
        /// バリア弱体化終了イベント
        /// </summary>
        /// <param name="o">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnWeakEnd(object o, EventArgs e)
        {
            OnStatusEnd?.Invoke(this, EventArgs.Empty);
            _barrier.OnWeakEnd -= OnWeakEnd;
        }
    }
}