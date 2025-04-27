namespace Network.Tcp
{
    public class DiscoveryCompletePacket : BasePacket
    {
        /// <summary>
        /// �R���X�g���N�^
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
