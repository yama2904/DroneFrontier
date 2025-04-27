using Network;
using System;
using System.Linq;

namespace Drone.Battle.Network
{
    public class DroneStatusPacket : BasePacket
    {
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

        protected override BasePacket ParseBody(byte[] body)
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