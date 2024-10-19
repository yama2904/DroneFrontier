using Cysharp.Threading.Tasks;
using Offline;
using System;
using UnityEngine;

public class BarrierStrengthen : MonoBehaviour, IDroneStatus
{
    public RectTransform IconImage => null;

    public event EventHandler StatusEndEvent;

    public bool Invoke(GameObject drone, params object[] parameters)
    {
        // パラメータ取得
        float damageDownPercent = (float)parameters[0];
        int time = (int)parameters[1];

        // バリア強化実行
        bool success = drone.GetComponent<DroneBarrierComponent>().StrengthenBarrier(damageDownPercent, time);

        // 強化に失敗した場合は失敗で返す
        if (!success) return false;

        // 強化時間終了タイマー設定
        UniTask.Void(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(time));
            StatusEndEvent?.Invoke(this, EventArgs.Empty);
        });

        return true;
    }
}