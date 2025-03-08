using System;

namespace Network.Udp
{
    public class FrameSyncPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.FrameSync;

        public float TotalSeconds { get; private set; } = 0;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public FrameSyncPacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="totalSeconds">�o�ߎ���</param>
        public FrameSyncPacket(float totalSeconds)
        {
            TotalSeconds = totalSeconds;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            // �C���X�^���X���쐬���ĕԂ�
            return new FrameSyncPacket(BitConverter.ToSingle(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return BitConverter.GetBytes(TotalSeconds);
        }
    }
}