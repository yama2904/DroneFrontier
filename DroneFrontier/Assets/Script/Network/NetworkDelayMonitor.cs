using Network;
using Network.Udp;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkDelayMonitor : MonoBehaviour
{
    public static bool IsPause { get; private set; } = false;

    public float TotalSeconds => (float)_stopwatch.Elapsed.TotalSeconds;

    [SerializeField, Tooltip("許容する遅延時間（秒）")]
    private float _maxDelaySec = 1;

    private string _delayPlayer = null;

    private Stopwatch _stopwatch = new Stopwatch();

    private CancellationTokenSource _cancel = new CancellationTokenSource();

    private void Start()
    {
        MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnUdpReceive;
        _stopwatch.Start();

        Task.Run(async () =>
        {
            TimeSpan interval = TimeSpan.FromSeconds(_maxDelaySec * 0.5);
            while (true)
            {
                await Task.Delay(interval, cancellationToken: _cancel.Token);
                MyNetworkManager.Singleton.SendToAll(new FrameSyncPacket(TotalSeconds));
            }
        });
    }

    private void OnDestroy()
    {
        MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnUdpReceive;
        _cancel.Cancel();
    }

    /// <summary>
    /// UDPパケット受信イベント
    /// </summary>
    /// <param name="name">プレイヤー名</param>
    /// <param name="header">受信したUDPパケットのヘッダ</param>
    /// <param name="packet">受信したUDPパケット</param>
    private void OnUdpReceive(string name, UdpHeader header, UdpPacket packet)
    {
        if (header != UdpHeader.FrameSync) return;

        // パケット情報取得
        FrameSyncPacket syncPacket = packet as FrameSyncPacket;
        float sec = syncPacket.TotalSeconds;
        
        // 遅延中のプレイヤーがいない場合は遅延チェック
        if (_delayPlayer == null)
        {
            // 相手が遅延している場合はゲームを止める
            if (TotalSeconds - sec >= _maxDelaySec)
            {
                _delayPlayer = name;
                Time.timeScale = 0;
                _stopwatch.Stop();
                IsPause = true;
            }
        }
        else
        {
            // 遅延しているプレイヤー以外は無視
            if (_delayPlayer != name) return;

            // 遅延が解消した場合は再開
            if (TotalSeconds - sec < _maxDelaySec)
            {
                _delayPlayer = null;
                Time.timeScale = 1;
                _stopwatch.Start();
                IsPause = false;
            }
        }
    }
}
