using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Network.Connect
{
    internal class ConnectUtil
    {
        /// <summary>
        /// タイムアウトとキャンセルを考慮して非同期でUDP受信を行う
        /// </summary>
        /// <param name="client">受信するUDPクライアント</param>
        /// <param name="timeout">受信タイムアウト（秒）</param>
        /// <param name="tokens">キャンセルトークン</param>
        /// <returns>受信データ</returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<UdpReceiveResult> ReceiveUdpAsync(UdpClient client, int timeout, params CancellationToken[] tokens)
        {
            // 受信タスク
            var receiveTask = client.ReceiveAsync();

            // タイムアウトタスク
            Task timeoutTask = Task.Delay(timeout == -1 ? -1 : timeout * 1000);

            // 呼び出し元キャンセル検知タスク
            Task[] cancelTasks = tokens.Select(x => Task.Delay(-1, x)).ToArray();

            while (true)
            {
                // タスク完了待機
                List<Task> tasks = new List<Task>();
                tasks.Add(receiveTask);
                tasks.Add(timeoutTask);
                tasks.AddRange(cancelTasks);
                Task completeTask = await Task.WhenAny(tasks);

                // UDP受信
                if (completeTask == receiveTask)
                {
                    // 自分が投げたパケットの場合は無視（ブロードキャスト考慮）
                    if (GetLocalIPAddresses().Contains(receiveTask.Result.RemoteEndPoint.Address.ToString()))
                    {
                        receiveTask = client.ReceiveAsync();
                        continue;
                    }
                    break;
                }

                // タイムアウト
                if (completeTask == timeoutTask) throw new TimeoutException();

                // 呼び出し元キャンセル
                if (cancelTasks.Contains(completeTask)) throw new TaskCanceledException();
            }

            // 受信完了
            return await receiveTask;
        }

        /// <summary>
        /// タイムアウトとキャンセルを考慮して非同期でTCPコネクションの受付を行う
        /// </summary>
        /// <param name="listener">コネクション受付を行うTCPリスナー</param>
        /// <param name="port">ローカルポート番号</param>
        /// <param name="timeout">受付タイムアウト（秒）</param>
        /// <param name="allowIP">コネクションを許可するIPアドレス（全てのIPから許可する場合はIPAddress.Anyを指定）</param>
        /// <param name="tokens">キャンセルトークン</param>
        /// <returns>コネクションを確立したTCPクライアント</returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<TcpClient> AcceptTcpClientAsync(TcpListener listener, int port, int timeout, IPAddress allowIP, params CancellationToken[] tokens)
        {
            // コネクション受付タスク
            CancellationTokenSource acceptCancel = new CancellationTokenSource();
            var acceptTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (listener.Pending())
                    {
                        return listener.AcceptTcpClient();
                    }
                    await Task.Delay(1, acceptCancel.Token);
                }
            });

            // タイムアウトタスク
            Task timeoutTask = Task.Delay(timeout == -1 ? -1 : timeout * 1000);

            // 呼び出し元キャンセル検知タスク
            Task[] cancelTasks = tokens.Select(x => Task.Delay(-1, x)).ToArray();

            TcpClient tcpClient = null;
            try
            {
                while (true)
                {
                    // タスク完了待機
                    List<Task> tasks = new List<Task>();
                    tasks.Add(acceptTask);
                    tasks.Add(timeoutTask);
                    tasks.AddRange(cancelTasks);
                    Task completeTask = await Task.WhenAny(tasks);

                    // 接続要求
                    if (completeTask == acceptTask)
                    {
                        tcpClient = await acceptTask;

                        // 送信元IP判定
                        if (!allowIP.Equals(IPAddress.Any))
                        {
                            if (!(tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.Equals(allowIP))
                            {
                                tcpClient.Close();
                                tcpClient.Dispose();
                                tcpClient = null;
                                continue;
                            }
                        }
                        break;
                    }

                    // タイムアウト
                    if (completeTask == timeoutTask) throw new TimeoutException();

                    // 呼び出し元キャンセル
                    if (cancelTasks.Contains(completeTask)) throw new TaskCanceledException();
                }
            }
            finally
            {
                acceptCancel.Cancel();
            }
            
            return tcpClient;
        }

        /// <summary>
        /// タイムアウトとキャンセルを考慮して非同期でTCPコネクションを確立
        /// </summary>
        /// <param name="client">コネクションを行うTCPクライアント</param>
        /// <param name="remoteIP">コネクション先IPアドレス</param>
        /// <param name="remotePort">コネクション先ポート番号</param>
        /// <param name="timeout">コネクションタイムアウト（秒）</param>
        /// <param name="tokens">キャンセルトークン</param>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task ConnectAsync(TcpClient client, IPAddress remoteIP, int remotePort, int timeout, params CancellationToken[] tokens)
        {
            // コネクションタスク
            var connTask = client.ConnectAsync(remoteIP, remotePort);

            // タイムアウトタスク
            Task timeoutTask = Task.Delay(timeout == -1 ? -1 : timeout * 1000);

            // 呼び出し元キャンセル検知タスク
            Task[] cancelTasks = tokens.Select(x => Task.Delay(-1, x)).ToArray();

            // タスク完了待機
            List<Task> tasks = new List<Task>();
            tasks.Add(connTask);
            tasks.Add(timeoutTask);
            tasks.AddRange(cancelTasks);
            Task completeTask = await Task.WhenAny(tasks);

            // タイムアウト
            if (completeTask == timeoutTask) throw new TimeoutException();

            // 呼び出し元キャンセル
            if (cancelTasks.Contains(completeTask)) throw new TaskCanceledException();

            // 接続完了
            return;
        }

        /// <summary>
        /// タイムアウトとキャンセルを考慮して非同期でTCP受信を行う
        /// </summary>
        /// <param name="client">受信するTCPクライアント</param>
        /// <param name="timeout">受信タイムアウト（秒）</param>
        /// <param name="tokens">キャンセルトークン</param>
        /// <returns>受信データ</returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<byte[]> ReceiveTcpAsync(TcpClient client, int timeout, params CancellationToken[] tokens)
        {
            // 受信タスク
            byte[] buf = new byte[1024];
            var receiveTask = client.GetStream().ReadAsync(buf, 0, buf.Length);

            // タイムアウトタスク
            Task timeoutTask = Task.Delay(timeout == -1 ? -1 : timeout * 1000);

            // 呼び出し元キャンセル検知タスク
            Task[] cancelTasks = tokens.Select(x => Task.Delay(-1, x)).ToArray();

            // タスク完了待機
            List<Task> tasks = new List<Task>();
            tasks.Add(receiveTask);
            tasks.Add(timeoutTask);
            tasks.AddRange(cancelTasks);
            Task completeTask = await Task.WhenAny(tasks);

            // タイムアウト
            if (completeTask == timeoutTask) throw new TimeoutException();

            // 呼び出し元キャンセル
            if (cancelTasks.Contains(completeTask)) throw new TaskCanceledException();

            // 受信完了
            await receiveTask;
            return buf;
        }

        /// <summary>
        /// ローカルIPアドレス一覧を返す
        /// </summary>
        /// <returns></returns>
        private static List<string> GetLocalIPAddresses()
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
