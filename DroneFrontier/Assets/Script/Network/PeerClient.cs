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
    /// P2P接続クライアント
    /// </summary>
    public class PeerClient : IDisposable
    {
        /// <summary>
        /// 通信先プレイヤー名
        /// </summary>
        public string RemoteName { get; private set; } = string.Empty;

        /// <summary>
        /// 通信先のホスト/クライアント
        /// </summary>
        public PeerType RemoteType { get; private set; } = PeerType.None;

        /// <summary>
        /// TCPパケット受信イベント
        /// </summary>
        public event ReceiveHandler OnTcpReceived;

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        public event ReceiveHandler OnUdpReceived;

        /// <summary>
        /// UDPパケット受信イベント（メインスレッド上で実行）
        /// </summary>
        public event ReceiveHandler OnUdpReceivedOnMainThread;

        /// <summary>
        /// プレイヤー切断イベント
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

            // 受信開始
            ReceiveTcp();
            ReceiveUdp();
        }

        /// <summary>
        /// UDPパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
        public void SendUdp(BasePacket packet)
        {
            byte[] data = packet.ConvertToPacket();
            _udp.Send(data, data.Length);
        }

        /// <summary>
        /// TCPパケットを送信する
        /// </summary>
        /// <param name="packet">送信パケット</param>
        public void SendTcp(BasePacket packet)
        {
            byte[] data = packet.ConvertToPacket();
            _tcp.GetStream().Write(data, 0, data.Length);
        }

        /// <summary>
        /// 通信切断
        /// </summary>
        public void Disconnect()
        {
            // TCP切断/停止
            _tcp?.Close();
            _tcp?.Dispose();
            _tcp = null;

            // UDP停止
            _udp?.Close();
            _udp?.Dispose();
            _udp = null;

            // 受信キュー削除
            _receivedUdpQueue.Clear();
            _invokeUdpQueue.Clear();
        }

        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// 指定したプレイヤーからのTCP受信を開始する
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

                        // Tcp受信待機
                        byte[] buf = new byte[2048];
                        int size = await _tcp.GetStream().ReadAsync(buf, 0, buf.Length);

                        // 切断チェック
                        if (size == 0)
                        {
                            _tcp?.Close();
                            _tcp?.Dispose();

                            // 切断イベント発火
                            OnDisconnected?.Invoke(this, EventArgs.Empty);
                            Dispose();

                            break;
                        }

                        // 型名取得
                        Type type = BasePacket.GetPacketType(buf);

                        // 型名を基にコンストラクタ情報を取得
                        var constructor = type.GetConstructor(Type.EmptyTypes);
                        var expression = Expression.Lambda<Func<BasePacket>>(Expression.New(constructor)).Compile();
                        // コンストラクタ実行
                        BasePacket packet = expression();

                        // イベント発火
                        OnTcpReceived?.Invoke(this, packet.Parse(buf));
                    }
                }
                catch (IOException)
                {
                    // 受信キャンセル
                }
            });
        }

        /// <summary>
        /// UDP受信を開始する
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

                        // パケット受信
                        await semaphore.WaitAsync();
                        var result = await _udp.ReceiveAsync().ConfigureAwait(false);
                        semaphore.Release();

                        // 受信データをキューに追加
                        _receivedUdpQueue.Enqueue((result.Buffer, result.RemoteEndPoint));
                    }
                }
                catch (Exception)
                {
                    // 切断
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
                        // 型名取得
                        Type type = BasePacket.GetPacketType(data.data);

                        // 型名を基にコンストラクタ情報を取得
                        var constructor = type.GetConstructor(Type.EmptyTypes);
                        var expression = Expression.Lambda<Func<BasePacket>>(Expression.New(constructor)).Compile();
                        // コンストラクタ実行
                        BasePacket packet = expression();

                        // パケット解析
                        BasePacket udpPacket = packet.Parse(data.data);

                        // メインスレッド実行用キューへ解析パケット追加
                        _invokeUdpQueue.Enqueue(udpPacket);

                        // ワーカースレッドでUDP受信イベント発火
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
