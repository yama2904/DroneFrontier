using System;

namespace Network.Udp
{
    public class FrameSyncPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.FrameSync;

        public long SequenceId { get; private set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FrameSyncPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sequenceId">パケット連番</param>
        public FrameSyncPacket(long sequenceId)
        {
            SequenceId = sequenceId;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            // インスタンスを作成して返す
            return new FrameSyncPacket(BitConverter.ToInt64(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return BitConverter.GetBytes(SequenceId);
        }
    }
}