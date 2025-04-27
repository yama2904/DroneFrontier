namespace Network.Tcp
{
    public class DiscoveryCompletePacket : BasePacket
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DiscoveryCompletePacket() { }

        protected override BasePacket ParseBody(byte[] body)
        {
            return new DiscoveryCompletePacket();
        }

        protected override byte[] ConvertToPacketBody()
        {
            return new byte[0];
        }
    }
}
