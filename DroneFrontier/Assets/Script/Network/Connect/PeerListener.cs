using Cysharp.Threading.Tasks;
using Network.Tcp;
using Network.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Network.Connect
{
    /// <summary>
    /// �N���C�A���g�����P2P�ڑ���t�N���X
    /// </summary>
    public class PeerListener
    {
        /// <summary>
        /// �ʐM�ڑ��C�x���g
        /// </summary>
        public event ConnectionHandler OnConnected;

        /// <summary>
        /// ��t�����C�x���g
        /// </summary>
        public event EventHandler OnAcceptCompleted;

        /// <summary>
        /// �ڑ���t�|�[�g�ԍ�
        /// </summary>
        private readonly int PORT;

        /// <summary>
        /// �ő�ڑ���
        /// </summary>
        private readonly int MAX_CONNECT;

        /// <summary>
        /// �ڑ��ς݃v���C���[��TCP���b�X���ꗗ
        /// </summary>
        private List<(string name, string address)> _clientListenAddresses = new List<(string name, string address)>();

        /// <summary>
        /// �ڑ��ς݃N���C�A���g
        /// </summary>
        private List<PeerClient> _connectedClients = new List<PeerClient>();

        /// <summary>
        /// �ڑ���t���̐ڑ������L�����Z��
        /// </summary>
        private CancellationTokenSource _completedCancel = new CancellationTokenSource();

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="port">�ڑ���t�|�[�g�ԍ�</param>
        /// <param name="maxConnect">�ő�ڑ���</param>
        public PeerListener(int port, int maxConnect)
        {
            PORT = port;
            MAX_CONNECT = maxConnect;
        }

        /// <summary>
        /// �N���C�A���g����̐ڑ���t�J�n<br/>
        /// �L�����Z���𔭍s�����ꍇ�͐ڑ��ς݂̑S�ẴN���C�A���g�Ɛؒf����
        /// </summary>
        /// <param name="name">�����̃v���C���[��</param>
        /// <param name="gameMode">�Q�[�����[�h</param>
        /// <param name="token">�L�����Z���g�[�N��</param>
        /// <exception cref="TaskCanceledException"></exception>
        public async UniTask StartAccept(string name, string gameMode, CancellationToken token)
        {
            // �N���C�A���g����̒T����M�pUDP
            UdpClient broadcastUdp = null;

            // ��M�J�n
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, PORT);
                listener.Start();

                while (true) 
                {
                    // �ő�ڑ����ɒB�����ꍇ�͎�t���Ȃ�
                    if (_connectedClients.Count == MAX_CONNECT)
                    {
                        await Task.Delay(1 * 1000, _completedCancel.Token);
                        continue;
                    }

                    // UDP�N���C�A���g������
                    broadcastUdp?.Close();
                    broadcastUdp?.Dispose();
                    broadcastUdp = new UdpClient(new IPEndPoint(IPAddress.Any, PORT));
                    broadcastUdp.EnableBroadcast = true;

                    // ��M�J�n
                    var receive = await ConnectUtil.ReceiveUdpAsync(broadcastUdp, -1, token, _completedCancel.Token);
                    Debug.Log("��M�F" + receive.RemoteEndPoint);

                    // �v���C���[�T���p�P�b�g�ȊO�̏ꍇ�̓X�L�b�v
                    if (BasePacket.GetPacketType(receive.Buffer) != typeof(DiscoverPacket)) continue;

                    // �Q�[�����[�h���قȂ�ꍇ�̓X�L�b�v
                    DiscoverPacket discoverPacket = new DiscoverPacket().Parse(receive.Buffer) as DiscoverPacket;
                    if (discoverPacket.GameMode != gameMode) continue;

                    // �v���C���[���d���`�F�b�N
                    if (discoverPacket.Name == name || _clientListenAddresses.Any(x => x.name == discoverPacket.Name))
                    {
                        Debug.Log("�v���C���[���d��");

                        // �v���C���[�����d�����Ă���ꍇ�̓G���[�p�P�b�g��Ԃ�
                        byte[] errData = new ErrorPacket(ErrorCode.ExistsName).ConvertToPacket();
                        await broadcastUdp.SendAsync(errData, errData.Length, receive.RemoteEndPoint);

                        // �ēx��M
                        continue;
                    }

                    // ����M�pUDP������
                    UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                    udpClient.Connect(receive.RemoteEndPoint);

                    // �ڑ��ςݏ���Ԃ�
                    List<string> addresses = _clientListenAddresses.Select(x => x.address).ToList();
                    byte[] responseData = new DiscoverResponsePacket(name, addresses).ConvertToPacket();
                    await udpClient.SendAsync(responseData, responseData.Length);

                    // �N���C�A���g�����TCP�ڑ��ҋ@
                    TcpClient tcpClient = null;
                    try
                    {
                        tcpClient = await ConnectUtil.AcceptTcpClientAsync(listener, 10, receive.RemoteEndPoint.Address, token, _completedCancel.Token);
                    }
                    catch (Exception ex)
                    {
                        udpClient.Close();
                        udpClient.Dispose();

                        // �^�C���A�E�g�����ꍇ�͎�M���Ȃ���
                        if (ex is TimeoutException) continue;

                        throw;
                    }

                    // �N���C�A���g���m�̐ڑ������҂�
                    byte[] buf = null;
                    try
                    {
                        buf = await ConnectUtil.ReceiveTcpAsync(tcpClient, 10, token, _completedCancel.Token);
                    }
                    catch (Exception ex)
                    {
                        udpClient.Close();
                        udpClient.Dispose();
                        tcpClient.Close();
                        tcpClient.Dispose();

                        // �^�C���A�E�g�����ꍇ�͎�M���Ȃ���
                        if (ex is TimeoutException) continue;

                        throw;
                    }

                    // �N���C�A���g���m�̐ڑ������p�P�b�g�ȊO�̏ꍇ�͕s�����̂��߃G���[
                    if (BasePacket.GetPacketType(buf) != typeof(ConnectedClientsPacket))
                    {
                        udpClient.Close();
                        udpClient.Dispose();
                        tcpClient.Close();
                        tcpClient.Dispose();
                        throw new NetworkException(ExceptionError.UnexpectedError, "�ʐM�ڑ����ɑz��O�̃G���[���������܂����B");
                    }

                    // �N���C�A���g�̃��b�X���A�h���X�ۑ�
                    _clientListenAddresses.Add((discoverPacket.Name, NetworkUtil.ConvertToString(receive.RemoteEndPoint.Address, discoverPacket.ListenPort)));

                    // �ڑ���N���C�A���g�ۑ�
                    PeerClient peerClient = new PeerClient(discoverPacket.Name, PeerType.Client, udpClient, tcpClient);
                    _connectedClients.Add(peerClient);

                    // �ؒf�C�x���g�ݒ�
                    peerClient.OnDisconnected += OnDisconnectPeer;

                    // �ڑ��C�x���g���s
                    OnConnected?.Invoke(this, peerClient);
                }
            }
            catch (Exception ex)
            {
                // �L�����Z���̏ꍇ
                if (ex is TaskCanceledException)
                {
                    // �Ăяo��������L�����Z�����ꂽ�ꍇ�͐ؒf
                    if (token.IsCancellationRequested)
                    {
                        DisconnectAll();
                        throw;
                    }

                    // �ڑ������ɂ��L�����Z���͏������s
                }
                else
                {
                    // �z��O�̗�O�͐ؒf���ăG���[��f��
                    Debug.Log(ex);
                    DisconnectAll();
                    throw new NetworkException(ExceptionError.UnexpectedError, "�z��O�̃G���[���������܂����B");
                }
            }
            finally
            {
                broadcastUdp?.Close();
                broadcastUdp?.Dispose();
                listener?.Stop();
            }

            // �N���C�A���g�ɐڑ������𑗐M
            foreach (PeerClient client in _connectedClients)
            {
                client.SendTcp(new ConnectionCompletedPacket());
            }

            // �ڑ������C�x���g����
            OnAcceptCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// �N���C�A���g�����ő�ڑ����ɒB����O�ɐڑ���t������������
        /// </summary>
        public void CompleteAppect()
        {
            _completedCancel.Cancel();
        }

        /// <summary>
        /// �S�ẴN���C�A���g�ƒʐM��ؒf
        /// </summary>
        private void DisconnectAll()
        {
            foreach (PeerClient client in _connectedClients)
            {
                client.Disconnect();
            }
            _clientListenAddresses.Clear();
            _connectedClients.Clear();
        }

        /// <summary>
        /// �ڑ��ς݂̃v���C���[�ؒf�C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnDisconnectPeer(object sender, EventArgs e)
        {
            PeerClient client = sender as PeerClient;

            // �ڑ��ςݏ�񂩂�폜
            _connectedClients.Remove(client);
            _clientListenAddresses.RemoveAt(_clientListenAddresses.FindIndex(x => x.name == client.RemoteName));
        }
    }
}
