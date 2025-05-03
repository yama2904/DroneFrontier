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
    /// クライアントからのP2P接続受付クラス
    /// </summary>
    public class PeerListener
    {
        /// <summary>
        /// 通信接続イベント
        /// </summary>
        public event ConnectionHandler OnConnected;

        /// <summary>
        /// 受付完了イベント
        /// </summary>
        public event EventHandler OnAcceptCompleted;

        /// <summary>
        /// 接続受付ポート番号
        /// </summary>
        private readonly int PORT;

        /// <summary>
        /// 最大接続数
        /// </summary>
        private readonly int MAX_CONNECT;

        /// <summary>
        /// 接続済みプレイヤーのTCPリッスン一覧
        /// </summary>
        private List<(string name, string address)> _clientListenAddresses = new List<(string name, string address)>();

        /// <summary>
        /// 接続済みクライアント
        /// </summary>
        private List<PeerClient> _connectedClients = new List<PeerClient>();

        /// <summary>
        /// 接続受付中の接続完了キャンセル
        /// </summary>
        private CancellationTokenSource _completedCancel = new CancellationTokenSource();

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="port">接続受付ポート番号</param>
        /// <param name="maxConnect">最大接続数</param>
        public PeerListener(int port, int maxConnect)
        {
            PORT = port;
            MAX_CONNECT = maxConnect;
        }

        /// <summary>
        /// クライアントからの接続受付開始<br/>
        /// キャンセルを発行した場合は接続済みの全てのクライアントと切断する
        /// </summary>
        /// <param name="name">自分のプレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="token">キャンセルトークン</param>
        /// <exception cref="TaskCanceledException"></exception>
        public async UniTask StartAccept(string name, string gameMode, CancellationToken token)
        {
            // クライアントからの探索受信用UDP
            UdpClient broadcastUdp = null;

            // 受信開始
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, PORT);
                listener.Start();

                while (true) 
                {
                    // 最大接続数に達した場合は受付しない
                    if (_connectedClients.Count == MAX_CONNECT)
                    {
                        await Task.Delay(1 * 1000, _completedCancel.Token);
                        continue;
                    }

                    // UDPクライアント初期化
                    broadcastUdp?.Close();
                    broadcastUdp?.Dispose();
                    broadcastUdp = new UdpClient(new IPEndPoint(IPAddress.Any, PORT));
                    broadcastUdp.EnableBroadcast = true;

                    // 受信開始
                    var receive = await ConnectUtil.ReceiveUdpAsync(broadcastUdp, -1, token, _completedCancel.Token);
                    Debug.Log("受信：" + receive.RemoteEndPoint);

                    // プレイヤー探索パケット以外の場合はスキップ
                    if (BasePacket.GetPacketType(receive.Buffer) != typeof(DiscoverPacket)) continue;

                    // ゲームモードが異なる場合はスキップ
                    DiscoverPacket discoverPacket = new DiscoverPacket().Parse(receive.Buffer) as DiscoverPacket;
                    if (discoverPacket.GameMode != gameMode) continue;

                    // プレイヤー名重複チェック
                    if (discoverPacket.Name == name || _clientListenAddresses.Any(x => x.name == discoverPacket.Name))
                    {
                        Debug.Log("プレイヤー名重複");

                        // プレイヤー名が重複している場合はエラーパケットを返す
                        byte[] errData = new ErrorPacket(ErrorCode.ExistsName).ConvertToPacket();
                        await broadcastUdp.SendAsync(errData, errData.Length, receive.RemoteEndPoint);

                        // 再度受信
                        continue;
                    }

                    // 送受信用UDP初期化
                    UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                    udpClient.Connect(receive.RemoteEndPoint);

                    // 接続済み情報を返す
                    List<string> addresses = _clientListenAddresses.Select(x => x.address).ToList();
                    byte[] responseData = new DiscoverResponsePacket(name, addresses).ConvertToPacket();
                    await udpClient.SendAsync(responseData, responseData.Length);

                    // クライアントからのTCP接続待機
                    TcpClient tcpClient = null;
                    try
                    {
                        tcpClient = await ConnectUtil.AcceptTcpClientAsync(listener, 10, receive.RemoteEndPoint.Address, token, _completedCancel.Token);
                    }
                    catch (Exception ex)
                    {
                        udpClient.Close();
                        udpClient.Dispose();

                        // タイムアウトした場合は受信しなおし
                        if (ex is TimeoutException) continue;

                        throw;
                    }

                    // クライアント同士の接続完了待ち
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

                        // タイムアウトした場合は受信しなおし
                        if (ex is TimeoutException) continue;

                        throw;
                    }

                    // クライアント同士の接続完了パケット以外の場合は不整合のためエラー
                    if (BasePacket.GetPacketType(buf) != typeof(ConnectedClientsPacket))
                    {
                        udpClient.Close();
                        udpClient.Dispose();
                        tcpClient.Close();
                        tcpClient.Dispose();
                        throw new NetworkException(ExceptionError.UnexpectedError, "通信接続中に想定外のエラーが発生しました。");
                    }

                    // クライアントのリッスンアドレス保存
                    _clientListenAddresses.Add((discoverPacket.Name, NetworkUtil.ConvertToString(receive.RemoteEndPoint.Address, discoverPacket.ListenPort)));

                    // 接続先クライアント保存
                    PeerClient peerClient = new PeerClient(discoverPacket.Name, PeerType.Client, udpClient, tcpClient);
                    _connectedClients.Add(peerClient);

                    // 切断イベント設定
                    peerClient.OnDisconnected += OnDisconnectPeer;

                    // 接続イベント発行
                    OnConnected?.Invoke(this, peerClient);
                }
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

                    // 接続完了によるキャンセルは処理続行
                }
                else
                {
                    // 想定外の例外は切断してエラーを吐く
                    Debug.Log(ex);
                    DisconnectAll();
                    throw new NetworkException(ExceptionError.UnexpectedError, "想定外のエラーが発生しました。");
                }
            }
            finally
            {
                broadcastUdp?.Close();
                broadcastUdp?.Dispose();
                listener?.Stop();
            }

            // クライアントに接続完了を送信
            foreach (PeerClient client in _connectedClients)
            {
                client.SendTcp(new ConnectionCompletedPacket());
            }

            // 接続完了イベント発火
            OnAcceptCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// クライアント数が最大接続数に達する前に接続受付を完了させる
        /// </summary>
        public void CompleteAppect()
        {
            _completedCancel.Cancel();
        }

        /// <summary>
        /// 全てのクライアントと通信を切断
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
        /// 接続済みのプレイヤー切断イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnDisconnectPeer(object sender, EventArgs e)
        {
            PeerClient client = sender as PeerClient;

            // 接続済み情報から削除
            _connectedClients.Remove(client);
            _clientListenAddresses.RemoveAt(_clientListenAddresses.FindIndex(x => x.name == client.RemoteName));
        }
    }
}
