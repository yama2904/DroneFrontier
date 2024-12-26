using System.Linq;
using System.Text;

namespace Network.Udp
{
    public class DiscoverPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.Discover;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DiscoverPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        public DiscoverPacket(string name)
        {
            Name = name;
        }

        public override Packet Parse(byte[] data)
        {
            // ボディ部取得
            Split(data, out _, out byte[] body);

            // プレイヤー名取得
            string name = Encoding.UTF8.GetString(body);

            // インスタンスを作成して返す
            return new DiscoverPacket(name);
        }

        public override byte[] ConvertToPacket()
        {
            // パラメータをバイト変換
            byte[] name = Encoding.UTF8.GetBytes(Name);
            return GetHeaderBytes().Concat(name).ToArray();
        }
    }
}