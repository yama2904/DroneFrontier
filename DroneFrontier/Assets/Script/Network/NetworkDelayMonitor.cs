using Network;
using Network.Udp;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// �l�b�g���[�N�x���Ď��N���X
/// </summary>
public class NetworkDelayMonitor : MonoBehaviour
{
    public static bool IsPause { get; private set; } = false;

    public static float TotalSeconds => (float)_stopwatch.Elapsed.TotalSeconds;

    public static float MaxDelaySec { get; set; }

    [SerializeField, Tooltip("���e����x�����ԁi�b�j")]
    private float _maxDelaySec = 1;

    private static string _delayPlayer = null;

    private static Stopwatch _stopwatch = new Stopwatch();

    private static CancellationTokenSource _cancel = new CancellationTokenSource();

    /// <summary>
    /// �Ď����s
    /// </summary>
    public static void Run()
    {
        // ���s���̏ꍇ���l�����Ē�~
        Stop();

        // ��M�C�x���g�ݒ�
        NetworkManager.OnUdpReceivedOnMainThread += OnUdpReceive;

        // ������
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
    /// �Ď����~
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
    /// UDP�p�P�b�g��M�C�x���g
    /// </summary>
    /// <param name="name">�v���C���[��</param>
    /// <param name="packet">��M����UDP�p�P�b�g</param>
    private static void OnUdpReceive(string name, BasePacket packet)
    {
        if (packet is FrameSyncPacket syncPacket)
        {
            // �x�����̃v���C���[�����Ȃ��ꍇ�͒x���`�F�b�N
            if (_delayPlayer == null)
            {
                // ���肪�x�����Ă���ꍇ�̓Q�[�����~�߂�
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
                // �x�����Ă���v���C���[�ȊO�͖���
                if (_delayPlayer != name) return;

                // �x�������������ꍇ�͍ĊJ
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
