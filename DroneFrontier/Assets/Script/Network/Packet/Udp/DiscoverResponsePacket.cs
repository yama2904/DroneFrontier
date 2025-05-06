using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Network.Udp
{
    internal class DiscoverResponsePacket : BasePacket
    {
        /// <summary>
        /// ホストのプレイヤー名
        /// </summary>
        public string HostName { get; private set; } = string.Empty;

        /// <summary>
        /// 各クライアントのTCPアドレス
        /// </summary>
        public List<string> ClientAddresses = new List<string>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DiscoverResponsePacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="hostName">ホストのプレイヤー名</param>
        /// <param name="clientAdrs">各クライアントのTCPアドレス</param>
        public DiscoverResponsePacket(string hostName, List<string> clientAdrs)
        {
            HostName = hostName;
            ClientAddresses = clientAdrs;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            // ホストプレイヤー名のバイト長取得
            int hostLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // ホストプレイヤー名取得
            string hostName = Encoding.UTF8.GetString(body, offset, hostLen);
            offset += hostLen;

            // プレイヤー数取得
            int num = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // 各プレイヤーのTCPアドレス取得
            List<string> addresses = new List<string>();
            for (int i = 0; i < num; i++)
            {
                // アドレスのバイト長取得
                int addressLen = BitConverter.ToInt32(body, offset);
                offset += sizeof(int);

                // アドレス取得
                string address = Encoding.UTF8.GetString(body, offset, addressLen);
                offset += addressLen;

                addresses.Add(address);
            }

            // インスタンスを作成して返す
            return new DiscoverResponsePacket(hostName, addresses);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // 戻り値
            byte[] body;

            // ホストのプレイヤー名をバイト変換
            byte[] hostByte = Encoding.UTF8.GetBytes(HostName);

            // ホストプレイヤー名のバイト長を取得
            byte[] hostLen = BitConverter.GetBytes(hostByte.Length);

            // プレイヤー数をパケットに結合
            byte[] num = BitConverter.GetBytes(ClientAddresses.Count);

            // [ホストプレイヤー名バイト長] [ホストプレイヤー名] [プレイヤー数] の順に結合
            body = hostLen.Concat(hostByte).Concat(num).ToArray();

            // TCPアドレスをパケットに結合する
            foreach (string address in ClientAddresses)
            {
                // アドレスをバイト変換
                byte[] addressByte = Encoding.UTF8.GetBytes(address);

                // アドレスのバイト長を取得
                byte[] addressLen = BitConverter.GetBytes(addressByte.Length);

                // [アドレスバイト長] [アドレス] の順に結合
                body = body.Concat(addressLen).Concat(addressByte).ToArray();
            }

            return body;
        }
    }
}