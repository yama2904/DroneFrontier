using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class JammingStatus : IDroneStatusChange
{
    public StatusChangeType StatusType => StatusChangeType.Jamming;

    public event EventHandler StatusEndEvent;

    /// <summary>
    /// ジャミング付与したオブジェクトのLockOnコンポーネント
    /// </summary>
    private DroneLockOnComponent _lockon = null;

    /// <summary>
    /// ジャミング付与したオブジェクトのRadarコンポーネント
    /// </summary>
    private DroneRadarComponent _radar = null;

    /// <summary>
    /// ジャミング付与したオブジェクトのSoundコンポーネント
    /// </summary>
    private DroneSoundComponent _sound = null;

    /// <summary>
    /// 再生したSE番号
    /// </summary>
    private int _seId = 0;

    /// <summary>
    /// キャンセルトークン発行クラス
    /// </summary>
    private CancellationTokenSource _cancel = new CancellationTokenSource();

    public Image InstantiateIcon()
    {
        return Addressables.InstantiateAsync("JammigUI").WaitForCompletion().GetComponent<Image>();
    }

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        // コンポーネント取得
        _lockon = drone.GetComponent<DroneLockOnComponent>();
        _radar = drone.GetComponent<DroneRadarComponent>();
        _sound = drone.GetComponent<DroneSoundComponent>();

        // ジャミング効果付与
        _lockon?.QueueDisabled();
        _radar?.QueueDisabled();

        // ジャミングSE再生
        if (_sound != null)
        {
            _seId = _sound.PlayLoopSE(SoundManager.SE.JAMMING_NOISE);
        }

        // ジャミング終了タイマー設定
        UniTask.Void(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(statusSec), cancellationToken: _cancel.Token);
            EndJamming();
        });

        return true;
    }
    
    /// <summary>
    /// ジャミング終了
    /// </summary>
    public void EndJamming()
    {
        // ジャミング終了
        _lockon?.DequeueDisabled();
        _radar?.DequeueDisabled();
        _sound?.StopLoopSE(_seId);
        
        // ジャミング終了タイマー停止
        _cancel.Cancel();

        // 終了イベント発火
        StatusEndEvent?.Invoke(this, EventArgs.Empty);
    }
}
