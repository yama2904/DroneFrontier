namespace Network.Udp
{
    public class SimpleSyncPacket : BasePacket
    {
        public object Value { get; private set; } = new object();

        public SimpleSyncPacket() { }

        public SimpleSyncPacket(object value)
        {
            Value = value;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            return new SimpleSyncPacket(NetworkUtil.ConvertToObject<object>(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return NetworkUtil.ConvertToByteArray(Value);
        }
    }
}