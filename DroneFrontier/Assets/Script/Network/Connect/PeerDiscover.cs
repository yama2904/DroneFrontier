using Cysharp.Threading.Tasks;
using Network.Tcp;
using Network.Udp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Network.Connect
{
    /// <summary>
    /// P2P�z�X�g�T���N���X
    /// </summary>
    public class PeerDiscover
    {
        /// <summary>
        /// �z�X�g�����C�x���g�n���h���[
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="clients">�z�X�g�������_�̊e�v���C���[�Ƃ�P2P�ڑ��N���C�A���g</param>
        public delegate void HostDiscoveredHandler(PeerDiscover sender, List<PeerClient> clients);

        /// <summary>
        /// �z�X�g�����C�x���g
        /// </summary>
        public event HostDiscoveredHandler OnHostDiscovered;

        /// <summary>
        /// �ʐM�ڑ��C�x���g
        /// </summary>
        public event ConnectionHandler OnConnected;

        /// <summary>
        /// �T�������C�x���g
        /// </summary>
        public event EventHandler OnDiscoveryCompleted;

        /// <summary>
        /// ����M�|�[�g�ԍ�
        /// </summary>
        private readonly int PORT;

        /// <summary>
        /// �ڑ��ς݃N���C�A���g
        /// </summary>
        private List<PeerClient> _connectedClients = new List<PeerClient>();

        /// <summary>
        /// �z�X�g����ڑ��������ɔ��s����L�����Z��
        /// </summary>
        private CancellationTokenSource _completedCancel = new CancellationTokenSource();

        /// <summary>
        /// �z�X�g�ؒf���ɔ��s����L�����Z��
        /// </summary>
        private CancellationTokenSource _hostDisconnectCancel = new CancellationTokenSource();

        private TcpListener _listener = null;

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="port">����M�|�[�g�ԍ�</param>
        public PeerDiscover(int port)
        {
            PORT = port;
        }

        /// <summary>
        /// �z�X�g�̒T�����J�n<br/>
        /// �L�����Z���𔭍s�����ꍇ�͐ڑ��ς݂̑S�Ẵz�X�g/�N���C�A���g�Ɛؒf����
        /// </summary>
        /// <param name="name">�����̃v���C���[��</param>
        /// <param name="gameMode">�Q�[�����[�h</param>
        /// <param name="token">�L�����Z���g�[�N��</param>
        /// <exception cref="TaskCanceledException"></exception>
        public async UniTask StartDiscovery(string name, string gameMode, CancellationToken token)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, 0);
                _listener.Start();

                // �z�X�g��T�����Đڑ��m��
                await DiscoverHost(name, gameMode, token);

                // �V�K�v���C���[����̐ڑ���t
                await AcceptFromClient(name, token);
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

                    // �z�X�g�ؒf�ɂ��L�����Z���̏ꍇ�͑S�v���C���[�Ɛؒf���ăG���[
                    if (_hostDisconnectCancel.IsCancellationRequested)
                    {
                        DisconnectAll();
                        throw new NetworkException(ExceptionError.UnexpectedError, "�z�X�g����ؒf����܂����B");
                    }

                    // �ڑ������ɂ��L�����Z���͐���I��
                }
                else if (ex is NetworkException)
                {
                    // NetworkException�̏ꍇ�͐ؒf���čăX���[
                    DisconnectAll();
                    throw;
                }
                else
                {
                    // NetworkException�ȊO�̏ꍇ�͑z��O�G���[�̂��ߐؒf���ăG���[��f��
                    Debug.Log(ex);
                    DisconnectAll();
                    throw new NetworkException(ExceptionError.UnexpectedError, "�z��O�̃G���[���������܂����B");
                }
            }
            finally
            {
                _listener?.Stop();
                _listener = null;
            }

            // �ڑ������C�x���g����
            OnDiscoveryCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// �z�X�g��T�����Đڑ��m��
        /// </summary>
        /// <param name="name">�����̃v���C���[��</param>
        /// <param name="gameMode">�Q�[�����[�h</param>
        /// <param name="token">�L�����Z���g�[�N��</param>
        /// <exception cref="NetworkException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        private async UniTask DiscoverHost(string name, string gameMode, CancellationToken token)
        {
            // �L�����Z�����s���m�p�^�X�N
            Task cancelTask = Task.Run(() =>
            {
                token.WaitHandle.WaitOne();
            });

            // TCP���b�X���|�[�g
            int listenPort = (_listener.LocalEndpoint as IPEndPoint).Port;

            // �T���J�n
            while (true)
            {
                // UDP�N���C�A���g������
                UdpClient hostUdp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                hostUdp.EnableBroadcast = true;

                // �u���[�h�L���X�g�ŒT���p�P�b�g���M
                byte[] data = new DiscoverPacket(name, gameMode, listenPort).ConvertToPacket();
                await hostUdp.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, PORT));
                hostUdp.EnableBroadcast = false;

                // �ԐM�ҋ@
                UdpReceiveResult receive = new UdpReceiveResult();
                try
                {
                    receive = await ConnectUtil.ReceiveUdpAsync(hostUdp, 10, token);
                }
                catch (Exception ex)
                {
                    hostUdp.Close();
                    hostUdp.Dispose();

                    // �^�C���A�E�g�����ꍇ�͍ēx�T���p�P�b�g���M
                    if (ex is TimeoutException) continue;

                    throw;
                }
                Debug.Log("��M�F" + receive.RemoteEndPoint);

                // �G���[�`�F�b�N
                if (BasePacket.GetPacketType(receive.Buffer) == typeof(ErrorPacket))
                {
                    // �G���[�p�P�b�g���
                    ErrorPacket errPacket = new ErrorPacket().Parse(receive.Buffer) as ErrorPacket;

                    // ��O�\�z
                    NetworkException ex;
                    if (errPacket.ErrorCode == ErrorCode.ExistsName)
                    {
                        ex = new NetworkException(ExceptionError.ExistsName, "�w�肳�ꂽ�v���C���[���͊��Ɏg�p����Ă��܂��B");
                    }
                    else
                    {
                        ex = new NetworkException(ExceptionError.UnexpectedError, "�ʐM�ڑ����ɑz��O�̃G���[���������܂����B");
                    }

                    // ��O�X���[
                    hostUdp.Close();
                    hostUdp.Dispose();
                    throw ex;
                }

                // �����p�P�b�g�ȊO�̏ꍇ�̓X�L�b�v
                if (BasePacket.GetPacketType(receive.Buffer) != typeof(DiscoverResponsePacket)) continue;

                // �����p�P�b�g���
                DiscoverResponsePacket responsePacket = new DiscoverResponsePacket().Parse(receive.Buffer) as DiscoverResponsePacket;

                // �z�X�g�Ƃ̐ڑ����ۑ�
                hostUdp.Connect(receive.RemoteEndPoint);

                // �z�X�g��TCP�R�l�N�V�����m��
                TcpClient hostTcp = new TcpClient(new IPEndPoint(IPAddress.Any, 0));
                try
                {
                    await ConnectUtil.ConnectAsync(hostTcp, receive.RemoteEndPoint.Address, PORT, 10, token);
                }
                catch (Exception ex)
                {
                    hostTcp.Close();
                    hostTcp.Dispose();
                    hostUdp.Close();
                    hostUdp.Dispose();

                    // �^�C���A�E�g�����ꍇ�͂�蒼��
                    if (ex is TimeoutException) continue;

                    throw;
                }

                // �ڑ��ς݈ꗗ�ɒǉ�
                PeerClient hostPeer = new PeerClient(responsePacket.HostName, PeerType.Host, hostUdp, hostTcp);
                _connectedClients.Add(hostPeer);

                // �ؒf�C�x���g�ݒ�
                hostPeer.OnDisconnected += OnDisconnectedPeer;

                // ���v���C���[�Ƃ��ڑ�
                foreach (string address in responsePacket.ClientAddresses)
                {
                    // �e�v���C���[���Ƃ�UDP�N���C�A���g������
                    UdpClient clientUdp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                    int udpPort = (clientUdp.Client.LocalEndPoint as IPEndPoint).Port;

                    // �e�v���C���[���Ƃ�TCP�N���C�A���g������
                    TcpClient clientTcp = new TcpClient(new IPEndPoint(IPAddress.Any, 0));

                    // �ڑ���v���C���[��IP�A�h���X�ATCP�|�[�g�ԍ����o��
                    IPEndPoint tcpEP = NetworkUtil.ConvertToIPEndPoint(address);

                    // �R�l�N�V�����m��
                    try
                    {
                        await ConnectUtil.ConnectAsync(clientTcp, tcpEP.Address, tcpEP.Port, 10, token);
                    }
                    catch (Exception ex)
                    {
                        clientUdp.Close();
                        clientUdp.Dispose();

                        // �^�C���A�E�g
                        if (ex is TimeoutException)
                        {
                            throw new NetworkException(ExceptionError.UnexpectedError, "�ʐM�ڑ����ɑz��O�̃G���[���������܂����B");
                        }

                        throw;
                    }

                    // �v���C���[����Udp�|�[�g�ԍ����M
                    byte[] peerConnectData = new PeerConnectPacket(name, udpPort).ConvertToPacket();
                    await clientTcp.GetStream().WriteAsync(peerConnectData, 0, peerConnectData.Length);

                    // �v���C���[����Udp�|�[�g�ԍ���M
                    byte[] buf = await ConnectUtil.ReceiveTcpAsync(clientTcp, 10, token);

                    // �N���C�A���g�ڑ��p�P�b�g�ȊO�̏ꍇ�͕s�����̂��߃G���[
                    if (BasePacket.GetPacketType(buf) != typeof(PeerConnectPacket))
                    {
                        clientUdp.Close();
                        clientUdp.Dispose();
                        clientTcp.Close();
                        clientTcp.Dispose();
                        throw new NetworkException(ExceptionError.UnexpectedError, "�ʐM�ڑ����ɑz��O�̃G���[���������܂����B");
                    }

                    // �p�P�b�g���
                    PeerConnectPacket connectPacket = new PeerConnectPacket().Parse(buf) as PeerConnectPacket;

                    // UDP�ڑ����m��
                    clientUdp.Connect(tcpEP.Address, connectPacket.UdpPort);

                    // �ڑ��ς݈ꗗ�ɒǉ�
                    PeerClient peerClient = new PeerClient(connectPacket.Name, PeerType.Client, clientUdp, clientTcp);
                    _connectedClients.Add(peerClient);

                    // �ؒf�C�x���g�ݒ�
                    peerClient.OnDisconnected += OnDisconnectedPeer;
                }

                // �N���C�A���g���m�̐ڑ��������z�X�g�ɒʒm
                byte[] connectedData = new ConnectedClientsPacket().ConvertToPacket();
                await hostTcp.GetStream().WriteAsync(connectedData, 0, connectedData.Length);

                // �z�X�g�����TCP��M���J�n����
                hostPeer.OnTcpReceived += OnTcpReceivedFromHost;

                // �z�X�g�����C�x���g����
                OnHostDiscovered?.Invoke(this, new List<PeerClient>(_connectedClients));

                break;
            }
        }

        /// <summary>
        /// �V�K�v���C���[����̐ڑ�����t
        /// </summary>
        /// <param name="name">�����̃v���C���[��</param>
        /// <param name="token">�L�����Z���g�[�N��</param>
        /// <exception cref="NetworkException"></exception>
        private async UniTask AcceptFromClient(string name, CancellationToken token)
        {
            while (true)
            {
                // �V�K�v���C���[����̐ڑ��ҋ@
                TcpClient tcpClient = await ConnectUtil.AcceptTcpClientAsync(_listener, -1, IPAddress.Any, token, _completedCancel.Token, _completedCancel.Token, _hostDisconnectCancel.Token);

                // �N���C�A���g�ڑ��p�P�b�g��M�ҋ@
                byte[] buf = await ConnectUtil.ReceiveTcpAsync(tcpClient, 10, token, _completedCancel.Token, _hostDisconnectCancel.Token);

                // �N���C�A���g�ڑ��p�P�b�g�ȊO�̏ꍇ�͑z��O�̂��߃G���[
                if (BasePacket.GetPacketType(buf) != typeof(PeerConnectPacket))
                {
                    tcpClient.Close();
                    tcpClient.Dispose();
                    throw new NetworkException(ExceptionError.UnexpectedError, "�ʐM�ڑ����ɑz��O�̃G���[���������܂����B");
                }

                // �p�P�b�g���
                PeerConnectPacket connectPacket = new PeerConnectPacket().Parse(buf) as PeerConnectPacket;

                // UDP�ڑ����m��
                UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                udpClient.Connect((tcpClient.Client.RemoteEndPoint as IPEndPoint).Address, connectPacket.UdpPort);

                // �v���C���[����Udp�|�[�g�ԍ����M
                int udpPort = (udpClient.Client.LocalEndPoint as IPEndPoint).Port;
                byte[] peerConnectData = new PeerConnectPacket(name, udpPort).ConvertToPacket();
                await tcpClient.GetStream().WriteAsync(peerConnectData, 0, peerConnectData.Length);

                // �ڑ��ς݈ꗗ�ɒǉ�
                PeerClient peerClient = new PeerClient(connectPacket.Name, PeerType.Client, udpClient, tcpClient);
                _connectedClients.Add(peerClient);

                // �ؒf�C�x���g�ݒ�
                peerClient.OnDisconnected += OnDisconnectedPeer;

                // �ڑ������C�x���g����
                OnConnected?.Invoke(this, peerClient);
            }
        }

        /// <summary>
        /// �z�X�g�����TCP�p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="client">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="name">�v���C���[��</param>
        /// <param name="packet">��M����TCP�p�P�b�g</param>
        private void OnTcpReceivedFromHost(PeerClient client, BasePacket packet)
        {
            if (packet is ConnectionCompletedPacket complete)
            {
                // �L�����Z�����s���Đڑ�����������
                client.OnTcpReceived -= OnTcpReceivedFromHost;
                _completedCancel.Cancel();
            }
        }

        /// <summary>
        /// �ڑ��ς݂̃v���C���[�ؒf�C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnDisconnectedPeer(object sender, EventArgs e)
        {
            PeerClient client = sender as PeerClient;

            // �C�x���g�폜
            client.OnTcpReceived -= OnTcpReceivedFromHost;
            client.OnDisconnected -= OnDisconnectedPeer;

            // �z�X�g����ؒf���ꂽ�ꍇ�̓L�����Z�����s
            if (client.RemoteType == PeerType.Host)
            {
                _hostDisconnectCancel.Cancel();
            }
            else
            {
                // �ڑ��ςݏ�񂩂�폜
                _connectedClients.Remove(client);
            }
        }

        /// <summary>
        /// �S�Ẵz�X�g/�N���C�A���g�ƒʐM��ؒf
        /// </summary>
        private void DisconnectAll()
        {
            // �ʐM�ؒf
            foreach (PeerClient client in _connectedClients)
            {
                client.OnTcpReceived -= OnTcpReceivedFromHost;
                client.OnDisconnected -= OnDisconnectedPeer;
                client.Disconnect();
            }
            _connectedClients.Clear();
        }
    }
}
