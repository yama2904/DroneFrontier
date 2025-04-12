namespace Network.Udp
{
    public class GetItemPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.GetItem;

        /// <summary>
        /// �擾�A�C�e��
        /// </summary>
        public IDroneItem Item { get; private set; } = null;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public GetItemPacket() { }


        public GetItemPacket(IDroneItem item)
        {
            Item = item;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            return new GetItemPacket(NetworkUtil.ConvertToObject<IDroneItem>(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return NetworkUtil.ConvertToByteArray(Item);
        }
    }
}