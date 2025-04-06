using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class PlayerStunStatus : IDroneStatusChange
{
    public StatusChangeType StatusType => StatusChangeType.Stun;

    public event EventHandler StatusEndEvent;

    private FadeoutImage _createdMask;

    public Image InstantiateIcon()
    {
        return null;
    }

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        Addressables.InstantiateAsync("StunMask").Completed += handle =>
        {
            _createdMask = handle.Result.GetComponent<FadeoutImage>();
            _createdMask.FadeoutSec = statusSec;
            _createdMask.FadeoutEndEvent += FadeoutEndEvent;
        };
        return true;
    }

    /// <summary>
    /// フェードアウト終了イベント
    /// </summary>
    /// <param name="o">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void FadeoutEndEvent(object o, EventArgs e)
    {
        // ステータス終了イベント発火
        StatusEndEvent?.Invoke(this, EventArgs.Empty);

        // イベント削除してオブジェクト破棄
        _createdMask.FadeoutEndEvent -= FadeoutEndEvent;
        UnityEngine.Object.Destroy(_createdMask);
    }
}
