using Network;
using Network.Udp;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ネットワーク遅延監視クラス
/// </summary>
public class NetworkDelayMonitor : MonoBehaviour
{
    public static bool IsPause { get; private set; } = false;

    public static float TotalSeconds => (float)_stopwatch.Elapsed.TotalSeconds;

    public static float MaxDelaySec { get; set; }

    [SerializeField, Tooltip("許容する遅延時間（秒）")]
    private float _maxDelaySec = 1;

    private static string _delayPlayer = null;

    private static Stopwatch _stopwatch = new Stopwatch();

    private static CancellationTokenSource _cancel = new CancellationTokenSource();

    /// <summary>
    /// 監視実行
    /// </summary>
    public static void Run()
    {
        // 実行中の場合を考慮して停止
        Stop();

        // 受信イベント設定
        NetworkManager.OnUdpReceivedOnMainThread += OnUdpReceive;

        // 初期化
        _cancel = new CancellationTokenSource();
        _stopwatch = Stopwatch.StartNew();

        Task.Run(async () =>
        {
            while (true)
            {
                TimeSpan interval = TimeSpan.FromSeconds(MaxDelaySec * 0.5);
                await Task.Delay(interval, cancellationToken: _cancel.Token);
                NetworkManager.SendUdpToAll(new FrameSyncPacket(TotalSeconds));
            }
        });
    }

    /// <summary>
    /// 監視を停止
    /// </summary>
    public static void Stop()
    {
        NetworkManager.OnUdpReceivedOnMainThread -= OnUdpReceive;
        _cancel.Cancel();
        _stopwatch.Stop();
    }

    private void Start()
    {
        MaxDelaySec = _maxDelaySec;
    }

    private void OnDestroy()
    {
        Stop();
    }

    /// <summary>
    /// UDPパケット受信イベント
    /// </summary>
    /// <param name="name">プレイヤー名</param>
    /// <param name="packet">受信したUDPパケット</param>
    private static void OnUdpReceive(string name, BasePacket packet)
    {
        if (packet is FrameSyncPacket syncPacket)
        {
            // 遅延中のプレイヤーがいない場合は遅延チェック
            if (_delayPlayer == null)
            {
                // 相手が遅延している場合はゲームを止める
                if (TotalSeconds - syncPacket.TotalSeconds >= MaxDelaySec)
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
                if (TotalSeconds - syncPacket.TotalSeconds < MaxDelaySec)
                {
                    _delayPlayer = null;
                    Time.timeScale = 1;
                    _stopwatch.Start();
                    IsPause = false;
                }
            }
        }
    }
}
