namespace Network.Udp
{
    public class DroneEventPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.DroneEvent;

        /// <summary>
        /// バリア破壊
        /// </summary>
        public bool BarrierBreak { get; private set; } = false;

        /// <summary>
        /// バリア復活
        /// </summary>
        public bool BarrierResurrect { get; private set; } = false;

        /// <summary>
        /// ドローン死亡
        /// </summary>
        public bool Destroy { get; private set; } = false;

        /// <summary>
        /// コンストラクタ
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

            // インスタンスを作成して返す
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