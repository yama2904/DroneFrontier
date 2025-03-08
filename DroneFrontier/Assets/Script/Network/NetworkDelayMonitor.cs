using Network;
using Network.Udp;
using UnityEngine;

public class NetworkDelayMonitor : MonoBehaviour
{
    [SerializeField, Tooltip("許容する遅延時間（秒）")]
    private float _maxDelaySec = 1;

    /// <summary>
    /// パケット連番
    /// </summary>
    private long _sequenceId = 0;

    private string _delayPlayer = null;

    private void Start()
    {
        MyNetworkManager.Singleton.OnUdpReceive += OnUdpReceive;
    }
     
    private void Update()
    {
        MyNetworkManager.Singleton.SendToAll(new FrameSyncPacket(_sequenceId));
        if (_delayPlayer == null)
        {
            _sequenceId++;
        }
    }

    private void OnDestroy()
    {
        MyNetworkManager.Singleton.OnUdpReceive -= OnUdpReceive;
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
        long seq = syncPacket.SequenceId;
        
        // 遅延中のプレイヤーがいない場合は遅延チェック
        if (_delayPlayer == null)
        {
            // 相手が遅延している場合はゲームを止める
            if (_sequenceId - seq >= Application.targetFrameRate * _maxDelaySec)
            {
                _delayPlayer = name;
                Time.timeScale = 0;
            }

            DebugLogger.OutLog($"●_packetId:{_sequenceId}");
            DebugLogger.OutLog($"●id:{seq}");
        }
        else
        {
            // 遅延しているプレイヤー以外は無視
            if (_delayPlayer != name) return;


            if (_sequenceId - Application.targetFrameRate * _maxDelaySec < seq)
            {
                _delayPlayer = null;
                Time.timeScale = 1;
            }

            DebugLogger.OutLog($"★_packetId:{_sequenceId}");
            DebugLogger.OutLog($"★id:{seq}");
        }
    }
}
