using Network;
using System;
using System.Linq;
using System.Text;

namespace Drone.Battle.Network
{
    public class DamagePacket : BasePacket
    {
        /// <summary>
        /// ドローン名
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// ダメージ量
        /// </summary>
        public float Damage { get; private set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DamagePacket() { }

        public DamagePacket(string name, float damage)
        {
            Name = name;
            Damage = damage;
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);
            byte[] damage = BitConverter.GetBytes(Damage);
            return nameLen.Concat(name)
                          .Concat(damage)
                          .ToArray();
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            int nameLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            string name = Encoding.UTF8.GetString(body, offset, nameLen);
            offset += nameLen;

            float damage = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            return new DamagePacket(name, damage);
        }
    }
}
