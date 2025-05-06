using Network;
using System;
using System.Linq;
using UnityEngine;

namespace Battle.Packet
{
    public class MagnetSpawnPacket : BasePacket
    {
        public float DownPercent { get; private set; } = 0;

        public float ActiveTime { get; private set; } = 0;

        public float AreaSize { get; private set; } = 0;

        public Vector3 Position { get; private set; } = Vector3.zero;

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        public MagnetSpawnPacket() { }

        public MagnetSpawnPacket(float downPercent, float activeTime, float areaSize, Vector3 position, Quaternion rotate)
        {
            DownPercent = downPercent;
            ActiveTime = activeTime;
            AreaSize = areaSize;
            Position = position;
            Rotation = rotate;
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte[] downPercent = BitConverter.GetBytes(DownPercent);
            byte[] activeTime = BitConverter.GetBytes(ActiveTime);
            byte[] areaSize = BitConverter.GetBytes(AreaSize);

            byte[] posX = BitConverter.GetBytes(Position.x);
            byte[] posY = BitConverter.GetBytes(Position.y);
            byte[] posZ = BitConverter.GetBytes(Position.z);

            byte[] rotateX = BitConverter.GetBytes(Rotation.x);
            byte[] rotateY = BitConverter.GetBytes(Rotation.y);
            byte[] rotateZ = BitConverter.GetBytes(Rotation.z);
            byte[] rotateW = BitConverter.GetBytes(Rotation.w);

            return downPercent.Concat(activeTime)
                              .Concat(areaSize)
                              .Concat(posX)
                              .Concat(posY)
                              .Concat(posZ)
                              .Concat(rotateX)
                              .Concat(rotateY)
                              .Concat(rotateZ)
                              .Concat(rotateW)
                              .ToArray();
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            float downPercent = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float activeTime = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float areaSize = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float posX = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float posY = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float posZ = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            Vector3 pos = new Vector3(posX, posY, posZ);

            float rotateX = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateY = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateZ = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            float rotateW = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);
            Quaternion rotate = new Quaternion(rotateX, rotateY, rotateZ, rotateW);

            return new MagnetSpawnPacket(downPercent, activeTime, areaSize, pos, rotate);
        }
    }
}