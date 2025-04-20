using Network.Udp;
using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Network
{
    public class DroneSpawnPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.None;

        /// <summary>
        /// ドローン名
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// サブ武器
        /// </summary>
        public WeaponType Weapon { get; private set; } = WeaponType.NONE;

        /// <summary>
        /// ストック数
        /// </summary>
        public int StockNum { get; private set; } = 0;

        /// <summary>
        /// スクリプトの有効有無
        /// </summary>
        public bool Enabled { get; private set; } = false;

        public Vector3 Position { get; private set; } = Vector3.zero;

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DroneSpawnPacket() { }

        public DroneSpawnPacket(string name, WeaponType weapon, int stock, bool enabled, Vector3 position, Quaternion rotation)
        {
            Name = name;
            Weapon = weapon;
            StockNum = stock;
            Enabled = enabled;
            Position = position;
            Rotation = rotation;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            int offset = 0;

            // サブ武器
            WeaponType weapon = (WeaponType)BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // ストック数
            int stock = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // 有効有無
            bool enabled = BitConverter.ToBoolean(body, offset);
            offset += sizeof(bool);

            // 座標
            float posX = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float posY = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float posZ = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            Vector3 pos = new Vector3(posX, posY, posZ);

            // 角度
            float rotateX = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateY = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateZ = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateW = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            Quaternion rotate = new Quaternion(rotateX, rotateY, rotateZ, rotateW);

            // ドローン名
            string name = Encoding.UTF8.GetString(body, offset, body.Length - offset);
            offset += body.Length - offset;

            // インスタンスを作成して返す
            return new DroneSpawnPacket(name, weapon, stock, enabled, pos, rotate);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // サブ武器
            byte[] weapon = BitConverter.GetBytes((int)Weapon);

            // ストック数
            byte[] stock = BitConverter.GetBytes(StockNum);

            // 有効有無
            byte[] enabled = BitConverter.GetBytes(Enabled);

            // 座標
            byte[] posX = BitConverter.GetBytes(Position.x);
            byte[] posY = BitConverter.GetBytes(Position.y);
            byte[] posZ = BitConverter.GetBytes(Position.z);

            // 角度
            byte[] rotateX = BitConverter.GetBytes(Rotation.x);
            byte[] rotateY = BitConverter.GetBytes(Rotation.y);
            byte[] rotateZ = BitConverter.GetBytes(Rotation.z);
            byte[] rotateW = BitConverter.GetBytes(Rotation.w);

            // ドローン名
            byte[] name = Encoding.UTF8.GetBytes(Name);

            // [サブ武器][ストック数][有効有無][ドローン名]
            return weapon.Concat(stock)
                         .Concat(enabled)
                         .Concat(posX)
                         .Concat(posY)
                         .Concat(posZ)
                         .Concat(rotateX)
                         .Concat(rotateY)
                         .Concat(rotateZ)
                         .Concat(rotateW)
                         .Concat(name)
                         .ToArray();
        }
    }
}
