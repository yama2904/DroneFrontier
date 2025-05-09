using Drone.Battle;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Status
{
    public class BarrierStrengthenStatus : IDroneStatusChange
    {
        public event EventHandler OnStatusEnd;

        private DroneBarrierComponent _barrier = null;

        public Image InstantiateIcon()
        {
            return null;
        }

        public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
        {
            // パラメータ取得
            float damageDownPercent = (float)addParams[0];

            // バリア強化実行
            _barrier = drone.GetComponent<DroneBarrierComponent>();
            bool success = _barrier.StrengthenBarrier(damageDownPercent, statusSec);

            // 強化に失敗した場合は失敗で返す
            if (!success) return false;

            // 強化時間終了時にステータス変化終了イベント発火
            _barrier.OnStrengthenEnd += OnStrengthenEnd;

            return true;
        }

        /// <summary>
        /// バリア強化終了イベント
        /// </summary>
        /// <param name="o">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnStrengthenEnd(object o, EventArgs e)
        {
            OnStatusEnd?.Invoke(this, EventArgs.Empty);
            _barrier.OnStrengthenEnd -= OnStrengthenEnd;
        }
    }
}