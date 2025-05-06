using Network;
using System;
using System.Linq;
using System.Text;

namespace Drone.Battle.Network
{
    public class DroneStatusPacket : BasePacket
    {
        /// <summary>
        /// ドローン名
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// HP
        /// </summary>
        public float Hp { get; private set; } = 0;

        /// <summary>
        /// バリアHP
        /// </summary>
        public float BarrierHp { get; private set; } = 0;

        /// <summary>
        /// 移動スピード
        /// </summary>
        public float MoveSpeed { get; private set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DroneStatusPacket() { }

        public DroneStatusPacket(string name, float hp, float barrierHp, float moveSpeed)
        {
            Name = name;
            Hp = hp;
            BarrierHp = barrierHp;
            MoveSpeed = moveSpeed;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            int nameLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            string name = Encoding.UTF8.GetString(body, offset, nameLen);
            offset += nameLen;

            float hp = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float barrierHp = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float moveSpeed = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            return new DroneStatusPacket(name, hp, barrierHp, moveSpeed);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);
            byte[] hp = BitConverter.GetBytes(Hp);
            byte[] barrierHp = BitConverter.GetBytes(BarrierHp);
            byte[] moveSpeed = BitConverter.GetBytes(MoveSpeed);
            return nameLen.Concat(name)
                          .Concat(hp)
                          .Concat(barrierHp)
                          .Concat(moveSpeed)
                          .ToArray();
        }
    }
}