namespace Network.Tcp
{
    public class DiscoveryCompletePacket : TcpPacket
    {
        public override TcpHeader Header => TcpHeader.DiscoveryComplete;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DiscoveryCompletePacket() { }

        protected override IPacket ParseBody(byte[] body)
        {
            return new DiscoveryCompletePacket();
        }

        protected override byte[] ConvertToPacketBody()
        {
            return new byte[0];
        }
    }
}
