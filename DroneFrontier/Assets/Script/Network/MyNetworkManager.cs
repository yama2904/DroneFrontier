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
        /// ホスト側であるか
        /// </summary>
        public bool IsHost { get; private set; } = false;

        /// <summary>
        /// クライアント側であるか
        /// </summary>
        public bool IsClient { get; private set; } = false;

        /// <summary>
        /// 自分のプレイヤー名
        /// </summary>
        public string MyPlayerName { get; private set; } = string.Empty;

        /// <summary>
        /// 各プレイヤー名
        /// </summary>
        public List<string> PlayerNames { get; private set; } = new List<string>();

        /// <summary>
        /// プレイヤー数
        /// </summary>
        public int PlayerCount => PlayerNames.Count;

        #region 通信相手発見イベント

        /// <summary>
        /// 通信相手発見イベントハンドラー
        /// </summary>
        /// <param name="playerName">通信相手のプレイヤー名</param>
        public delegate void DiscoveryHandle(string playerName);

        /// <summary>
        /// 通信相手発見イベント
        /// </summary>
        public event DiscoveryHandle OnDiscovery;

        #endregion

        #region TCPパケット受信イベント

        /// <summary>
        /// TCPパケット受信イベントハンドラー
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したTCPパケットのヘッダ</param>
        /// <param name="packet">受信したTCPパケット</param>
        public delegate void TcpReceiveHandle(string name, TcpHeader header, TcpPacket packet);

        /// <summary>
        /// TCPパケット受信イベント
        /// </summary>
        public event TcpReceiveHandle OnTcpReceive;

        #endregion

        #region UDPパケット受信イベント

        /// <summary>
        /// UDPパケット受信イベントハンドラー
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        public delegate void UdpReceiveHandle(string name, UdpHeader header, UdpPacket packet);

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        public event UdpReceiveHandle OnUdpReceive;

        #endregion

        #region プレイヤー切断イベント

        /// <summary>
        /// プレイヤー切断イベントハンドラー
        /// </summary>
        /// <param name="name">切断したプレイヤー名</param>
        /// <param name="isHost">切断したプレイヤーがホストであるか</param>
        public delegate void DisconnectHandle(string name, bool isHost);

        /// <summary>
        /// プレイヤー切断
        /// </summary>
        public event DisconnectHandle OnDisconnect;

        #endregion

        /// <summary>
        /// 使用するポート番号
        /// </summary>
        private const int PORT = 5556;

        /// <summary>
        /// 最大クライアント数
        /// </summary>
        private const int MAX_CLIENT_NUM = 3;

        /// <summary>
        /// ローカルエンドポイント
        /// </summary>
        private readonly IPEndPoint LOCAL_ENDPOINT = new IPEndPoint(IPAddress.Any, PORT);

        /// <summary>
        /// UDP管理クラス
        /// </summary>
        private UdpClient _udpClient = null;

        /// <summary>
        /// 接続先一覧<br/>
        /// key:プレイヤー名<br/>
        /// value:接続先情報
        /// </summary>
        private Dictionary<string, (IPEndPoint ep, TcpClient tcp, bool isHost)> _peers = new Dictionary<string, (IPEndPoint ep, TcpClient tcp, bool isHost)>();

        /// <summary>
        /// 探索キャンセル発行クラス
        /// </summary>
        private CancellationTokenSource _discoverCancel = new CancellationTokenSource();

        /// <summary>
        /// ホストとして通信を開始
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        public async UniTask StartHost(string name)
        {
            // ホストフラグを立てる
            IsHost = true;

            // キャンセルトークン初期化
            _discoverCancel = new CancellationTokenSource();

            // プレイヤー名保存
            MyPlayerName = name;

            // プレイヤーリストに自分を追加
            lock (PlayerNames) PlayerNames.Add(name);

            // 受信開始
            try
            {
                while (true)
                {
                    // キャンセル確認
                    if (_discoverCancel.IsCancellationRequested) break;

                    // 最大プレイヤー数に達している場合は受信しない
                    if (_peers.Count >= MAX_CLIENT_NUM) continue;

                    // UdpClient初期化
                    _udpClient?.Close();
                    _udpClient?.Dispose();
                    _udpClient = new UdpClient(LOCAL_ENDPOINT);
                    _udpClient.EnableBroadcast = true;

                    // UDP受信待機
                    var receive = await _udpClient.ReceiveAsync();
                    Debug.Log("受信：" + receive.RemoteEndPoint);

                    // プレイヤー探索パケット以外の場合はスキップ
                    if (UdpPacket.GetUdpHeader(receive.Buffer) != UdpHeader.Discover) continue;

                    // プレイヤー名重複チェック
                    DiscoverPacket receivePacket = new DiscoverPacket().Parse(receive.Buffer) as DiscoverPacket;
                    if (receivePacket.Name == name || _peers.ContainsKey(receivePacket.Name))
                    {
                        Debug.Log("プレイヤー名重複");

                        // プレイヤー名が重複している場合はエラーパケットを返す
                        byte[] errData = new ErrorPacket(ErrorCode.ExistsName).ConvertToPacket();
                        await _udpClient.SendAsync(errData, errData.Length, receive.RemoteEndPoint);

                        // 再度受信
                        _udpClient.Close();
                        _udpClient.Dispose();
                        continue;
                    }

                    // --- クライアントへ返信 start
                    
                    // 接続済みクライアントの名前とIPアドレスを構築
                    Dictionary<string, string> clientAdrs = new Dictionary<string, string>();
                    lock (_peers)
                    {
                        foreach (string key in _peers.Keys)
                        {
                            clientAdrs.Add(key, _peers[key].ep.Address.ToString());
                        }
                    }

                    // 自分の名前と各クライアントの情報を格納して返信
                    byte[] responseData = new DiscoverResponsePacket(name, clientAdrs).ConvertToPacket();
                    await _udpClient.SendAsync(responseData, responseData.Length, receive.RemoteEndPoint);
                    
                    // --- クライアントへ返信 end

                    // クライアントからのTCP待機
                    TcpListener listener = new TcpListener(LOCAL_ENDPOINT);
                    listener.Start();
                    TcpClient tcpClient = null;
                    try
                    {
                        // タイムアウト計測用ストップウォッチ
                        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

                        // TCP待機開始
                        while (true)
                        {
                            // タイムアウトチェック
                            if (stopwatch.Elapsed.TotalSeconds > 10) break;

                            // 接続要求待機
                            if (listener.Pending())
                            {
                                tcpClient = listener.AcceptTcpClient();

                                // 送信元IP判定
                                if (!(tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.Equals(receive.RemoteEndPoint.Address))
                                {
                                    tcpClient.Close();
                                    tcpClient.Dispose();
                                    tcpClient = null;
                                    continue;
                                }

                                // TCPパケットを送って接続完了を知らせる
                                await tcpClient.GetStream().WriteAsync(new byte[1]);
                                break;
                            }
                            else
                            {
                                // 接続要求がない場合はキャンセルチェック
                                if (_discoverCancel.IsCancellationRequested)
                                {
                                    return;
                                }
                            }

                            // 1秒間隔でチェック
                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 例外が起きた場合は想定外の不整合のため全て切断
                        Debug.LogError(ex);
                        Disconnect();
                        break;
                    }
                    finally
                    {
                        listener.Stop();
                    }

                    // タイムアウトした場合は受信しなおし
                    if (tcpClient == null) continue;

                    // 接続先クライアント保存
                    lock (_peers) _peers.Add(receivePacket.Name, (receive.RemoteEndPoint, tcpClient, false));
                    lock (PlayerNames) PlayerNames.Add(receivePacket.Name);

                    // 接続イベント発行
                    OnDiscovery?.Invoke(receivePacket.Name);

                    // 新規クライアントからのTCP受信開始
                    ReceiveTcp(receivePacket.Name, false);
                }
            }
            catch (SocketException)
            {
                // 受信キャンセル
            }
            catch (ObjectDisposedException)
            {
                // 受信キャンセル
            }
            catch (AggregateException ex)
            {
                // ソケットキャンセル以外の例外がある場合は再スロー
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
        /// クライアントとして通信を開始
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        public async UniTask StartClient(string name)
        {
            // クライアントフラグを立てる
            IsClient = true;

            // 探索キャンセルトークン初期化
            _discoverCancel = new CancellationTokenSource();

            // プレイヤー名保存
            MyPlayerName = name;

            // 探索キャンセル検知用タスクを事前に構築
            Task cancelCheckTask = Task.Run(async () =>
            {
                while (true)
                {
                    // 500ミリ秒ごとにチェック
                    if (_discoverCancel.IsCancellationRequested) break;
                    await Task.Delay(500);
                }
            });

            // ホスト探索開始
            try
            {
                while (true)
                {
                    // キャンセル確認
                    if (_discoverCancel.IsCancellationRequested) break;

                    // UdpClient初期化
                    _udpClient = new UdpClient(LOCAL_ENDPOINT);

                    // ブロードキャスト有効化
                    _udpClient.EnableBroadcast = true;

                    // ブロードキャストで探索パケット送信
                    byte[] data = new DiscoverPacket(name).ConvertToPacket();
                    await _udpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, PORT));

                    // 返信待機
                    Task responseTimeoutTask = Task.Delay(10 * 1000);
                    var receiveTask = _udpClient.ReceiveAsync();
                    while (true)
                    {
                        if (await Task.WhenAny(receiveTask, responseTimeoutTask) == receiveTask)
                        {
                            // 自分が投げたブロードキャストの場合は無視
                            if (GetLocalIPAddresses().Contains(receiveTask.Result.RemoteEndPoint.Address.ToString()))
                            {
                                receiveTask = _udpClient.ReceiveAsync();
                                continue;
                            }
                            break;
                        }
                        else
                        {
                            // 返信待ちタイムアウト
                            receiveTask = null;
                            break;
                        }
                    }

                    // タイムアウトチェック
                    if (receiveTask == null)
                    {
                        _udpClient.Close();
                        _udpClient.Dispose();
                        continue;
                    }

                    // 受信データ取得
                    var receive = receiveTask.Result;
                    Debug.Log("受信：" + receive.RemoteEndPoint);

                    // ヘッダ取得
                    UdpHeader header = UdpPacket.GetUdpHeader(receive.Buffer);

                    // エラーチェック
                    if (header == UdpHeader.Error)
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
                            ex = new NetworkException(ExceptionError.UnexpectedError, "想定外のエラーが発生しました。");
                        }

                        // ソケットを閉じて例外スロー
                        _udpClient.Close();
                        _udpClient.Dispose();
                        throw ex;
                    }

                    // 応答パケット以外の場合はスキップ
                    if (header != UdpHeader.DiscoverResponse) continue;

                    // 応答パケット解析
                    DiscoverResponsePacket responsePacket = new DiscoverResponsePacket().Parse(receive.Buffer) as DiscoverResponsePacket;

                    // --- ホストとTCPコネクションを貼る start

                    // TCP接続タスク
                    TcpClient tcpClient = new TcpClient();
                    Task connTask = tcpClient.ConnectAsync(receive.RemoteEndPoint.Address, PORT);

                    // タイムアウト用タスク
                    Task connTimeoutTask = Task.Delay(10 * 1000);

                    // タスク終了待機
                    if (await Task.WhenAny(connTask, connTimeoutTask, cancelCheckTask) != connTask)
                    {
                        // タイムアウト
                        tcpClient.Close();
                        tcpClient.Dispose();
                        _udpClient?.Close();
                        _udpClient?.Dispose();
                        continue;
                    }

                    // --- ホストとTCPコネクションを貼る end

                    // --- TCP接続完了チェック用のTCPパケット待機 start

                    // TCP受信用タスク
                    byte[] tcpBuf = new byte[1];
                    CancellationTokenSource tcpCancel = new CancellationTokenSource();
                    Task<int> tcpTask = tcpClient.GetStream().ReadAsync(tcpBuf, 0, tcpBuf.Length, tcpCancel.Token);

                    // タイムアウト用タスク
                    Task tcpTimeoutTask = Task.Delay(30 * 1000);

                    // タスク終了待機
                    if (await Task.WhenAny(tcpTask, tcpTimeoutTask, cancelCheckTask) != tcpTask)
                    {
                        // TCP受信以外のタスクが終わった場合はキャンセル処理
                        tcpCancel.Cancel();
                        tcpCancel.Dispose();
                        tcpClient.Close();
                        tcpClient.Dispose();
                        _udpClient?.Close();
                        _udpClient?.Dispose();
                        continue;
                    }

                    // 切断された場合は不整合が起きているので最初からやり直す
                    if (tcpTask.Result == 0)
                    {
                        tcpCancel.Dispose();
                        tcpClient.Close();
                        tcpClient.Dispose();
                        _udpClient?.Close();
                        _udpClient?.Dispose();
                        continue;
                    }

                    // --- TCP接続完了チェック用のTCPパケット待機 end

                    // 接続先一覧にホストを追加
                    lock (_peers) _peers.Add(responsePacket.HostName, (receive.RemoteEndPoint, tcpClient, true));
                    lock (PlayerNames) PlayerNames.Add(responsePacket.HostName);

                    // ホストからのTCP受信を開始する
                    ReceiveTcp(responsePacket.HostName, true);

                    // 他プレイヤーとも接続
                    try
                    {
                        foreach (string key in responsePacket.ClientAddresses.Keys)
                        {
                            // コネクションを貼る
                            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(responsePacket.ClientAddresses[key]), PORT);
                            TcpClient client = new TcpClient(ep);

                            // プレイヤー名送信
                            byte[] connectData = new PeerConnectPacket(name).ConvertToPacket();
                            await client.GetStream().WriteAsync(connectData, 0, connectData.Length);

                            // 接続先一覧に追加
                            lock (_peers) _peers.Add(key, (ep, client, false));

                            // プレイヤーからのTCP受信開始
                            ReceiveTcp(key, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 例外が起きた場合は想定外の不整合のため全て切断
                        Debug.LogError(ex);
                        Disconnect();
                        break;
                    }

                    // 接続済みプレイヤーの後続に自分を追加
                    lock (PlayerNames) PlayerNames.Add(name);

                    // 接続イベント発行
                    OnDiscovery?.Invoke(responsePacket.HostName);

                    // Udp受信開始
                    ReceiveUdp();

                    // 新規プレイヤーからの接続待機
                    TcpListener listener = new TcpListener(LOCAL_ENDPOINT);
                    listener.Start();
                    try
                    {
                        while (true)
                        {
                            // 接続要求待機
                            if (listener.Pending())
                            {
                                TcpClient client = listener.AcceptTcpClient();

                                // 名前受信
                                byte[] connectBuf = new byte[1024];
                                await tcpClient.GetStream().ReadAsync(connectBuf);

                                // ヘッダーチェック
                                if (TcpPacket.GetTcpHeader(connectBuf) != TcpHeader.PeerConnect)
                                {
                                    client.Close();
                                    client.Dispose();
                                    continue;
                                }

                                // パケット解析
                                PeerConnectPacket connectPacket = new PeerConnectPacket().Parse(connectBuf) as PeerConnectPacket;

                                // 接続先一覧に追加
                                lock (_peers) _peers.Add(connectPacket.Name, (client.Client.RemoteEndPoint as IPEndPoint, tcpClient, false));
                                lock (PlayerNames) PlayerNames.Add(connectPacket.Name);

                                // 新規プレイヤーからのTCP受信開始
                                ReceiveTcp(connectPacket.Name, false);
                            }
                            else
                            {
                                // 接続要求がない場合はキャンセルチェック
                                if (_discoverCancel.IsCancellationRequested)
                                {
                                    break;
                                }
                            }

                            // 1秒間隔でチェック
                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 例外が起きた場合は想定外の不整合のため全て切断
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
                // 受信キャンセル
            }
            catch (ObjectDisposedException)
            {
                // 受信キャンセル
            }
            catch (AggregateException ex)
            {
                // ソケットキャンセル以外の例外がある場合は再スロー
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
        /// 通信切断
        /// </summary>
        public void Disconnect()
        {
            // ホスト・クライアントフラグ初期化
            IsHost = false;
            IsClient = false;

            // 探索停止
            _discoverCancel.Cancel();

            // Udp停止
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;

            // 全てのプレイヤーと切断
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
        /// 通信相手の探索を停止
        /// </summary>
        public void StopDiscovery()
        {
            _discoverCancel.Cancel();
            _udpClient.Close();
            _udpClient.Dispose();
            _udpClient = new UdpClient(LOCAL_ENDPOINT);

            // 受信開始
            ReceiveUdp();
        }

        /// <summary>
        /// ホストへパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
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
        /// 全ての通信相手へパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
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
        /// 指定したプレイヤーからのTCP受信を開始する
        /// </summary>
        /// <param name="player">TCPの受信先プレイヤー</param>
        /// <param name="isHost">受信先がホストであるか</param>
        private void ReceiveTcp(string player, bool isHost)
        {
            TcpClient client = _peers[player].tcp;
            UniTask.Void(async () =>
            {
                while (true)
                {
                    // Tcp受信待機
                    byte[] buf = new byte[2048];
                    int size = 0;
                    try
                    {
                        size = await client.GetStream().ReadAsync(buf, 0, buf.Length);
                    }
                    catch (SocketException)
                    {
                        // 切断
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // 切断
                        break;
                    }
                    catch (IOException)
                    {
                        // 切断
                        break;
                    }

                    // 切断チェック
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

                        // ホストの場合は全てのクライアントと切断
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

                    // 型名取得
                    Type type = TcpPacket.GetTcpType(buf);

                    // 型名を基にコンストラクタ情報を取得
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    var expression = Expression.Lambda<Func<IPacket>>(Expression.New(constructor)).Compile();
                    // コンストラクタ実行
                    IPacket packet = expression();

                    // イベント発火
                    OnTcpReceive?.Invoke(player, TcpPacket.GetTcpHeader(buf), packet.Parse(buf) as TcpPacket);
                }
            });
        }

        /// <summary>
        /// UDP受信を開始する
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
                        // パケット受信
                        var receive = await _udpClient.ReceiveAsync();
                        buf = receive.Buffer;

                        // 送信元プレイヤー名取得
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
                        // 切断
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // 切断
                        break;
                    }

                    // 型名取得
                    Type type = UdpPacket.GetUdpType(buf);

                    // 型名を基にコンストラクタ情報を取得
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    var expression = Expression.Lambda<Func<IPacket>>(Expression.New(constructor)).Compile();
                    // コンストラクタ実行
                    IPacket packet = expression();

                    // イベント発火
                    OnUdpReceive?.Invoke(sendPlayer, UdpPacket.GetUdpHeader(buf), packet.Parse(buf) as UdpPacket);
                }
            });
        }

        /// <summary>
        /// ローカルIPアドレス一覧を返す
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