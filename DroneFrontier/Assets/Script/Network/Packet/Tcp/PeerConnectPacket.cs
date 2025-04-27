using System.Text;

namespace Network.Tcp
{
    /// <summary>
    /// クライアント同士の接続用TCPパケット
    /// </summary>
    public class PeerConnectPacket : BasePacket
    {
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

        protected override BasePacket ParseBody(byte[] body)
        {
            return new PeerConnectPacket(Encoding.UTF8.GetString(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return Encoding.UTF8.GetBytes(Name);
        }
    }
}