namespace Network.Udp
{
    public class SimpleSyncPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.SimpleSync;

        public object Value { get; private set; } = new object();

        public SimpleSyncPacket() { }

        public SimpleSyncPacket(object value)
        {
            Value = value;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            return new SimpleSyncPacket(NetworkUtil.ConvertToObject<object>(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return NetworkUtil.ConvertToByteArray(Value);
        }
    }
}