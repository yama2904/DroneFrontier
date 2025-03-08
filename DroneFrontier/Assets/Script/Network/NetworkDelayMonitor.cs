using Network;
using Network.Udp;
using UnityEngine;

public class NetworkDelayMonitor : MonoBehaviour
{
    [SerializeField, Tooltip("���e����x�����ԁi�b�j")]
    private float _maxDelaySec = 1;

    /// <summary>
    /// �p�P�b�g�A��
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
    /// UDP�p�P�b�g��M�C�x���g
    /// </summary>
    /// <param name="name">�v���C���[��</param>
    /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
    /// <param name="packet">��M����UDP�p�P�b�g</param>
    private void OnUdpReceive(string name, UdpHeader header, UdpPacket packet)
    {
        if (header != UdpHeader.FrameSync) return;

        // �p�P�b�g���擾
        FrameSyncPacket syncPacket = packet as FrameSyncPacket;
        long seq = syncPacket.SequenceId;
        
        // �x�����̃v���C���[�����Ȃ��ꍇ�͒x���`�F�b�N
        if (_delayPlayer == null)
        {
            // ���肪�x�����Ă���ꍇ�̓Q�[�����~�߂�
            if (_sequenceId - seq >= Application.targetFrameRate * _maxDelaySec)
            {
                _delayPlayer = name;
                Time.timeScale = 0;
            }

            DebugLogger.OutLog($"��_packetId:{_sequenceId}");
            DebugLogger.OutLog($"��id:{seq}");
        }
        else
        {
            // �x�����Ă���v���C���[�ȊO�͖���
            if (_delayPlayer != name) return;


            if (_sequenceId - Application.targetFrameRate * _maxDelaySec < seq)
            {
                _delayPlayer = null;
                Time.timeScale = 1;
            }

            DebugLogger.OutLog($"��_packetId:{_sequenceId}");
            DebugLogger.OutLog($"��id:{seq}");
        }
    }
}
