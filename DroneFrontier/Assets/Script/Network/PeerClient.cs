using Cysharp.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// P2P�ڑ��N���C�A���g
    /// </summary>
    public class PeerClient : IDisposable
    {
        /// <summary>
        /// �ʐM��v���C���[��
        /// </summary>
        public string RemoteName { get; private set; } = string.Empty;

        /// <summary>
        /// �ʐM��̃z�X�g/�N���C�A���g
        /// </summary>
        public PeerType RemoteType { get; private set; } = PeerType.None;

        /// <summary>
        /// TCP�p�P�b�g��M�C�x���g
        /// </summary>
        public event ReceiveHandler OnTcpReceived;

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g
        /// </summary>
        public event ReceiveHandler OnUdpReceived;

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g�i���C���X���b�h��Ŏ��s�j
        /// </summary>
        public event ReceiveHandler OnUdpReceivedOnMainThread;

        /// <summary>
        /// �v���C���[�ؒf�C�x���g
        /// </summary>
        public event EventHandler OnDisconnected;

        private TcpClient _tcp;
        private UdpClient _udp;

        private ConcurrentQueue<(byte[] data, IPEndPoint ep)> _receivedUdpQueue = new ConcurrentQueue<(byte[] data, IPEndPoint ep)>();
        private ConcurrentQueue<BasePacket> _invokeUdpQueue = new ConcurrentQueue<BasePacket>();

        public PeerClient(string name, PeerType type, UdpClient udp, TcpClient tcp)
        {
            RemoteName = name;
            RemoteType = type;
            _udp = udp;
            _tcp = tcp;

            // ��M�J�n
            ReceiveTcp();
            ReceiveUdp();
        }

        /// <summary>
        /// UDP�p�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        public void SendUdp(BasePacket packet)
        {
            byte[] data = packet.ConvertToPacket();
            _udp.Send(data, data.Length);
        }

        /// <summary>
        /// TCP�p�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        public void SendTcp(BasePacket packet)
        {
            byte[] data = packet.ConvertToPacket();
            _tcp.GetStream().Write(data, 0, data.Length);
        }

        /// <summary>
        /// �ʐM�ؒf
        /// </summary>
        public void Disconnect()
        {
            // TCP�ؒf/��~
            _tcp?.Close();
            _tcp?.Dispose();
            _tcp = null;

            // UDP��~
            _udp?.Close();
            _udp?.Dispose();
            _udp = null;

            // ��M�L���[�폜
            _receivedUdpQueue.Clear();
            _invokeUdpQueue.Clear();
        }

        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// �w�肵���v���C���[�����TCP��M���J�n����
        /// </summary>
        private void ReceiveTcp()
        {
            UniTask.Void(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (_tcp == null) break;

                        // Tcp��M�ҋ@
                        byte[] buf = new byte[2048];
                        int size = await _tcp.GetStream().ReadAsync(buf, 0, buf.Length);

                        // �ؒf�`�F�b�N
                        if (size == 0)
                        {
                            _tcp?.Close();
                            _tcp?.Dispose();

                            // �ؒf�C�x���g����
                            OnDisconnected?.Invoke(this, EventArgs.Empty);
                            Dispose();

                            break;
                        }

                        // �^���擾
                        Type type = BasePacket.GetPacketType(buf);

                        // �^������ɃR���X�g���N�^�����擾
                        var constructor = type.GetConstructor(Type.EmptyTypes);
                        var expression = Expression.Lambda<Func<BasePacket>>(Expression.New(constructor)).Compile();
                        // �R���X�g���N�^���s
                        BasePacket packet = expression();

                        // �C�x���g����
                        OnTcpReceived?.Invoke(this, packet.Parse(buf));
                    }
                }
                catch (IOException)
                {
                    // ��M�L�����Z��
                }
            });
        }

        /// <summary>
        /// UDP��M���J�n����
        /// </summary>
        private void ReceiveUdp()
        {
            MonitorUdpReceiveData();
            MonitorUdpInvokeData();

            var semaphore = new SemaphoreSlim(1, 1);
            ParallelOptions options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 2
            };
            Parallel.For(0, 2, async i =>
            {
                try
                {
                    while (true)
                    {
                        if (_udp == null) break;

                        // �p�P�b�g��M
                        await semaphore.WaitAsync();
                        var result = await _udp.ReceiveAsync().ConfigureAwait(false);
                        semaphore.Release();

                        // ��M�f�[�^���L���[�ɒǉ�
                        _receivedUdpQueue.Enqueue((result.Buffer, result.RemoteEndPoint));
                    }
                }
                catch (Exception)
                {
                    // �ؒf
                    semaphore.Release();
                }

                Debug.Log("ReceiveUdp() End");
            });
        }

        private async void MonitorUdpReceiveData()
        {
            while (true)
            {
                if (_receivedUdpQueue.Count > 0)
                {
                    while (_receivedUdpQueue.TryDequeue(out var data))
                    {
                        // �^���擾
                        Type type = BasePacket.GetPacketType(data.data);

                        // �^������ɃR���X�g���N�^�����擾
                        var constructor = type.GetConstructor(Type.EmptyTypes);
                        var expression = Expression.Lambda<Func<BasePacket>>(Expression.New(constructor)).Compile();
                        // �R���X�g���N�^���s
                        BasePacket packet = expression();

                        // �p�P�b�g���
                        BasePacket udpPacket = packet.Parse(data.data);

                        // ���C���X���b�h���s�p�L���[�։�̓p�P�b�g�ǉ�
                        _invokeUdpQueue.Enqueue(udpPacket);

                        // ���[�J�[�X���b�h��UDP��M�C�x���g����
                        UniTask.Void(async () =>
                        {
                            OnUdpReceived?.Invoke(this, udpPacket);
                            await UniTask.CompletedTask;
                        });
                    }
                }

                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        private async void MonitorUdpInvokeData()
        {
            while (true)
            {
                if (_invokeUdpQueue.Count > 0)
                {
                    while (_invokeUdpQueue.TryDequeue(out var packet))
                    {
                        OnUdpReceivedOnMainThread?.Invoke(this, packet);
                    }
                }

                await UniTask.Delay(1, ignoreTimeScale: true);
            }
        }
    }
}
