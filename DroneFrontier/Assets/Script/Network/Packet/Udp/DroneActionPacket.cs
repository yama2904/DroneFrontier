namespace Network.Udp
{
    public class DroneActionPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.DroneAction;

        /// <summary>
        /// ロックオン開始
        /// </summary>
        public bool StartLockOn { get; private set; } = false;

        /// <summary>
        /// ロックオン解除
        /// </summary>
        public bool StopLockOn { get; private set; } = false;

        /// <summary>
        /// ブースト開始
        /// </summary>
        public bool StartBoost { get; private set; } = false;

        /// <summary>
        /// ブースト停止
        /// </summary>
        public bool StopBoost { get; private set; } = false;

        /// <summary>
        /// アイテム1使用
        /// </summary>
        public bool UseItem1 { get; private set; } = false;

        /// <summary>
        /// アイテム2使用
        /// </summary>
        public bool UseItem2 { get; private set; } = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DroneActionPacket() { }

        public DroneActionPacket(bool startLockOn, bool stopLockOn, bool startBoost, bool stopBoost, bool useItem1, bool useItem2)
        {
            StartLockOn = startLockOn;
            StopLockOn = stopLockOn;
            StartBoost = startBoost;
            StopBoost = stopBoost;
            UseItem1 = useItem1;
            UseItem2 = useItem2;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            byte data = body[0];
            int offset = 0;
            bool startLockOn = BitFlagUtil.CheckFlag(data, offset++);
            bool stopLockOn = BitFlagUtil.CheckFlag(data, offset++);
            bool startBoost = BitFlagUtil.CheckFlag(data, offset++);
            bool stopBoost = BitFlagUtil.CheckFlag(data, offset++);
            bool item1 = BitFlagUtil.CheckFlag(data, offset++);
            bool item2 = BitFlagUtil.CheckFlag(data, offset++);
            return new DroneActionPacket(startLockOn, stopLockOn, startBoost, stopBoost, item1, item2);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte bitFlag = 0;
            int offset = 0;
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, StartLockOn);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, StopLockOn);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, StartBoost);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, StopBoost);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, UseItem1);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, UseItem2);
            return new byte[] { bitFlag };
        }
    }
}
