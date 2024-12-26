using System.Linq;
using System.Text;

namespace Network.Tcp
{
    /// <summary>
    /// クライアント同士の接続用TCPパケット
    /// </summary>
    public class PeerConnectPacket : TcpPacket
    {
        public override TcpHeader Header => TcpHeader.PeerConnect;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PeerConnectPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        public PeerConnectPacket(string name)
        {
            Name = name;
        }

        public override Packet Parse(byte[] data)
        {
            // ボディ部取得
            Split(data, out _, out byte[] body);

            return new PeerConnectPacket(Encoding.UTF8.GetString(body));
        }

        public override byte[] ConvertToPacket()
        {
            byte[] name = Encoding.UTF8.GetBytes(Name);
            return GetHeaderBytes().Concat(name).ToArray();
        }
    }
}