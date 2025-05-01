using System.Collections.Generic;

namespace Network.Tcp
{
    public class ConnectionCompletedPacket : BasePacket
    {
        public List<string> PlayerNames { get; private set; } = new List<string>();

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public ConnectionCompletedPacket() { }

        protected override byte[] ConvertToPacketBody()
        {
            return new byte[0];
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            return new ConnectionCompletedPacket();
        }
    }
}
