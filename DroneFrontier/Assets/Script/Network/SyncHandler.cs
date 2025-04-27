using Cysharp.Threading.Tasks;
using Network.Udp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Network
{
    /// <summary>
    /// �����Ɋւ��鏈�����s���N���X
    /// </summary>
    public class SyncHandler
    {
        /// <summary>
        /// �����p�P�b�g����M�����e�v���C���[��
        /// </summary>
        private List<string> _receivedPlayers = new List<string>();

        /// <summary>
        /// ���������l
        /// </summary>
        private object _syncValue = null;

        /// <summary>
        /// �S�Ẵv���C���[���������s���܂Ŕ񓯊��őҋ@����
        /// </summary>
        /// <param name="timeout">�^�C���A�E�g�i�b�j</param>
        /// <returns>���������������ꍇ��true</returns>
        /// <exception cref="TimeoutException">�^�C���A�E�g</exception>
        public async UniTask WaitAsync(int timeout = 0)
        {
            await SyncValueAsync(null, timeout);
        }

        /// <summary>
        /// �z�X�g���w�肵���l��S�Ẵv���C���[���擾���ē������s���܂Ŕ񓯊��őҋ@����
        /// </summary>
        /// <param name="value">��������l</param>
        /// <param name="timeout">�^�C���A�E�g�i�b�j</param>
        /// <returns>���������������ꍇ��true</returns>
        /// <exception cref="TimeoutException">�^�C���A�E�g</exception>
        public async UniTask<object> SyncValueAsync(object value, int timeout = 0)
        {
            _receivedPlayers.Clear();
            _receivedPlayers.Add(NetworkManager.Singleton.MyPlayerName);

            // �����p�P�b�g��M�C�x���g�ݒ�
            NetworkManager.Singleton.OnUdpReceive += OnUdpReceiveOfSync;

            // ��M�O�ɓ����p�P�b�g���M
            UdpPacket packet = new SimpleSyncPacket();
            if (NetworkManager.Singleton.IsHost)
            {
                if (value != null)
                {
                    packet = new SimpleSyncPacket(value);
                    _syncValue = value;
                }
                NetworkManager.Singleton.SendToAll(packet);
            }

            // �^�C���A�E�g�v���p�X�g�b�v�E�H�b�`�J�n
            Stopwatch timeoutStopwatch = Stopwatch.StartNew();

            // �z�X�g���đ��v���p�X�g�b�v�E�H�b�`�J�n
            Stopwatch retryStopwatch = new Stopwatch();
            if (NetworkManager.Singleton.IsHost)
            {
                retryStopwatch.Start();
            }

            // ���������܂őҋ@
            bool success = false;
            while (true)
            {
                // �S�Ẵv���C���[�����M�����ꍇ�͏I��
                if (_receivedPlayers.Count == NetworkManager.Singleton.PlayerCount)
                {
                    success = true;
                    break;
                }

                // �^�C���A�E�g���m
                if (timeout > 0)
                {
                    if (timeoutStopwatch.Elapsed.Seconds > timeout) break;
                }

                // 1�b���ƂɃ��g���C
                if (retryStopwatch.Elapsed.Seconds >= 1)
                {
                    NetworkManager.Singleton.SendToAll(packet);
                    retryStopwatch.Restart();
                }

                // 100�~���b���ƂɃ`�F�b�N
                await UniTask.Delay(100);
            }

            // �����p�P�b�g��M�C�x���g�폜
            NetworkManager.Singleton.OnUdpReceive -= OnUdpReceiveOfSync;

            if (!success)
            {
                throw new TimeoutException("�^�C���A�E�g�ɒB�������ߓ������L�����Z�����܂����B");
            }

            return _syncValue;
        }

        /// <summary>
        /// �����p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnUdpReceiveOfSync(string name, UdpHeader header, UdpPacket packet)
        {
            // �����p�P�b�g�ȊO�͖���
            if (header != UdpHeader.SimpleSync) return;

            if (!_receivedPlayers.Contains(name))
            {
                _receivedPlayers.Add(name);
            }

            if (_syncValue == null)
            {
                _syncValue = (packet as SimpleSyncPacket).Value;
            }

            // �����p�P�b�g��Ԃ�
            NetworkManager.Singleton.SendToAll(packet);
        }
    }
}