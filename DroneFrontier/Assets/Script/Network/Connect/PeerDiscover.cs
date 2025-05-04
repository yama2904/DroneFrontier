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
    /// P2Pホスト探索クラス
    /// </summary>
    public class PeerDiscover
    {
        /// <summary>
        /// ホスト発見イベントハンドラー
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="clients">ホスト発見時点の各プレイヤーとのP2P接続クライアント</param>
        public delegate void HostDiscoveredHandler(PeerDiscover sender, List<PeerClient> clients);

        /// <summary>
        /// ホスト発見イベント
        /// </summary>
        public event HostDiscoveredHandler OnHostDiscovered;

        /// <summary>
        /// 通信接続イベント
        /// </summary>
        public event ConnectionHandler OnConnected;

        /// <summary>
        /// 探索完了イベント
        /// </summary>
        public event EventHandler OnDiscoveryCompleted;

        /// <summary>
        /// 送受信ポート番号
        /// </summary>
        private readonly int PORT;

        /// <summary>
        /// 接続済みクライアント
        /// </summary>
        private List<PeerClient> _connectedClients = new List<PeerClient>();

        /// <summary>
        /// ホストから接続完了時に発行するキャンセル
        /// </summary>
        private CancellationTokenSource _completedCancel = new CancellationTokenSource();

        /// <summary>
        /// ホスト切断時に発行するキャンセル
        /// </summary>
        private CancellationTokenSource _hostDisconnectCancel = new CancellationTokenSource();

        private TcpListener _listener = null;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="port">送受信ポート番号</param>
        public PeerDiscover(int port)
        {
            PORT = port;
        }

        /// <summary>
        /// ホストの探索を開始<br/>
        /// キャンセルを発行した場合は接続済みの全てのホスト/クライアントと切断する
        /// </summary>
        /// <param name="name">自分のプレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="token">キャンセルトークン</param>
        /// <exception cref="TaskCanceledException"></exception>
        public async UniTask StartDiscovery(string name, string gameMode, CancellationToken token)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, 0);
                _listener.Start();

                // ホストを探索して接続確立
                await DiscoverHost(name, gameMode, token);

                // 新規プレイヤーからの接続受付
                await AcceptFromClient(name, token);
            }
            catch (Exception ex)
            {
                // キャンセルの場合
                if (ex is TaskCanceledException)
                {
                    // 呼び出し元からキャンセルされた場合は切断
                    if (token.IsCancellationRequested)
                    {
                        DisconnectAll();
                        throw;
                    }

                    // ホスト切断によるキャンセルの場合は全プレイヤーと切断してエラー
                    if (_hostDisconnectCancel.IsCancellationRequested)
                    {
                        DisconnectAll();
                        throw new NetworkException(ExceptionError.UnexpectedError, "ホストから切断されました。");
                    }

                    // 接続完了によるキャンセルは正常終了
                }
                else if (ex is NetworkException)
                {
                    // NetworkExceptionの場合は切断して再スロー
                    DisconnectAll();
                    throw;
                }
                else
                {
                    // NetworkException以外の場合は想定外エラーのため切断してエラーを吐く
                    Debug.Log(ex);
                    DisconnectAll();
                    throw new NetworkException(ExceptionError.UnexpectedError, "想定外のエラーが発生しました。");
                }
            }
            finally
            {
                _listener?.Stop();
                _listener = null;
            }

            // 接続完了イベント発火
            OnDiscoveryCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ホストを探索して接続確立
        /// </summary>
        /// <param name="name">自分のプレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="token">キャンセルトークン</param>
        /// <exception cref="NetworkException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        private async UniTask DiscoverHost(string name, string gameMode, CancellationToken token)
        {
            // キャンセル発行検知用タスク
            Task cancelTask = Task.Run(() =>
            {
                token.WaitHandle.WaitOne();
            });

            // TCPリッスンポート
            int listenPort = (_listener.LocalEndpoint as IPEndPoint).Port;

            // 探索開始
            while (true)
            {
                // UDPクライアント初期化
                UdpClient hostUdp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                hostUdp.EnableBroadcast = true;

                // ブロードキャストで探索パケット送信
                byte[] data = new DiscoverPacket(name, gameMode, listenPort).ConvertToPacket();
                await hostUdp.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, PORT));
                hostUdp.EnableBroadcast = false;

                // 返信待機
                UdpReceiveResult receive = new UdpReceiveResult();
                try
                {
                    receive = await ConnectUtil.ReceiveUdpAsync(hostUdp, 10, token);
                }
                catch (Exception ex)
                {
                    hostUdp.Close();
                    hostUdp.Dispose();

                    // タイムアウトした場合は再度探索パケット送信
                    if (ex is TimeoutException) continue;

                    throw;
                }
                Debug.Log("受信：" + receive.RemoteEndPoint);

                // エラーチェック
                if (BasePacket.GetPacketType(receive.Buffer) == typeof(ErrorPacket))
                {
                    // エラーパケット解析
                    ErrorPacket errPacket = new ErrorPacket().Parse(receive.Buffer) as ErrorPacket;

                    // 例外構築
                    NetworkException ex;
                    if (errPacket.ErrorCode == ErrorCode.ExistsName)
                    {
                        ex = new NetworkException(ExceptionError.ExistsName, "指定されたプレイヤー名は既に使用されています。");
                    }
                    else
                    {
                        ex = new NetworkException(ExceptionError.UnexpectedError, "通信接続中に想定外のエラーが発生しました。");
                    }

                    // 例外スロー
                    hostUdp.Close();
                    hostUdp.Dispose();
                    throw ex;
                }

                // 応答パケット以外の場合はスキップ
                if (BasePacket.GetPacketType(receive.Buffer) != typeof(DiscoverResponsePacket)) continue;

                // 応答パケット解析
                DiscoverResponsePacket responsePacket = new DiscoverResponsePacket().Parse(receive.Buffer) as DiscoverResponsePacket;

                // ホストとの接続情報保存
                hostUdp.Connect(receive.RemoteEndPoint);

                // ホストとTCPコネクション確立
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

                    // タイムアウトした場合はやり直し
                    if (ex is TimeoutException) continue;

                    throw;
                }

                // 接続済み一覧に追加
                PeerClient hostPeer = new PeerClient(responsePacket.HostName, PeerType.Host, hostUdp, hostTcp);
                _connectedClients.Add(hostPeer);

                // 切断イベント設定
                hostPeer.OnDisconnected += OnDisconnectedPeer;

                // 他プレイヤーとも接続
                foreach (string address in responsePacket.ClientAddresses)
                {
                    // 各プレイヤーごとのUDPクライアント初期化
                    UdpClient clientUdp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                    int udpPort = (clientUdp.Client.LocalEndPoint as IPEndPoint).Port;

                    // 各プレイヤーごとのTCPクライアント初期化
                    TcpClient clientTcp = new TcpClient(new IPEndPoint(IPAddress.Any, 0));

                    // 接続先プレイヤーのIPアドレス、TCPポート番号取り出し
                    IPEndPoint tcpEP = NetworkUtil.ConvertToIPEndPoint(address);

                    // コネクション確立
                    try
                    {
                        await ConnectUtil.ConnectAsync(clientTcp, tcpEP.Address, tcpEP.Port, 10, token);
                    }
                    catch (Exception ex)
                    {
                        clientUdp.Close();
                        clientUdp.Dispose();

                        // タイムアウト
                        if (ex is TimeoutException)
                        {
                            throw new NetworkException(ExceptionError.UnexpectedError, "通信接続中に想定外のエラーが発生しました。");
                        }

                        throw;
                    }

                    // プレイヤー名とUdpポート番号送信
                    byte[] peerConnectData = new PeerConnectPacket(name, udpPort).ConvertToPacket();
                    await clientTcp.GetStream().WriteAsync(peerConnectData, 0, peerConnectData.Length);

                    // プレイヤー名とUdpポート番号受信
                    byte[] buf = await ConnectUtil.ReceiveTcpAsync(clientTcp, 10, token);

                    // クライアント接続パケット以外の場合は不整合のためエラー
                    if (BasePacket.GetPacketType(buf) != typeof(PeerConnectPacket))
                    {
                        clientUdp.Close();
                        clientUdp.Dispose();
                        clientTcp.Close();
                        clientTcp.Dispose();
                        throw new NetworkException(ExceptionError.UnexpectedError, "通信接続中に想定外のエラーが発生しました。");
                    }

                    // パケット解析
                    PeerConnectPacket connectPacket = new PeerConnectPacket().Parse(buf) as PeerConnectPacket;

                    // UDP接続を確立
                    clientUdp.Connect(tcpEP.Address, connectPacket.UdpPort);

                    // 接続済み一覧に追加
                    PeerClient peerClient = new PeerClient(connectPacket.Name, PeerType.Client, clientUdp, clientTcp);
                    _connectedClients.Add(peerClient);

                    // 切断イベント設定
                    peerClient.OnDisconnected += OnDisconnectedPeer;
                }

                // クライアント同士の接続完了をホストに通知
                byte[] connectedData = new ConnectedClientsPacket().ConvertToPacket();
                await hostTcp.GetStream().WriteAsync(connectedData, 0, connectedData.Length);

                // ホストからのTCP受信を開始する
                hostPeer.OnTcpReceived += OnTcpReceivedFromHost;

                // ホスト発見イベント発火
                OnHostDiscovered?.Invoke(this, new List<PeerClient>(_connectedClients));

                break;
            }
        }

        /// <summary>
        /// 新規プレイヤーからの接続を受付
        /// </summary>
        /// <param name="name">自分のプレイヤー名</param>
        /// <param name="token">キャンセルトークン</param>
        /// <exception cref="NetworkException"></exception>
        private async UniTask AcceptFromClient(string name, CancellationToken token)
        {
            while (true)
            {
                // 新規プレイヤーからの接続待機
                TcpClient tcpClient = await ConnectUtil.AcceptTcpClientAsync(_listener, -1, IPAddress.Any, token, _completedCancel.Token, _completedCancel.Token, _hostDisconnectCancel.Token);

                // クライアント接続パケット受信待機
                byte[] buf = await ConnectUtil.ReceiveTcpAsync(tcpClient, 10, token, _completedCancel.Token, _hostDisconnectCancel.Token);

                // クライアント接続パケット以外の場合は想定外のためエラー
                if (BasePacket.GetPacketType(buf) != typeof(PeerConnectPacket))
                {
                    tcpClient.Close();
                    tcpClient.Dispose();
                    throw new NetworkException(ExceptionError.UnexpectedError, "通信接続中に想定外のエラーが発生しました。");
                }

                // パケット解析
                PeerConnectPacket connectPacket = new PeerConnectPacket().Parse(buf) as PeerConnectPacket;

                // UDP接続を確立
                UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                udpClient.Connect((tcpClient.Client.RemoteEndPoint as IPEndPoint).Address, connectPacket.UdpPort);

                // プレイヤー名とUdpポート番号送信
                int udpPort = (udpClient.Client.LocalEndPoint as IPEndPoint).Port;
                byte[] peerConnectData = new PeerConnectPacket(name, udpPort).ConvertToPacket();
                await tcpClient.GetStream().WriteAsync(peerConnectData, 0, peerConnectData.Length);

                // 接続済み一覧に追加
                PeerClient peerClient = new PeerClient(connectPacket.Name, PeerType.Client, udpClient, tcpClient);
                _connectedClients.Add(peerClient);

                // 切断イベント設定
                peerClient.OnDisconnected += OnDisconnectedPeer;

                // 接続完了イベント発火
                OnConnected?.Invoke(this, peerClient);
            }
        }

        /// <summary>
        /// ホストからのTCPパケット受信イベント
        /// </summary>
        /// <param name="client">イベントオブジェクト</param>
        /// <param name="name">プレイヤー名</param>
        /// <param name="packet">受信したTCPパケット</param>
        private void OnTcpReceivedFromHost(PeerClient client, BasePacket packet)
        {
            if (packet is ConnectionCompletedPacket complete)
            {
                // キャンセル発行して接続完了させる
                client.OnTcpReceived -= OnTcpReceivedFromHost;
                _completedCancel.Cancel();
            }
        }

        /// <summary>
        /// 接続済みのプレイヤー切断イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnDisconnectedPeer(object sender, EventArgs e)
        {
            PeerClient client = sender as PeerClient;

            // イベント削除
            client.OnTcpReceived -= OnTcpReceivedFromHost;
            client.OnDisconnected -= OnDisconnectedPeer;

            // ホストから切断された場合はキャンセル発行
            if (client.RemoteType == PeerType.Host)
            {
                _hostDisconnectCancel.Cancel();
            }
            else
            {
                // 接続済み情報から削除
                _connectedClients.Remove(client);
            }
        }

        /// <summary>
        /// 全てのホスト/クライアントと通信を切断
        /// </summary>
        private void DisconnectAll()
        {
            // 通信切断
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
