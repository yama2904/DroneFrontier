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
        /// �^�C���A�E�g�ƃL�����Z�����l�����Ĕ񓯊���UDP��M���s��
        /// </summary>
        /// <param name="client">��M����UDP�N���C�A���g</param>
        /// <param name="timeout">��M�^�C���A�E�g�i�b�j</param>
        /// <param name="tokens">�L�����Z���g�[�N��</param>
        /// <returns>��M�f�[�^</returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<UdpReceiveResult> ReceiveUdpAsync(UdpClient client, int timeout, params CancellationToken[] tokens)
        {
            // ��M�^�X�N
            var receiveTask = client.ReceiveAsync();

            // �^�C���A�E�g�^�X�N
            Task timeoutTask = Task.Delay(timeout == -1 ? -1 : timeout * 1000);

            // �Ăяo�����L�����Z�����m�^�X�N
            Task[] cancelTasks = tokens.Select(x => Task.Delay(-1, x)).ToArray();

            while (true)
            {
                // �^�X�N�����ҋ@
                List<Task> tasks = new List<Task>();
                tasks.Add(receiveTask);
                tasks.Add(timeoutTask);
                tasks.AddRange(cancelTasks);
                Task completeTask = await Task.WhenAny(tasks);

                // UDP��M
                if (completeTask == receiveTask)
                {
                    // �������������p�P�b�g�̏ꍇ�͖����i�u���[�h�L���X�g�l���j
                    if (GetLocalIPAddresses().Contains(receiveTask.Result.RemoteEndPoint.Address.ToString()))
                    {
                        receiveTask = client.ReceiveAsync();
                        continue;
                    }
                    break;
                }

                // �^�C���A�E�g
                if (completeTask == timeoutTask) throw new TimeoutException();

                // �Ăяo�����L�����Z��
                if (cancelTasks.Contains(completeTask)) throw new TaskCanceledException();
            }

            // ��M����
            return await receiveTask;
        }

        /// <summary>
        /// �^�C���A�E�g�ƃL�����Z�����l�����Ĕ񓯊���TCP�R�l�N�V�����̎�t���s��
        /// </summary>
        /// <param name="listener">�R�l�N�V������t���s��TCP���X�i�[</param>
        /// <param name="port">���[�J���|�[�g�ԍ�</param>
        /// <param name="timeout">��t�^�C���A�E�g�i�b�j</param>
        /// <param name="allowIP">�R�l�N�V������������IP�A�h���X�i�S�Ă�IP���狖����ꍇ��IPAddress.Any���w��j</param>
        /// <param name="tokens">�L�����Z���g�[�N��</param>
        /// <returns>�R�l�N�V�������m������TCP�N���C�A���g</returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<TcpClient> AcceptTcpClientAsync(TcpListener listener, int port, int timeout, IPAddress allowIP, params CancellationToken[] tokens)
        {
            // �R�l�N�V������t�^�X�N
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

            // �^�C���A�E�g�^�X�N
            Task timeoutTask = Task.Delay(timeout == -1 ? -1 : timeout * 1000);

            // �Ăяo�����L�����Z�����m�^�X�N
            Task[] cancelTasks = tokens.Select(x => Task.Delay(-1, x)).ToArray();

            TcpClient tcpClient = null;
            try
            {
                while (true)
                {
                    // �^�X�N�����ҋ@
                    List<Task> tasks = new List<Task>();
                    tasks.Add(acceptTask);
                    tasks.Add(timeoutTask);
                    tasks.AddRange(cancelTasks);
                    Task completeTask = await Task.WhenAny(tasks);

                    // �ڑ��v��
                    if (completeTask == acceptTask)
                    {
                        tcpClient = await acceptTask;

                        // ���M��IP����
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

                    // �^�C���A�E�g
                    if (completeTask == timeoutTask) throw new TimeoutException();

                    // �Ăяo�����L�����Z��
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
        /// �^�C���A�E�g�ƃL�����Z�����l�����Ĕ񓯊���TCP�R�l�N�V�������m��
        /// </summary>
        /// <param name="client">�R�l�N�V�������s��TCP�N���C�A���g</param>
        /// <param name="remoteIP">�R�l�N�V������IP�A�h���X</param>
        /// <param name="remotePort">�R�l�N�V������|�[�g�ԍ�</param>
        /// <param name="timeout">�R�l�N�V�����^�C���A�E�g�i�b�j</param>
        /// <param name="tokens">�L�����Z���g�[�N��</param>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task ConnectAsync(TcpClient client, IPAddress remoteIP, int remotePort, int timeout, params CancellationToken[] tokens)
        {
            // �R�l�N�V�����^�X�N
            var connTask = client.ConnectAsync(remoteIP, remotePort);

            // �^�C���A�E�g�^�X�N
            Task timeoutTask = Task.Delay(timeout == -1 ? -1 : timeout * 1000);

            // �Ăяo�����L�����Z�����m�^�X�N
            Task[] cancelTasks = tokens.Select(x => Task.Delay(-1, x)).ToArray();

            // �^�X�N�����ҋ@
            List<Task> tasks = new List<Task>();
            tasks.Add(connTask);
            tasks.Add(timeoutTask);
            tasks.AddRange(cancelTasks);
            Task completeTask = await Task.WhenAny(tasks);

            // �^�C���A�E�g
            if (completeTask == timeoutTask) throw new TimeoutException();

            // �Ăяo�����L�����Z��
            if (cancelTasks.Contains(completeTask)) throw new TaskCanceledException();

            // �ڑ�����
            return;
        }

        /// <summary>
        /// �^�C���A�E�g�ƃL�����Z�����l�����Ĕ񓯊���TCP��M���s��
        /// </summary>
        /// <param name="client">��M����TCP�N���C�A���g</param>
        /// <param name="timeout">��M�^�C���A�E�g�i�b�j</param>
        /// <param name="tokens">�L�����Z���g�[�N��</param>
        /// <returns>��M�f�[�^</returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<byte[]> ReceiveTcpAsync(TcpClient client, int timeout, params CancellationToken[] tokens)
        {
            // ��M�^�X�N
            byte[] buf = new byte[1024];
            var receiveTask = client.GetStream().ReadAsync(buf, 0, buf.Length);

            // �^�C���A�E�g�^�X�N
            Task timeoutTask = Task.Delay(timeout == -1 ? -1 : timeout * 1000);

            // �Ăяo�����L�����Z�����m�^�X�N
            Task[] cancelTasks = tokens.Select(x => Task.Delay(-1, x)).ToArray();

            // �^�X�N�����ҋ@
            List<Task> tasks = new List<Task>();
            tasks.Add(receiveTask);
            tasks.Add(timeoutTask);
            tasks.AddRange(cancelTasks);
            Task completeTask = await Task.WhenAny(tasks);

            // �^�C���A�E�g
            if (completeTask == timeoutTask) throw new TimeoutException();

            // �Ăяo�����L�����Z��
            if (cancelTasks.Contains(completeTask)) throw new TaskCanceledException();

            // ��M����
            await receiveTask;
            return buf;
        }

        /// <summary>
        /// ���[�J��IP�A�h���X�ꗗ��Ԃ�
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
