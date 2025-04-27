using System.Text;

namespace Network.Udp
{
    internal class DiscoverPacket : BasePacket
    {
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

        protected override BasePacket ParseBody(byte[] body)
        {
            // プレイヤー名取得
            string name = Encoding.UTF8.GetString(body);

            // インスタンスを作成して返す
            return new DiscoverPacket(name);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // パラメータをバイト変換
            return Encoding.UTF8.GetBytes(Name);
        }
    }
}