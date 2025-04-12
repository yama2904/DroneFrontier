using System;
using System.Linq;

namespace Network.Udp
{
    public class DroneStatusPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.DroneStatus;

        /// <summary>
        /// HP
        /// </summary>
        public float Hp { get; private set; } = 0;

        /// <summary>
        /// 移動スピード
        /// </summary>
        public float MoveSpeed { get; private set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DroneStatusPacket() { }

        public DroneStatusPacket(float hp, float moveSpeed)
        {
            Hp = hp;
            MoveSpeed = moveSpeed;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            int offset = 0;

            float hp = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float moveSpeed = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            return new DroneStatusPacket(hp, moveSpeed);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte[] hp = BitConverter.GetBytes(Hp);
            byte[] moveSpeed = BitConverter.GetBytes(MoveSpeed);
            return hp.Concat(moveSpeed)
                     .ToArray();
        }
    }
}