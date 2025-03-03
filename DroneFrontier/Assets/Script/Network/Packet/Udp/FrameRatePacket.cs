using System;

namespace Network.Udp
{
    public class FrameRatePacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.FrameRate;

        /// <summary>
        /// フレームレート
        /// </summary>
        public int FrameRate { get; private set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FrameRatePacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="frameRate">フレームレート</param>
        public FrameRatePacket(int frameRate)
        {
            FrameRate = frameRate;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            return new FrameRatePacket(BitConverter.ToInt32(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return BitConverter.GetBytes(FrameRate);
        }
    }
}