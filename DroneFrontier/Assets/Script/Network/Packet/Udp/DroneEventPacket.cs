namespace Network.Udp
{
    public class DroneEventPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.DroneEvent;

        /// <summary>
        /// �o���A�j��
        /// </summary>
        public bool BarrierBreak { get; private set; } = false;

        /// <summary>
        /// �o���A����
        /// </summary>
        public bool BarrierResurrect { get; private set; } = false;

        /// <summary>
        /// �h���[�����S
        /// </summary>
        public bool Destroy { get; private set; } = false;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DroneEventPacket() { }

        public DroneEventPacket(bool barrierBreak, bool resurrectBarrier, bool destroy)
        {
            BarrierBreak = barrierBreak;
            BarrierResurrect = resurrectBarrier;
            Destroy = destroy;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            byte data = body[0];
            int offset = 0;
            bool barrierBreak = BitFlagUtil.CheckFlag(data, offset++);
            bool resurrectBarrier = BitFlagUtil.CheckFlag(data, offset++);
            bool destroy = BitFlagUtil.CheckFlag(data, offset++);

            // �C���X�^���X���쐬���ĕԂ�
            return new DroneEventPacket(barrierBreak, resurrectBarrier, destroy);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte bitFlag = 0;
            int offset = 0;
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, BarrierBreak);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, BarrierResurrect);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, Destroy);
            return new byte[] { bitFlag };
        }
    }
}