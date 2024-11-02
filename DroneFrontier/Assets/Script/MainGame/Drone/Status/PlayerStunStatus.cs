using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class PlayerStunStatus : IDroneStatusChange

{
    public StatusChangeType StatusType => StatusChangeType.Stun;

    public Image IconImage => null;

    public event EventHandler StatusEndEvent;

    private FadeoutImage _createdMask;

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        Addressables.LoadAssetAsync<GameObject>("StunMask").Completed += handle =>
        {
            _createdMask = UnityEngine.Object.Instantiate(handle.Result).GetComponent<FadeoutImage>();
            _createdMask.FadeoutSec = statusSec;
            _createdMask.FadeoutEndEvent += FadeoutEndEvent;
            Addressables.Release(handle);
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
