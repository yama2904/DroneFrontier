using Network;

namespace Battle.Packet
{
    public class DroneWatchPacket : BasePacket
    {
        public DroneWatchPacket() { }

        protected override byte[] ConvertToPacketBody()
        {
            return new byte[0];
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            return new DroneWatchPacket();
        }
    }
}