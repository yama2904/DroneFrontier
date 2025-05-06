using System.Text;

namespace Network.Udp
{
    internal class DestroyPacket : BasePacket
    {
        /// <summary>
        /// 削除するオブジェクトの共有ID
        /// </summary>
        public string Id { get; private set; } = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DestroyPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">削除するオブジェクトの共有ID</param>
        public DestroyPacket(string id)
        {
            Id = id;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            // インスタンスを作成して返す
            return new DestroyPacket(Encoding.UTF8.GetString(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return Encoding.UTF8.GetBytes(Id);
        }
    }
}