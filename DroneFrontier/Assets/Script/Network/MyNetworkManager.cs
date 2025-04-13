using Cysharp.Threading.Tasks;
using Network.Tcp;
using Network.Udp;
using System;
using System.Collections.Concurrent;
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

        /// <summary>
        /// 通信相手探索完了イベント
        /// </summary>
        public event EventHandler OnDiscoveryCompleted;

        #region 通信接続イベント

        /// <summary>
        /// 通信接続イベントハンドラー
        /// </summary>
        /// <param name="playerName">通信相手のプレイヤー名</param>
        public delegate void DiscoveryHandle(string playerName);

        /// <summary>
        /// 通信接続イベント
        /// </summary>
        public event DiscoveryHandle OnConnect;

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

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        public event UdpReceiveHandle OnUdpReceiveOnMainThread;

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
        /// プレイヤー探索用UDP管理クラス
        /// </summary>
        private UdpClient _discoverUdpClient = null;

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

        private ConcurrentQueue<(byte[] data, IPEndPoint ep)> _receivedUdpQueue = new ConcurrentQueue<(byte[] data, IPEndPoint ep)>();
        private ConcurrentQueue<(string name, UdpHeader header, UdpPacket packet)> _invokeUdpQueue = new ConcurrentQueue<(string name, UdpHeader header, UdpPacket packet)>();

        private bool _tcpReceiving = false;
        private bool _udpReceiving = false;

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
                    _discoverUdpClient?.Close();
                    _discoverUdpClient?.Dispose();
                    _discoverUdpClient = new UdpClient(LOCAL_ENDPOINT);
                    _discoverUdpClient.EnableBroadcast = true;

                    // UDP受信待機
                    var receive = await _discoverUdpClient.ReceiveAsync();
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
                        await _discoverUdpClient.SendAsync(errData, errData.Length, receive.RemoteEndPoint);

                        // 再度受信
                        _discoverUdpClient.Close();
                        _discoverUdpClient.Dispose();
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
                    await _discoverUdpClient.SendAsync(responseData, responseData.Length, receive.RemoteEndPoint);

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
                    OnConnect?.Invoke(receivePacket.Name);

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
                    _discoverUdpClient = new UdpClient(LOCAL_ENDPOINT);

                    // ブロードキャスト有効化
                    _discoverUdpClient.EnableBroadcast = true;

                    // ブロードキャストで探索パケット送信
                    byte[] data = new DiscoverPacket(name).ConvertToPacket();
                    await _discoverUdpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, PORT));

                    // 返信待機
                    Task responseTimeoutTask = Task.Delay(10 * 1000);
                    var receiveTask = _discoverUdpClient.ReceiveAsync();
                    while (true)
                    {
                        if (await Task.WhenAny(receiveTask, responseTimeoutTask) == receiveTask)
                        {
                            // 自分が投げたブロードキャストの場合は無視
                            if (GetLocalIPAddresses().Contains(receiveTask.Result.RemoteEndPoint.Address.ToString()))
                            {
                                receiveTask = _discoverUdpClient.ReceiveAsync();
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
                        _discoverUdpClient.Close();
                        _discoverUdpClient.Dispose();
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
                        _discoverUdpClient.Close();
                        _discoverUdpClient.Dispose();
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
                        _discoverUdpClient?.Close();
                        _discoverUdpClient?.Dispose();
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
                        _discoverUdpClient?.Close();
                        _discoverUdpClient?.Dispose();
                        continue;
                    }

                    // 切断された場合は不整合が起きているので最初からやり直す
                    if (tcpTask.Result == 0)
                    {
                        tcpCancel.Dispose();
                        tcpClient.Close();
                        tcpClient.Dispose();
                        _discoverUdpClient?.Close();
                        _discoverUdpClient?.Dispose();
                        continue;
                    }

                    // --- TCP接続完了チェック用のTCPパケット待機 end

                    // 接続先一覧にホストを追加
                    lock (_peers) _peers.Add(responsePacket.HostName, (receive.RemoteEndPoint, tcpClient, true));
                    lock (PlayerNames) PlayerNames.Add(responsePacket.HostName);

                    // ホストからのTCP受信を開始する
                    ReceiveTcp(responsePacket.HostName, true);

                    // ホストからの探索完了パケット受信待機
                    OnTcpReceive += OnDiscoveryCompleteReceive;

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
                    OnConnect?.Invoke(responsePacket.HostName);

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
        /// 通信相手の探索を停止
        /// </summary>
        public void StopDiscovery()
        {
            _discoverCancel.Cancel();
            _discoverUdpClient.Close();
            _discoverUdpClient.Dispose();
            _discoverUdpClient = null;

            // クライアントへ探索完了を通知
            if (IsHost)
                SendToAll(new DiscoveryCompletePacket());

            // UDP受信開始
            _udpClient = new UdpClient(LOCAL_ENDPOINT);
            ReceiveUdp();

            // 探索完了受信イベント削除
            OnTcpReceive -= OnDiscoveryCompleteReceive;

            // 探索完了イベント発火
            OnDiscoveryCompleted?.Invoke(this, EventArgs.Empty);
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

            // 探索完了受信イベント削除
            OnTcpReceive -= OnDiscoveryCompleteReceive;

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

            // 受信キュー削除
            _receivedUdpQueue.Clear();
            _invokeUdpQueue.Clear();
        }

        /// <summary>
        /// ホストへパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
        public void SendToHost(UdpPacket packet)
        {
            if (IsHost) return;

            byte[] data = packet.ConvertToPacket();
            UniTask.Void(async () =>
            {
                foreach (string key in _peers.Keys)
                {
                    if (_peers[key].isHost)
                    {
                        _udpClient.Send(data, data.Length, _peers[key].ep);
                        break;
                    }
                }

                await UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// ホストへパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
        public void SendToHost(TcpPacket packet)
        {
            if (IsHost) return;

            byte[] data = packet.ConvertToPacket();
            UniTask.Void(async () =>
            {
                foreach (string key in _peers.Keys)
                {
                    if (_peers[key].isHost)
                    {
                        _peers[key].tcp.GetStream().Write(data, 0, data.Length);
                        break;
                    }
                }

                await UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// 全ての通信相手へパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
        public void SendToAll(UdpPacket packet)
        {
            byte[] data = packet.ConvertToPacket();
            UniTask.Void(async () =>
            {
                foreach (string key in _peers.Keys)
                {
                    _udpClient.Send(data, data.Length, _peers[key].ep);
                }

                await UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// 全ての通信相手へパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
        public void SendToAll(TcpPacket packet)
        {
            byte[] data = packet.ConvertToPacket();
            UniTask.Void(async () =>
            {
                foreach (string key in _peers.Keys)
                {
                    _peers[key].tcp.GetStream().Write(data, 0, data.Length);
                }

                await UniTask.CompletedTask;
            });
        }

        private void Awake()
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        /// <summary>
        /// 指定したプレイヤーからのTCP受信を開始する
        /// </summary>
        /// <param name="player">TCPの受信先プレイヤー</param>
        /// <param name="isHost">送信元がホストであるか</param>
        private void ReceiveTcp(string player, bool isHost)
        {
            if (_tcpReceiving) return;
            _tcpReceiving = true;

            TcpClient client = _peers[player].tcp;
            UniTask.Void(async () =>
            {
                try
                {
                    while (true)
                    {
                        // Tcp受信待機
                        byte[] buf = new byte[2048];
                        int size = await client.GetStream().ReadAsync(buf, 0, buf.Length);

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

                            // 通信切断済みでない場合
                            if (IsHost || IsClient)
                            {
                                // 切断イベント発火
                                OnDisconnect?.Invoke(player, isHost);

                                // ホストの場合は全てのクライアントと切断
                                if (isHost)
                                {
                                    Disconnect();
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
                }
                catch (SocketException)
                {
                    // 切断
                }
                catch (ObjectDisposedException)
                {
                    // 切断
                }
                catch (IOException)
                {
                    // 切断
                }
                finally
                {
                    _tcpReceiving = false;
                }
            });
        }

        /// <summary>
        /// UDP受信を開始する
        /// </summary>
        private void ReceiveUdp()
        {
            if (_udpReceiving) return;
            _udpReceiving = true;

            CheckUdpReceiveData();
            CheckUdpInvokeData();

            var semaphore = new SemaphoreSlim(1, 1);
            ParallelOptions options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 3
            };
            Parallel.For(0, 3, async i =>
            {
                try
                {
                    while (true)
                    {
                        if (_udpClient == null) break;

                        // パケット受信
                        await semaphore.WaitAsync();
                        var result = await _udpClient.ReceiveAsync().ConfigureAwait(false);
                        semaphore.Release();

                        // 受信データをキューに追加
                        _receivedUdpQueue.Enqueue((result.Buffer, result.RemoteEndPoint));
                    }
                }
                catch (SocketException)
                {
                    // 切断
                    semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // 切断
                    semaphore.Release();
                }

                Debug.Log("ReceiveUdp() End");
                _udpReceiving = false;
            });
        }

        private async void CheckUdpReceiveData()
        {
            while (true)
            {
                if (!_udpReceiving) break;
                if (_receivedUdpQueue.Count > 0)
                {
                    while (_receivedUdpQueue.TryDequeue(out var data))
                    {
                        // 送信元プレイヤー名取得
                        string sendPlayer = string.Empty;
                        foreach (string key in _peers.Keys)
                        {
                            if (_peers[key].ep.Equals(data.ep))
                            {
                                sendPlayer = key;
                                break;
                            }
                        }

                        // 型名取得
                        Type type = UdpPacket.GetUdpType(data.data);

                        // 型名を基にコンストラクタ情報を取得
                        var constructor = type.GetConstructor(Type.EmptyTypes);
                        var expression = Expression.Lambda<Func<IPacket>>(Expression.New(constructor)).Compile();
                        // コンストラクタ実行
                        IPacket packet = expression();

                        UdpHeader header = UdpPacket.GetUdpHeader(data.data);
                        UdpPacket udpPacket = packet.Parse(data.data) as UdpPacket;
                        _invokeUdpQueue.Enqueue((sendPlayer, header, udpPacket));
                        UniTask.Void(async () =>
                        {
                            OnUdpReceive?.Invoke(sendPlayer, header, udpPacket);
                            await UniTask.CompletedTask;
                        });
                    }
                }

                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        private async void CheckUdpInvokeData()
        {
            while (true)
            {
                if (!_udpReceiving) break;
                if (_invokeUdpQueue.Count > 0)
                {
                    while (_invokeUdpQueue.TryDequeue(out var data))
                    {
                        OnUdpReceiveOnMainThread?.Invoke(data.name, data.header, data.packet);
                    }
                }

                await UniTask.Delay(1, ignoreTimeScale: true);
            }
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

        /// <summary>
        /// 探索完了パケット受信イベント
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したTCPパケットのヘッダ</param>
        /// <param name="packet">受信したTCPパケット</param>
        private void OnDiscoveryCompleteReceive(string name, TcpHeader header, TcpPacket packet)
        {
            if (header == TcpHeader.DiscoveryComplete)
            {
                OnTcpReceive -= OnDiscoveryCompleteReceive;
                StopDiscovery();
            }
        }
    }
}