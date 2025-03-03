using Cysharp.Threading.Tasks;
using Network.Tcp;
using Network.Udp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Network
{
    public class MyNetworkManager : MonoBehaviour
    {
        public static MyNetworkManager Singleton { get; private set; } = null;

        /// <summary>
        /// �z�X�g���ł��邩
        /// </summary>
        public bool IsHost { get; private set; } = false;

        /// <summary>
        /// �N���C�A���g���ł��邩
        /// </summary>
        public bool IsClient { get; private set; } = false;

        /// <summary>
        /// �����̃v���C���[��
        /// </summary>
        public string MyPlayerName { get; private set; } = string.Empty;

        /// <summary>
        /// �e�v���C���[��
        /// </summary>
        public List<string> PlayerNames { get; private set; } = new List<string>();

        /// <summary>
        /// �v���C���[��
        /// </summary>
        public int PlayerCount => PlayerNames.Count;

        #region �ʐM���蔭���C�x���g

        /// <summary>
        /// �ʐM���蔭���C�x���g�n���h���[
        /// </summary>
        /// <param name="playerName">�ʐM����̃v���C���[��</param>
        public delegate void DiscoveryHandle(string playerName);

        /// <summary>
        /// �ʐM���蔭���C�x���g
        /// </summary>
        public event DiscoveryHandle OnDiscovery;

        #endregion

        #region TCP�p�P�b�g��M�C�x���g

        /// <summary>
        /// TCP�p�P�b�g��M�C�x���g�n���h���[
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="header">��M����TCP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����TCP�p�P�b�g</param>
        public delegate void TcpReceiveHandle(string name, TcpHeader header, TcpPacket packet);

        /// <summary>
        /// TCP�p�P�b�g��M�C�x���g
        /// </summary>
        public event TcpReceiveHandle OnTcpReceive;

        #endregion

        #region UDP�p�P�b�g��M�C�x���g

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g�n���h���[
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        public delegate void UdpReceiveHandle(string name, UdpHeader header, UdpPacket packet);

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g
        /// </summary>
        public event UdpReceiveHandle OnUdpReceive;

        #endregion

        #region �v���C���[�ؒf�C�x���g

        /// <summary>
        /// �v���C���[�ؒf�C�x���g�n���h���[
        /// </summary>
        /// <param name="name">�ؒf�����v���C���[��</param>
        /// <param name="isHost">�ؒf�����v���C���[���z�X�g�ł��邩</param>
        public delegate void DisconnectHandle(string name, bool isHost);

        /// <summary>
        /// �v���C���[�ؒf
        /// </summary>
        public event DisconnectHandle OnDisconnect;

        #endregion

        /// <summary>
        /// �g�p����|�[�g�ԍ�
        /// </summary>
        private const int PORT = 5556;

        /// <summary>
        /// �ő�N���C�A���g��
        /// </summary>
        private const int MAX_CLIENT_NUM = 3;

        /// <summary>
        /// ���[�J���G���h�|�C���g
        /// </summary>
        private readonly IPEndPoint LOCAL_ENDPOINT = new IPEndPoint(IPAddress.Any, PORT);

        /// <summary>
        /// UDP�Ǘ��N���X
        /// </summary>
        private UdpClient _udpClient = null;

        /// <summary>
        /// �ڑ���ꗗ<br/>
        /// key:�v���C���[��<br/>
        /// value:�ڑ�����
        /// </summary>
        private Dictionary<string, (IPEndPoint ep, TcpClient tcp, bool isHost)> _peers = new Dictionary<string, (IPEndPoint ep, TcpClient tcp, bool isHost)>();

        /// <summary>
        /// �T���L�����Z�����s�N���X
        /// </summary>
        private CancellationTokenSource _discoverCancel = new CancellationTokenSource();

        /// <summary>
        /// �z�X�g�Ƃ��ĒʐM���J�n
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        public async UniTask StartHost(string name)
        {
            // �z�X�g�t���O�𗧂Ă�
            IsHost = true;

            // �L�����Z���g�[�N��������
            _discoverCancel = new CancellationTokenSource();

            // �v���C���[���ۑ�
            MyPlayerName = name;

            // �v���C���[���X�g�Ɏ�����ǉ�
            lock (PlayerNames) PlayerNames.Add(name);

            // ��M�J�n
            try
            {
                while (true)
                {
                    // �L�����Z���m�F
                    if (_discoverCancel.IsCancellationRequested) break;

                    // �ő�v���C���[���ɒB���Ă���ꍇ�͎�M���Ȃ�
                    if (_peers.Count >= MAX_CLIENT_NUM) continue;

                    // UdpClient������
                    _udpClient?.Close();
                    _udpClient?.Dispose();
                    _udpClient = new UdpClient(LOCAL_ENDPOINT);
                    _udpClient.EnableBroadcast = true;

                    // UDP��M�ҋ@
                    var receive = await _udpClient.ReceiveAsync();
                    Debug.Log("��M�F" + receive.RemoteEndPoint);

                    // �v���C���[�T���p�P�b�g�ȊO�̏ꍇ�̓X�L�b�v
                    if (UdpPacket.GetUdpHeader(receive.Buffer) != UdpHeader.Discover) continue;

                    // �v���C���[���d���`�F�b�N
                    DiscoverPacket receivePacket = new DiscoverPacket().Parse(receive.Buffer) as DiscoverPacket;
                    if (receivePacket.Name == name || _peers.ContainsKey(receivePacket.Name))
                    {
                        Debug.Log("�v���C���[���d��");

                        // �v���C���[�����d�����Ă���ꍇ�̓G���[�p�P�b�g��Ԃ�
                        byte[] errData = new ErrorPacket(ErrorCode.ExistsName).ConvertToPacket();
                        await _udpClient.SendAsync(errData, errData.Length, receive.RemoteEndPoint);

                        // �ēx��M
                        _udpClient.Close();
                        _udpClient.Dispose();
                        continue;
                    }

                    // --- �N���C�A���g�֕ԐM start
                    
                    // �ڑ��ς݃N���C�A���g�̖��O��IP�A�h���X���\�z
                    Dictionary<string, string> clientAdrs = new Dictionary<string, string>();
                    lock (_peers)
                    {
                        foreach (string key in _peers.Keys)
                        {
                            clientAdrs.Add(key, _peers[key].ep.Address.ToString());
                        }
                    }

                    // �����̖��O�Ɗe�N���C�A���g�̏����i�[���ĕԐM
                    byte[] responseData = new DiscoverResponsePacket(name, clientAdrs).ConvertToPacket();
                    await _udpClient.SendAsync(responseData, responseData.Length, receive.RemoteEndPoint);
                    
                    // --- �N���C�A���g�֕ԐM end

                    // �N���C�A���g�����TCP�ҋ@
                    TcpListener listener = new TcpListener(LOCAL_ENDPOINT);
                    listener.Start();
                    TcpClient tcpClient = null;
                    try
                    {
                        // �^�C���A�E�g�v���p�X�g�b�v�E�H�b�`
                        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

                        // TCP�ҋ@�J�n
                        while (true)
                        {
                            // �^�C���A�E�g�`�F�b�N
                            if (stopwatch.Elapsed.TotalSeconds > 10) break;

                            // �ڑ��v���ҋ@
                            if (listener.Pending())
                            {
                                tcpClient = listener.AcceptTcpClient();

                                // ���M��IP����
                                if (!(tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.Equals(receive.RemoteEndPoint.Address))
                                {
                                    tcpClient.Close();
                                    tcpClient.Dispose();
                                    tcpClient = null;
                                    continue;
                                }

                                // TCP�p�P�b�g�𑗂��Đڑ�������m�点��
                                await tcpClient.GetStream().WriteAsync(new byte[1]);
                                break;
                            }
                            else
                            {
                                // �ڑ��v�����Ȃ��ꍇ�̓L�����Z���`�F�b�N
                                if (_discoverCancel.IsCancellationRequested)
                                {
                                    return;
                                }
                            }

                            // 1�b�Ԋu�Ń`�F�b�N
                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        // ��O���N�����ꍇ�͑z��O�̕s�����̂��ߑS�Đؒf
                        Debug.LogError(ex);
                        Disconnect();
                        break;
                    }
                    finally
                    {
                        listener.Stop();
                    }

                    // �^�C���A�E�g�����ꍇ�͎�M���Ȃ���
                    if (tcpClient == null) continue;

                    // �ڑ���N���C�A���g�ۑ�
                    lock (_peers) _peers.Add(receivePacket.Name, (receive.RemoteEndPoint, tcpClient, false));
                    lock (PlayerNames) PlayerNames.Add(receivePacket.Name);

                    // �ڑ��C�x���g���s
                    OnDiscovery?.Invoke(receivePacket.Name);

                    // �V�K�N���C�A���g�����TCP��M�J�n
                    ReceiveTcp(receivePacket.Name, false);
                }
            }
            catch (SocketException)
            {
                // ��M�L�����Z��
            }
            catch (ObjectDisposedException)
            {
                // ��M�L�����Z��
            }
            catch (AggregateException ex)
            {
                // �\�P�b�g�L�����Z���ȊO�̗�O������ꍇ�͍ăX���[
                foreach (Exception e in ex.InnerExceptions)
                {
                    if (e is not SocketException && e is not ObjectDisposedException)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }
            }
        }

        /// <summary>
        /// �N���C�A���g�Ƃ��ĒʐM���J�n
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        public async UniTask StartClient(string name)
        {
            // �N���C�A���g�t���O�𗧂Ă�
            IsClient = true;

            // �T���L�����Z���g�[�N��������
            _discoverCancel = new CancellationTokenSource();

            // �v���C���[���ۑ�
            MyPlayerName = name;

            // �T���L�����Z�����m�p�^�X�N�����O�ɍ\�z
            Task cancelCheckTask = Task.Run(async () =>
            {
                while (true)
                {
                    // 500�~���b���ƂɃ`�F�b�N
                    if (_discoverCancel.IsCancellationRequested) break;
                    await Task.Delay(500);
                }
            });

            // �z�X�g�T���J�n
            try
            {
                while (true)
                {
                    // �L�����Z���m�F
                    if (_discoverCancel.IsCancellationRequested) break;

                    // UdpClient������
                    _udpClient = new UdpClient(LOCAL_ENDPOINT);

                    // �u���[�h�L���X�g�L����
                    _udpClient.EnableBroadcast = true;

                    // �u���[�h�L���X�g�ŒT���p�P�b�g���M
                    byte[] data = new DiscoverPacket(name).ConvertToPacket();
                    await _udpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, PORT));

                    // �ԐM�ҋ@
                    Task responseTimeoutTask = Task.Delay(10 * 1000);
                    var receiveTask = _udpClient.ReceiveAsync();
                    while (true)
                    {
                        if (await Task.WhenAny(receiveTask, responseTimeoutTask) == receiveTask)
                        {
                            // �������������u���[�h�L���X�g�̏ꍇ�͖���
                            if (GetLocalIPAddresses().Contains(receiveTask.Result.RemoteEndPoint.Address.ToString()))
                            {
                                receiveTask = _udpClient.ReceiveAsync();
                                continue;
                            }
                            break;
                        }
                        else
                        {
                            // �ԐM�҂��^�C���A�E�g
                            receiveTask = null;
                            break;
                        }
                    }

                    // �^�C���A�E�g�`�F�b�N
                    if (receiveTask == null)
                    {
                        _udpClient.Close();
                        _udpClient.Dispose();
                        continue;
                    }

                    // ��M�f�[�^�擾
                    var receive = receiveTask.Result;
                    Debug.Log("��M�F" + receive.RemoteEndPoint);

                    // �w�b�_�擾
                    UdpHeader header = UdpPacket.GetUdpHeader(receive.Buffer);

                    // �G���[�`�F�b�N
                    if (header == UdpHeader.Error)
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
                            ex = new NetworkException(ExceptionError.UnexpectedError, "�z��O�̃G���[���������܂����B");
                        }

                        // �\�P�b�g����ė�O�X���[
                        _udpClient.Close();
                        _udpClient.Dispose();
                        throw ex;
                    }

                    // �����p�P�b�g�ȊO�̏ꍇ�̓X�L�b�v
                    if (header != UdpHeader.DiscoverResponse) continue;

                    // �����p�P�b�g���
                    DiscoverResponsePacket responsePacket = new DiscoverResponsePacket().Parse(receive.Buffer) as DiscoverResponsePacket;

                    // --- �z�X�g��TCP�R�l�N�V������\�� start

                    // TCP�ڑ��^�X�N
                    TcpClient tcpClient = new TcpClient();
                    Task connTask = tcpClient.ConnectAsync(receive.RemoteEndPoint.Address, PORT);

                    // �^�C���A�E�g�p�^�X�N
                    Task connTimeoutTask = Task.Delay(10 * 1000);

                    // �^�X�N�I���ҋ@
                    if (await Task.WhenAny(connTask, connTimeoutTask, cancelCheckTask) != connTask)
                    {
                        // �^�C���A�E�g
                        tcpClient.Close();
                        tcpClient.Dispose();
                        _udpClient?.Close();
                        _udpClient?.Dispose();
                        continue;
                    }

                    // --- �z�X�g��TCP�R�l�N�V������\�� end

                    // --- TCP�ڑ������`�F�b�N�p��TCP�p�P�b�g�ҋ@ start

                    // TCP��M�p�^�X�N
                    byte[] tcpBuf = new byte[1];
                    CancellationTokenSource tcpCancel = new CancellationTokenSource();
                    Task<int> tcpTask = tcpClient.GetStream().ReadAsync(tcpBuf, 0, tcpBuf.Length, tcpCancel.Token);

                    // �^�C���A�E�g�p�^�X�N
                    Task tcpTimeoutTask = Task.Delay(30 * 1000);

                    // �^�X�N�I���ҋ@
                    if (await Task.WhenAny(tcpTask, tcpTimeoutTask, cancelCheckTask) != tcpTask)
                    {
                        // TCP��M�ȊO�̃^�X�N���I������ꍇ�̓L�����Z������
                        tcpCancel.Cancel();
                        tcpCancel.Dispose();
                        tcpClient.Close();
                        tcpClient.Dispose();
                        _udpClient?.Close();
                        _udpClient?.Dispose();
                        continue;
                    }

                    // �ؒf���ꂽ�ꍇ�͕s�������N���Ă���̂ōŏ������蒼��
                    if (tcpTask.Result == 0)
                    {
                        tcpCancel.Dispose();
                        tcpClient.Close();
                        tcpClient.Dispose();
                        _udpClient?.Close();
                        _udpClient?.Dispose();
                        continue;
                    }

                    // --- TCP�ڑ������`�F�b�N�p��TCP�p�P�b�g�ҋ@ end

                    // �ڑ���ꗗ�Ƀz�X�g��ǉ�
                    lock (_peers) _peers.Add(responsePacket.HostName, (receive.RemoteEndPoint, tcpClient, true));
                    lock (PlayerNames) PlayerNames.Add(responsePacket.HostName);

                    // �z�X�g�����TCP��M���J�n����
                    ReceiveTcp(responsePacket.HostName, true);

                    // ���v���C���[�Ƃ��ڑ�
                    try
                    {
                        foreach (string key in responsePacket.ClientAddresses.Keys)
                        {
                            // �R�l�N�V������\��
                            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(responsePacket.ClientAddresses[key]), PORT);
                            TcpClient client = new TcpClient(ep);

                            // �v���C���[�����M
                            byte[] connectData = new PeerConnectPacket(name).ConvertToPacket();
                            await client.GetStream().WriteAsync(connectData, 0, connectData.Length);

                            // �ڑ���ꗗ�ɒǉ�
                            lock (_peers) _peers.Add(key, (ep, client, false));

                            // �v���C���[�����TCP��M�J�n
                            ReceiveTcp(key, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // ��O���N�����ꍇ�͑z��O�̕s�����̂��ߑS�Đؒf
                        Debug.LogError(ex);
                        Disconnect();
                        break;
                    }

                    // �ڑ��ς݃v���C���[�̌㑱�Ɏ�����ǉ�
                    lock (PlayerNames) PlayerNames.Add(name);

                    // �ڑ��C�x���g���s
                    OnDiscovery?.Invoke(responsePacket.HostName);

                    // Udp��M�J�n
                    ReceiveUdp();

                    // �V�K�v���C���[����̐ڑ��ҋ@
                    TcpListener listener = new TcpListener(LOCAL_ENDPOINT);
                    listener.Start();
                    try
                    {
                        while (true)
                        {
                            // �ڑ��v���ҋ@
                            if (listener.Pending())
                            {
                                TcpClient client = listener.AcceptTcpClient();

                                // ���O��M
                                byte[] connectBuf = new byte[1024];
                                await tcpClient.GetStream().ReadAsync(connectBuf);

                                // �w�b�_�[�`�F�b�N
                                if (TcpPacket.GetTcpHeader(connectBuf) != TcpHeader.PeerConnect)
                                {
                                    client.Close();
                                    client.Dispose();
                                    continue;
                                }

                                // �p�P�b�g���
                                PeerConnectPacket connectPacket = new PeerConnectPacket().Parse(connectBuf) as PeerConnectPacket;

                                // �ڑ���ꗗ�ɒǉ�
                                lock (_peers) _peers.Add(connectPacket.Name, (client.Client.RemoteEndPoint as IPEndPoint, tcpClient, false));
                                lock (PlayerNames) PlayerNames.Add(connectPacket.Name);

                                // �V�K�v���C���[�����TCP��M�J�n
                                ReceiveTcp(connectPacket.Name, false);
                            }
                            else
                            {
                                // �ڑ��v�����Ȃ��ꍇ�̓L�����Z���`�F�b�N
                                if (_discoverCancel.IsCancellationRequested)
                                {
                                    break;
                                }
                            }

                            // 1�b�Ԋu�Ń`�F�b�N
                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        // ��O���N�����ꍇ�͑z��O�̕s�����̂��ߑS�Đؒf
                        Debug.LogError(ex);
                        Disconnect();
                    }
                    finally
                    {
                        listener.Stop();
                    }

                    break;
                }
            }
            catch (SocketException)
            {
                // ��M�L�����Z��
            }
            catch (ObjectDisposedException)
            {
                // ��M�L�����Z��
            }
            catch (AggregateException ex)
            {
                // �\�P�b�g�L�����Z���ȊO�̗�O������ꍇ�͍ăX���[
                foreach (Exception e in ex.InnerExceptions)
                {
                    if (e is not SocketException && e is not ObjectDisposedException)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }
            }
        }

        /// <summary>
        /// �ʐM�ؒf
        /// </summary>
        public void Disconnect()
        {
            // �z�X�g�E�N���C�A���g�t���O������
            IsHost = false;
            IsClient = false;

            // �T����~
            _discoverCancel.Cancel();

            // Udp��~
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;

            // �S�Ẵv���C���[�Ɛؒf
            lock (_peers)
            {
                foreach (string key in _peers.Keys)
                {
                    _peers[key].tcp.Close();
                    _peers[key].tcp.Dispose();
                }
                _peers.Clear();

                lock (PlayerNames) PlayerNames.Clear();
            }
        }

        /// <summary>
        /// �ʐM����̒T�����~
        /// </summary>
        public void StopDiscovery()
        {
            _discoverCancel.Cancel();
            _udpClient.Close();
            _udpClient.Dispose();
            _udpClient = new UdpClient(LOCAL_ENDPOINT);

            // ��M�J�n
            ReceiveUdp();
        }

        /// <summary>
        /// �z�X�g�փp�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        public void SendToHost(IPacket packet)
        {
            if (IsHost) return;

            byte[] data = packet.ConvertToPacket();
            lock (_peers)
            {
                foreach (string key in _peers.Keys)
                {
                    if (_peers[key].isHost)
                    {
                        _udpClient.Send(data, data.Length, _peers[key].ep);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// �S�Ă̒ʐM����փp�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        public void SendToAll(IPacket packet)
        {
            byte[] data = packet.ConvertToPacket();
            lock (_peers)
            {
                Parallel.ForEach(_peers.Keys, key =>
                {
                    _udpClient.Send(data, data.Length, _peers[key].ep);
                });
            }
        }

        private void Awake()
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// �w�肵���v���C���[�����TCP��M���J�n����
        /// </summary>
        /// <param name="player">TCP�̎�M��v���C���[</param>
        /// <param name="isHost">��M�悪�z�X�g�ł��邩</param>
        private void ReceiveTcp(string player, bool isHost)
        {
            TcpClient client = _peers[player].tcp;
            UniTask.Void(async () =>
            {
                while (true)
                {
                    // Tcp��M�ҋ@
                    byte[] buf = new byte[2048];
                    int size = 0;
                    try
                    {
                        size = await client.GetStream().ReadAsync(buf, 0, buf.Length);
                    }
                    catch (SocketException)
                    {
                        // �ؒf
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // �ؒf
                        break;
                    }
                    catch (IOException)
                    {
                        // �ؒf
                        break;
                    }

                    // �ؒf�`�F�b�N
                    if (size == 0)
                    {
                        lock (_peers)
                        {
                            if (_peers.ContainsKey(player))
                            {
                                client.Close();
                                client.Dispose();
                                _peers.Remove(player);
                                lock (PlayerNames) PlayerNames.Remove(player);
                            }
                        }
                        OnDisconnect?.Invoke(player, isHost);

                        // �z�X�g�̏ꍇ�͑S�ẴN���C�A���g�Ɛؒf
                        if (isHost)
                        {
                            lock (_peers)
                            {
                                foreach (string key in _peers.Keys)
                                {
                                    _peers[key].tcp.Close();
                                    _peers[key].tcp.Dispose();
                                    OnDisconnect?.Invoke(key, false);
                                }
                                _peers.Clear();
                                lock (PlayerNames) PlayerNames.Clear();
                            }
                        }
                        break;
                    }

                    // �^���擾
                    Type type = TcpPacket.GetTcpType(buf);

                    // �^������ɃR���X�g���N�^�����擾
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    var expression = Expression.Lambda<Func<IPacket>>(Expression.New(constructor)).Compile();
                    // �R���X�g���N�^���s
                    IPacket packet = expression();

                    // �C�x���g����
                    OnTcpReceive?.Invoke(player, TcpPacket.GetTcpHeader(buf), packet.Parse(buf) as TcpPacket);
                }
            });
        }

        /// <summary>
        /// UDP��M���J�n����
        /// </summary>
        private void ReceiveUdp()
        {
            UniTask.Void(async () =>
            {
                while (true)
                {
                    if (_udpClient == null) break;

                    byte[] buf = null;
                    string sendPlayer = string.Empty;
                    try
                    {
                        // �p�P�b�g��M
                        var receive = await _udpClient.ReceiveAsync();
                        buf = receive.Buffer;

                        // ���M���v���C���[���擾
                        foreach (string key in _peers.Keys)
                        {
                            if (_peers[key].ep.Equals(receive.RemoteEndPoint))
                            {
                                sendPlayer = key;
                                break;
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        // �ؒf
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // �ؒf
                        break;
                    }

                    // �^���擾
                    Type type = UdpPacket.GetUdpType(buf);

                    // �^������ɃR���X�g���N�^�����擾
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    var expression = Expression.Lambda<Func<IPacket>>(Expression.New(constructor)).Compile();
                    // �R���X�g���N�^���s
                    IPacket packet = expression();

                    // �C�x���g����
                    OnUdpReceive?.Invoke(sendPlayer, UdpPacket.GetUdpHeader(buf), packet.Parse(buf) as UdpPacket);
                }
            });
        }

        /// <summary>
        /// ���[�J��IP�A�h���X�ꗗ��Ԃ�
        /// </summary>
        /// <returns></returns>
        private List<string> GetLocalIPAddresses()
        {
            List<string> addresses = new List<string>();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        addresses.Add(unicastAddress.Address.ToString());
                    }
                }
            }
           
            return addresses;
        }
    }
}