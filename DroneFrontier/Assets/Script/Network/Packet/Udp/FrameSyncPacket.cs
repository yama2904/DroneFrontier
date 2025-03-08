using System;

namespace Network.Udp
{
    public class FrameSyncPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.FrameSync;

        public long SequenceId { get; private set; } = 0;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public FrameSyncPacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="sequenceId">�p�P�b�g�A��</param>
        public FrameSyncPacket(long sequenceId)
        {
            SequenceId = sequenceId;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            // �C���X�^���X���쐬���ĕԂ�
            return new FrameSyncPacket(BitConverter.ToInt64(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return BitConverter.GetBytes(SequenceId);
        }
    }
}