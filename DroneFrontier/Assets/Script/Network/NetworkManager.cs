using Cysharp.Threading.Tasks;
using Network.Connect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Network
{
    public class NetworkManager : MonoBehaviour
    {
        /// <summary>
        /// �g�p����|�[�g�ԍ�
        /// </summary>
        private const int PORT = 5556;

        /// <summary>
        /// �ő�N���C�A���g��
        /// </summary>
        private const int MAX_CLIENT_NUM = 3;

        /// <summary>
        /// �z�X�g/�N���C�A���g
        /// </summary>
        public static PeerType PeerType { get; private set; } = PeerType.None;

        /// <summary>
        /// �����̃v���C���[��
        /// </summary>
        public static string MyPlayerName { get; private set; } = string.Empty;

        /// <summary>
        /// �e�v���C���[��
        /// </summary>
        public static List<string> PlayerNames { get; private set; } = new List<string>();

        /// <summary>
        /// �v���C���[��
        /// </summary>
        public static int PlayerCount => PlayerNames.Count;

        #region �C�x���g�n���h���[

        /// <summary>
        /// �v���C���[�R�l�N�V�����C�x���g�n���h���[
        /// </summary>
        /// <param name="name">�R�l�N�V������v���C���[��</param>
        /// <param name="isHost">�R�l�N�V������v���C���[�̃z�X�g/�N���C�A���g���</param>
        public delegate void ConnectionHandler(string name, PeerType type);

        /// <summary>
        /// �p�P�b�g��M�C�x���g�n���h���[
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="packet">��M�����p�P�b�g</param>
        public delegate void ReceiveHandler(string name, BasePacket packet);

        #endregion

        #region �C�x���g

        /// <summary>
        /// �ʐM�ڑ��C�x���g
        /// </summary>
        public static event ConnectionHandler OnConnected;

        /// <summary>
        /// �v���C���[�ؒf�C�x���g
        /// </summary>
        public static event ConnectionHandler OnDisconnected;

        /// <summary>
        /// �ʐM����T�������C�x���g
        /// </summary>
        public static event EventHandler OnDiscoveryCompleted;

        /// <summary>
        /// TCP�p�P�b�g��M�C�x���g
        /// </summary>
        public static event ReceiveHandler OnTcpReceived;

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g
        /// </summary>
        public static event ReceiveHandler OnUdpReceived;

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g�i���C���X���b�h��Ŏ��s�j
        /// </summary>
        public static event ReceiveHandler OnUdpReceivedOnMainThread;

        #endregion

        /// <summary>
        /// �z�X�g�pP2P��t�N���X
        /// </summary>
        private static PeerListener _listener = null;

        /// <summary>
        /// P2P�ڑ��惊�X�g
        /// </summary>
        private static List<PeerClient> _peerClients = new List<PeerClient>();

        /// <summary>
        /// �ʐM�ؒf�����΃L�����Z��
        /// </summary>
        private static CancellationTokenSource _disconnectCancel = new CancellationTokenSource();

        [SerializeField]
        private NetworkObjectSpawner _spawner;

        [SerializeField]
        private NetworkDelayMonitor _delayMonitor;

        /// <summary>
        /// ������
        /// </summary>
        public static void Initialize()
        {
            Disconnect();

            MyPlayerName = string.Empty;
            PlayerNames.Clear();
            PeerType = PeerType.None;
            _disconnectCancel = new CancellationTokenSource();
        }

        /// <summary>
        /// �z�X�g�Ƃ��ĒʐM���J�n���ăN���C�A���g����̐ڑ���ҋ@����B<br/>
        /// �����̃Q�[�����[�h�������ꍇ��gameMode���w�肷�邱�ƂŁA����̃Q�[�����[�h����̂ݐڑ���������B
        /// </summary>
        /// <param name="name">�����̃v���C���[��</param>
        /// <param name="gameMode">�Q�[�����[�h</param>
        public static async UniTask StartAccept(string name, string gameMode = "")
        {
            Initialize();
            MyPlayerName = name;
            PlayerNames.Add(name);
            PeerType = PeerType.Host;

            try
            {
                _listener = new PeerListener(PORT, MAX_CLIENT_NUM);
                _listener.OnConnected += OnConnectedPeer;
                _listener.OnAcceptCompleted += OnDiscoveryCompletedPeer;
                
                await _listener.StartAccept(name, gameMode, _disconnectCancel.Token);
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                if (_listener != null)
                {
                    _listener.OnConnected -= OnConnectedPeer;
                    _listener.OnAcceptCompleted -= OnDiscoveryCompletedPeer;
                }
                _listener = null;
            }
        }

        /// <summary>
        /// �N���C�A���g�Ƃ��ĒʐM���J�n���đ��v���C���[�̒T�����s���B<br/>
        /// �����̃Q�[�����[�h�������ꍇ��gameMode���w�肷�邱�ƂŁA����̃Q�[�����[�h�̃v���C���[��T������B
        /// </summary>
        /// <param name="name">�����̃v���C���[��</param>
        /// <param name="gameMode">�Q�[�����[�h</param>
        public static async UniTask StartDiscovery(string name, string gameMode = "")
        {
            Initialize();
            MyPlayerName = name;
            PeerType = PeerType.Client;

            PeerDiscover discover = null;
            try
            {
                discover = new PeerDiscover(PORT);
                discover.OnHostDiscovered += OnHostDiscovered;
                discover.OnConnected += OnConnectedPeer;
                discover.OnDiscoveryCompleted += OnDiscoveryCompletedPeer;

                await discover.StartDiscovery(name, gameMode, _disconnectCancel.Token);
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                if (discover != null)
                {
                    discover.OnHostDiscovered -= OnHostDiscovered;
                    discover.OnConnected -= OnConnectedPeer;
                    discover.OnDiscoveryCompleted -= OnDiscoveryCompletedPeer;
                }
                discover = null;
            }
        }

        /// <summary>
        /// �ʐM����̒T�����������ăQ�[�����J�n�i�z�X�g�̂ݗL���j
        /// </summary>
        public static void StartGame()
        {
            if (PeerType != PeerType.Host) return;
            _listener.CompleteAppect();
        }

        /// <summary>
        /// �ʐM�ؒf
        /// </summary>
        public static void Disconnect()
        {
            _disconnectCancel.Cancel();

            // �z�X�g/�N���C�A���g��ʏ�����
            PeerType = PeerType.None;

            // �ؒf
            foreach (PeerClient client in _peerClients)
            {
                client.OnTcpReceived -= OnTcpReceivedPeer;
                client.OnUdpReceived -= OnUdpReceivedPeer;
                client.OnUdpReceivedOnMainThread -= OnUdpReceivedPeerOnMainThread;
                client.OnDisconnected -= OnDisconnectedPeer;
                client.Disconnect();
            }
            _peerClients.Clear();

            // �X�|�i�[��~
            NetworkObjectSpawner.Stop();

            // �x�����j�^�[��~
            NetworkDelayMonitor.Stop();
        }

        /// <summary>
        /// �z�X�g��UDP�p�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        public static void SendUdpToHost(BasePacket packet)
        {
            if (PeerType == PeerType.Host) return;

            UniTask.Void(async () =>
            {
                foreach (PeerClient client in _peerClients)
                {
                    if (client.RemoteType != PeerType.Host) continue;
                    client.SendUdp(packet);
                }

                await UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// �z�X�g��TCP�p�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        public static void SendTcpToHost(BasePacket packet)
        {
            if (PeerType == PeerType.Host) return;

            UniTask.Void(async () =>
            {
                foreach (PeerClient client in _peerClients)
                {
                    if (client.RemoteType != PeerType.Host) continue;
                    client.SendTcp(packet);
                }

                await UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// �w�肵���v���C���[��TCP�p�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        /// <param name="player">���M��v���C���[��</param>
        public static void SendTcpToPlayer(BasePacket packet, string player)
        {
            UniTask.Void(async () =>
            {
                foreach (PeerClient client in _peerClients)
                {
                    if (client.RemoteName != player) continue;
                    client.SendTcp(packet);
                }

                await UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// �w�肵���v���C���[��UDP�p�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        /// <param name="player">���M��v���C���[��</param>
        public static void SendUdpToPlayer(BasePacket packet, string player)
        {
            UniTask.Void(async () =>
            {
                foreach (PeerClient client in _peerClients)
                {
                    if (client.RemoteName != player) continue;
                    client.SendUdp(packet);
                }

                await UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// �S�Ă̒ʐM�����UDP�p�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        public static void SendUdpToAll(BasePacket packet)
        {
            UniTask.Void(async () =>
            {
                foreach (PeerClient client in _peerClients)
                {
                    client.SendUdp(packet);
                }

                await UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// �S�Ă̒ʐM�����TCP�p�P�b�g�𑗐M����
        /// </summary>
        /// <param name="packet">���M�p�P�b�g</param>
        public static void SendTcpToAll(BasePacket packet)
        {
            UniTask.Void(async () =>
            {
                foreach (PeerClient client in _peerClients)
                {
                    client.SendTcp(packet);
                }

                await UniTask.CompletedTask;
            });
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // �X�|�i�[����
            var spawner = Instantiate(_spawner);
            DontDestroyOnLoad(spawner.gameObject);

            // �x�����j�^�[����
            var monitor = Instantiate(_delayMonitor);
            DontDestroyOnLoad(monitor.gameObject);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        /// <summary>
        /// �z�X�g�����C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="clients">�z�X�g�������_�̊e�v���C���[�Ƃ�P2P�ڑ��N���C�A���g</param>
        private static void OnHostDiscovered(PeerDiscover sender, List<PeerClient> clients)
        {
            PlayerNames.AddRange(clients.Select(x => x.RemoteName).Concat(new string[] { MyPlayerName }));
            _peerClients = clients;

            foreach (PeerClient client in _peerClients)
            {
                client.OnDisconnected += OnDisconnectedPeer;
                OnConnected?.Invoke(client.RemoteName, client.RemoteType);
            }
        }

        /// <summary>
        /// �v���C���[�ڑ��C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="client">�ڑ������N���C�A���g</param>
        private static void OnConnectedPeer(object sender, PeerClient client)
        {
            // �ڑ����ۑ�
            PlayerNames.Add(client.RemoteName);
            _peerClients.Add(client);

            // �ؒf�C�x���g�ݒ�
            client.OnDisconnected += OnDisconnectedPeer;

            // �ڑ��C�x���g����
            OnConnected?.Invoke(client.RemoteName, client.RemoteType);
        }

        /// <summary>
        /// �v���C���[�ؒf�C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private static void OnDisconnectedPeer(object sender, EventArgs e)
        {
            PeerClient client = sender as PeerClient;

            // �C�x���g�폜
            client.OnTcpReceived -= OnTcpReceivedPeer;
            client.OnUdpReceived -= OnUdpReceivedPeer;
            client.OnUdpReceivedOnMainThread -= OnUdpReceivedPeerOnMainThread;
            client.OnDisconnected -= OnDisconnectedPeer;

            // �ؒf�v���C���[���ꗗ����폜
            PlayerNames.Remove(client.RemoteName);
            _peerClients.Remove(client);

            // �ؒf�C�x���g����
            OnDisconnected?.Invoke(client.RemoteName, client.RemoteType);
        }

        /// <summary>
        /// �v���C���[�T�������C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private static void OnDiscoveryCompletedPeer(object sender, EventArgs e)
        {
            // ��M�J�n
            foreach (PeerClient client in _peerClients)
            {
                client.OnTcpReceived += OnTcpReceivedPeer;
                client.OnUdpReceived += OnUdpReceivedPeer;
                client.OnUdpReceivedOnMainThread += OnUdpReceivedPeerOnMainThread;
            }

            // �X�|�i�[�J�n
            NetworkObjectSpawner.Run();

            // �x���Ď����J�n
            NetworkDelayMonitor.Run();

            // �����C�x���g����
            OnDiscoveryCompleted?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// TCP�p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="client">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="packet">��M�p�P�b�g</param>
        private static void OnTcpReceivedPeer(PeerClient client, BasePacket packet)
        {
            OnTcpReceived?.Invoke(client.RemoteName, packet);
        }

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="client">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="packet">��M�p�P�b�g</param>
        private static void OnUdpReceivedPeer(PeerClient client, BasePacket packet)
        {
            OnUdpReceived?.Invoke(client.RemoteName, packet);
        }

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="client">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="packet">��M�p�P�b�g</param>
        private static void OnUdpReceivedPeerOnMainThread(PeerClient client, BasePacket packet)
        {
            OnUdpReceivedOnMainThread?.Invoke(client.RemoteName, packet);
        }
    }
}