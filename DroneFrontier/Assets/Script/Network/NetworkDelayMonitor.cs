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

    [SerializeField, Tooltip("���e����x�����ԁi�b�j")]
    private float _maxDelaySec = 1;

    private string _delayPlayer = null;

    private Stopwatch _stopwatch = new Stopwatch();

    private CancellationTokenSource _cancel = new CancellationTokenSource();

    private void Start()
    {
        NetworkManager.Singleton.OnUdpReceiveOnMainThread += OnUdpReceive;
        _stopwatch.Start();

        Task.Run(async () =>
        {
            TimeSpan interval = TimeSpan.FromSeconds(_maxDelaySec * 0.5);
            while (true)
            {
                await Task.Delay(interval, cancellationToken: _cancel.Token);
                NetworkManager.Singleton.SendUdpToAll(new FrameSyncPacket(TotalSeconds));
            }
        });
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnUdpReceive;
        _cancel.Cancel();
    }

    /// <summary>
    /// UDP�p�P�b�g��M�C�x���g
    /// </summary>
    /// <param name="name">�v���C���[��</param>
    /// <param name="packet">��M����UDP�p�P�b�g</param>
    private void OnUdpReceive(string name, BasePacket packet)
    {
        if (packet is FrameSyncPacket syncPacket)
        {
            // �x�����̃v���C���[�����Ȃ��ꍇ�͒x���`�F�b�N
            if (_delayPlayer == null)
            {
                // ���肪�x�����Ă���ꍇ�̓Q�[�����~�߂�
                if (TotalSeconds - syncPacket.TotalSeconds >= _maxDelaySec)
                {
                    _delayPlayer = name;
                    Time.timeScale = 0;
                    _stopwatch.Stop();
                    IsPause = true;
                }
            }
            else
            {
                // �x�����Ă���v���C���[�ȊO�͖���
                if (_delayPlayer != name) return;

                // �x�������������ꍇ�͍ĊJ
                if (TotalSeconds - syncPacket.TotalSeconds < _maxDelaySec)
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
