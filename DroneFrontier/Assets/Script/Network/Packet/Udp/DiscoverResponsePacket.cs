using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Network.Udp
{
    public class DiscoverResponsePacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.DiscoverResponse;

        /// <summary>
        /// ホストのプレイヤー名
        /// </summary>
        public string HostName { get; private set; } = string.Empty;

        /// <summary>
        /// 各クライアントのプレイヤー名とIPアドレス
        /// </summary>
        public Dictionary<string, string> ClientAddresses = new Dictionary<string, string>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DiscoverResponsePacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="hostName">ホストのプレイヤー名</param>
        /// <param name="clientAdrs">各クライアントのプレイヤー名とIPアドレス</param>
        public DiscoverResponsePacket(string hostName, Dictionary<string, string> clientAdrs)
        {
            HostName = hostName;
            ClientAddresses = clientAdrs;
        }

        protected override IPacket ParseBody(byte[] body)
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

            // 各プレイヤー名とIPアドレス取得
            Dictionary<string, string> addresses = new Dictionary<string, string>();
            for (int i = 0; i < num; i++)
            {
                // プレイヤー名のバイト長取得
                int nameLen = BitConverter.ToInt32(body, offset);
                offset += sizeof(int);

                // プレイヤー名取得
                string name = Encoding.UTF8.GetString(body, offset, nameLen);
                offset += nameLen;

                // IPアドレスのバイト長取得
                int addressLen = BitConverter.ToInt32(body, offset);
                offset += sizeof(int);

                // IPアドレス取得
                string address = Encoding.UTF8.GetString(body, offset, addressLen);
                offset += addressLen;

                addresses.Add(name, address);
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

            // 各プレイヤー名とIPアドレスをパケットに結合する
            foreach (string name in ClientAddresses.Keys)
            {
                // プレイヤー名をバイト変換
                byte[] nameByte = Encoding.UTF8.GetBytes(name);

                // プレイヤー名のバイト長を取得
                byte[] nameLen = BitConverter.GetBytes(nameByte.Length);

                // IPアドレスをバイト変換
                byte[] addressByte = Encoding.UTF8.GetBytes(ClientAddresses[name]);

                // IPアドレスのバイト長を取得
                byte[] addressLen = BitConverter.GetBytes(addressByte.Length);

                // [プレイヤー名バイト長] [プレイヤー名] [IPアドレスバイト長] [IPアドレス] の順に結合
                body = body.Concat(nameLen).Concat(nameByte).Concat(addressLen).Concat(addressByte).ToArray();
            }

            return body;
        }
    }
}