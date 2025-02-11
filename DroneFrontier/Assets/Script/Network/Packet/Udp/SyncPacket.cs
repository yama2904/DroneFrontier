namespace Network.Udp
{
    public class SyncPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.Sync;

        public object Value { get; private set; } = new object();

        public SyncPacket() { }

        public SyncPacket(object value)
        {
            Value = value;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            return new SyncPacket(NetworkUtil.ConvertToObject<object>(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return NetworkUtil.ConvertToByteArray(Value);
        }
    }
}