namespace Network.Tcp
{
    /// <summary>
    /// �N���C�A���g���m�̐ڑ������ʒm�p�P�b�g
    /// </summary>
    internal class ConnectedClientsPacket : BasePacket
    {
        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public ConnectedClientsPacket() { }

        protected override BasePacket ParseBody(byte[] body)
        {
            return new ConnectedClientsPacket();
        }

        protected override byte[] ConvertToPacketBody()
        {
            return new byte[0];
        }
    }
}
