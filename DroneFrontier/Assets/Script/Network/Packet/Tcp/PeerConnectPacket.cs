using System;
using System.Linq;
using System.Text;

namespace Network.Tcp
{
    /// <summary>
    /// クライアント同士の接続用TCPパケット
    /// </summary>
    internal class PeerConnectPacket : BasePacket
    {
        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// UDPローカルポート
        /// </summary>
        public int UdpPort { get; private set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PeerConnectPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="port">UDPローカルポート</param>
        public PeerConnectPacket(string name, int port)
        {
            Name = name;
            UdpPort = port;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            int nameLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            string name = Encoding.UTF8.GetString(body, offset, nameLen);
            offset += nameLen;

            int port = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            return new PeerConnectPacket(name, port);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);
            byte[] port = BitConverter.GetBytes(UdpPort);
            return nameLen.Concat(name).Concat(port).ToArray();
        }
    }
}