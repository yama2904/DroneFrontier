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
        /// 使用するポート番号
        /// </summary>
        private const int PORT = 5556;

        /// <summary>
        /// 最大クライアント数
        /// </summary>
        private const int MAX_CLIENT_NUM = 3;

        /// <summary>
        /// ホスト/クライアント
        /// </summary>
        public static PeerType PeerType { get; private set; } = PeerType.None;

        /// <summary>
        /// 自分のプレイヤー名
        /// </summary>
        public static string MyPlayerName { get; private set; } = string.Empty;

        /// <summary>
        /// 各プレイヤー名
        /// </summary>
        public static List<string> PlayerNames { get; private set; } = new List<string>();

        /// <summary>
        /// プレイヤー数
        /// </summary>
        public static int PlayerCount => PlayerNames.Count;

        #region イベントハンドラー

        /// <summary>
        /// プレイヤーコネクションイベントハンドラー
        /// </summary>
        /// <param name="name">コネクション先プレイヤー名</param>
        /// <param name="isHost">コネクション先プレイヤーのホスト/クライアント種別</param>
        public delegate void ConnectionHandler(string name, PeerType type);

        /// <summary>
        /// パケット受信イベントハンドラー
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="packet">受信したパケット</param>
        public delegate void ReceiveHandler(string name, BasePacket packet);

        #endregion

        #region イベント

        /// <summary>
        /// 通信接続イベント
        /// </summary>
        public static event ConnectionHandler OnConnected;

        /// <summary>
        /// プレイヤー切断イベント
        /// </summary>
        public static event ConnectionHandler OnDisconnected;

        /// <summary>
        /// 通信相手探索完了イベント
        /// </summary>
        public static event EventHandler OnDiscoveryCompleted;

        /// <summary>
        /// TCPパケット受信イベント
        /// </summary>
        public static event ReceiveHandler OnTcpReceived;

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        public static event ReceiveHandler OnUdpReceived;

        /// <summary>
        /// UDPパケット受信イベント（メインスレッド上で実行）
        /// </summary>
        public static event ReceiveHandler OnUdpReceivedOnMainThread;

        #endregion

        /// <summary>
        /// ホスト用P2P受付クラス
        /// </summary>
        private static PeerListener _listener = null;

        /// <summary>
        /// P2P接続先リスト
        /// </summary>
        private static List<PeerClient> _peerClients = new List<PeerClient>();

        /// <summary>
        /// 通信切断時発火キャンセル
        /// </summary>
        private static CancellationTokenSource _disconnectCancel = new CancellationTokenSource();

        [SerializeField]
        private NetworkObjectSpawner _spawner;

        [SerializeField]
        private NetworkDelayMonitor _delayMonitor;

        /// <summary>
        /// 初期化
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
        /// ホストとして通信を開始してクライアントからの接続を待機する。<br/>
        /// 複数のゲームモードを扱う場合はgameModeを指定することで、同一のゲームモードからのみ接続を許可する。
        /// </summary>
        /// <param name="name">自分のプレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
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
        /// クライアントとして通信を開始して他プレイヤーの探索を行う。<br/>
        /// 複数のゲームモードを扱う場合はgameModeを指定することで、同一のゲームモードのプレイヤーを探索する。
        /// </summary>
        /// <param name="name">自分のプレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
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
        /// 通信相手の探索を完了してゲームを開始（ホストのみ有効）
        /// </summary>
        public static void StartGame()
        {
            if (PeerType != PeerType.Host) return;
            _listener.CompleteAppect();
        }

        /// <summary>
        /// 通信切断
        /// </summary>
        public static void Disconnect()
        {
            _disconnectCancel.Cancel();

            // ホスト/クライアント種別初期化
            PeerType = PeerType.None;

            // 切断
            foreach (PeerClient client in _peerClients)
            {
                client.OnTcpReceived -= OnTcpReceivedPeer;
                client.OnUdpReceived -= OnUdpReceivedPeer;
                client.OnUdpReceivedOnMainThread -= OnUdpReceivedPeerOnMainThread;
                client.OnDisconnected -= OnDisconnectedPeer;
                client.Disconnect();
            }
            _peerClients.Clear();

            // スポナー停止
            NetworkObjectSpawner.Stop();

            // 遅延モニター停止
            NetworkDelayMonitor.Stop();
        }

        /// <summary>
        /// ホストへUDPパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
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
        /// ホストへTCPパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
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
        /// 指定したプレイヤーへTCPパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
        /// <param name="player">送信先プレイヤー名</param>
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
        /// 指定したプレイヤーへUDPパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
        /// <param name="player">送信先プレイヤー名</param>
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
        /// 全ての通信相手へUDPパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
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
        /// 全ての通信相手へTCPパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
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

            // スポナー生成
            var spawner = Instantiate(_spawner);
            DontDestroyOnLoad(spawner.gameObject);

            // 遅延モニター生成
            var monitor = Instantiate(_delayMonitor);
            DontDestroyOnLoad(monitor.gameObject);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        /// <summary>
        /// ホスト発見イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="clients">ホスト発見時点の各プレイヤーとのP2P接続クライアント</param>
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
        /// プレイヤー接続イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="client">接続したクライアント</param>
        private static void OnConnectedPeer(object sender, PeerClient client)
        {
            // 接続情報保存
            PlayerNames.Add(client.RemoteName);
            _peerClients.Add(client);

            // 切断イベント設定
            client.OnDisconnected += OnDisconnectedPeer;

            // 接続イベント発火
            OnConnected?.Invoke(client.RemoteName, client.RemoteType);
        }

        /// <summary>
        /// プレイヤー切断イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private static void OnDisconnectedPeer(object sender, EventArgs e)
        {
            PeerClient client = sender as PeerClient;

            // イベント削除
            client.OnTcpReceived -= OnTcpReceivedPeer;
            client.OnUdpReceived -= OnUdpReceivedPeer;
            client.OnUdpReceivedOnMainThread -= OnUdpReceivedPeerOnMainThread;
            client.OnDisconnected -= OnDisconnectedPeer;

            // 切断プレイヤーを一覧から削除
            PlayerNames.Remove(client.RemoteName);
            _peerClients.Remove(client);

            // 切断イベント発火
            OnDisconnected?.Invoke(client.RemoteName, client.RemoteType);
        }

        /// <summary>
        /// プレイヤー探索完了イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private static void OnDiscoveryCompletedPeer(object sender, EventArgs e)
        {
            // 受信開始
            foreach (PeerClient client in _peerClients)
            {
                client.OnTcpReceived += OnTcpReceivedPeer;
                client.OnUdpReceived += OnUdpReceivedPeer;
                client.OnUdpReceivedOnMainThread += OnUdpReceivedPeerOnMainThread;
            }

            // スポナー開始
            NetworkObjectSpawner.Run();

            // 遅延監視を開始
            NetworkDelayMonitor.Run();

            // 完了イベント発火
            OnDiscoveryCompleted?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// TCPパケット受信イベント
        /// </summary>
        /// <param name="client">イベントオブジェクト</param>
        /// <param name="packet">受信パケット</param>
        private static void OnTcpReceivedPeer(PeerClient client, BasePacket packet)
        {
            OnTcpReceived?.Invoke(client.RemoteName, packet);
        }

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        /// <param name="client">イベントオブジェクト</param>
        /// <param name="packet">受信パケット</param>
        private static void OnUdpReceivedPeer(PeerClient client, BasePacket packet)
        {
            OnUdpReceived?.Invoke(client.RemoteName, packet);
        }

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        /// <param name="client">イベントオブジェクト</param>
        /// <param name="packet">受信パケット</param>
        private static void OnUdpReceivedPeerOnMainThread(PeerClient client, BasePacket packet)
        {
            OnUdpReceivedOnMainThread?.Invoke(client.RemoteName, packet);
        }
    }
}