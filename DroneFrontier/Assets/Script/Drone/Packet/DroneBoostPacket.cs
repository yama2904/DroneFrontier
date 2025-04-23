using Common;
using Network;
using Network.Udp;

namespace Drone.Network
{
    public class DroneBoostPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.None;

        public bool StartBoost { get; private set; } = false;

        public bool StopBoost { get; private set; } = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DroneBoostPacket() { }

        public DroneBoostPacket(bool start, bool stop)
        {
            StartBoost = start;
            StopBoost = stop;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            byte data = body[0];
            int offset = 0;
            bool start = BitFlagUtil.CheckFlag(data, offset++);
            bool stop = BitFlagUtil.CheckFlag(data, offset++);
            return new DroneBoostPacket(start, stop);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte bitFlag = 0;
            int offset = 0;
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, StartBoost);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, StopBoost);
            return new byte[] { bitFlag };
        }
    }
}
