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
        /// �h���[����
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// �T�u����
        /// </summary>
        public WeaponType Weapon { get; private set; } = WeaponType.NONE;

        /// <summary>
        /// �X�g�b�N��
        /// </summary>
        public int StockNum { get; private set; } = 0;

        /// <summary>
        /// �X�N���v�g�̗L���L��
        /// </summary>
        public bool Enabled { get; private set; } = false;

        public Vector3 Position { get; private set; } = Vector3.zero;

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// �R���X�g���N�^
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

            // �T�u����
            WeaponType weapon = (WeaponType)BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �X�g�b�N��
            int stock = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �L���L��
            bool enabled = BitConverter.ToBoolean(body, offset);
            offset += sizeof(bool);

            // ���W
            float posX = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float posY = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float posZ = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            Vector3 pos = new Vector3(posX, posY, posZ);

            // �p�x
            float rotateX = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateY = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateZ = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateW = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            Quaternion rotate = new Quaternion(rotateX, rotateY, rotateZ, rotateW);

            // �h���[����
            string name = Encoding.UTF8.GetString(body, offset, body.Length - offset);
            offset += body.Length - offset;

            // �C���X�^���X���쐬���ĕԂ�
            return new DroneSpawnPacket(name, weapon, stock, enabled, pos, rotate);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // �T�u����
            byte[] weapon = BitConverter.GetBytes((int)Weapon);

            // �X�g�b�N��
            byte[] stock = BitConverter.GetBytes(StockNum);

            // �L���L��
            byte[] enabled = BitConverter.GetBytes(Enabled);

            // ���W
            byte[] posX = BitConverter.GetBytes(Position.x);
            byte[] posY = BitConverter.GetBytes(Position.y);
            byte[] posZ = BitConverter.GetBytes(Position.z);

            // �p�x
            byte[] rotateX = BitConverter.GetBytes(Rotation.x);
            byte[] rotateY = BitConverter.GetBytes(Rotation.y);
            byte[] rotateZ = BitConverter.GetBytes(Rotation.z);
            byte[] rotateW = BitConverter.GetBytes(Rotation.w);

            // �h���[����
            byte[] name = Encoding.UTF8.GetBytes(Name);

            // [�T�u����][�X�g�b�N��][�L���L��][�h���[����]
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
