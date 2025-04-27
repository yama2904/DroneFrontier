using System;

namespace Network.Udp
{
    public class FrameRatePacket : BasePacket
    {
        /// <summary>
        /// �t���[�����[�g
        /// </summary>
        public int FrameRate { get; private set; } = 0;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public FrameRatePacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="frameRate">�t���[�����[�g</param>
        public FrameRatePacket(int frameRate)
        {
            FrameRate = frameRate;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            return new FrameRatePacket(BitConverter.ToInt32(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return BitConverter.GetBytes(FrameRate);
        }
    }
}