using Network;
using Network.Udp;
using System;
using System.Linq;
using UnityEngine;

namespace Battle.Packet
{
    public class BarrierWeakLaserPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.None;

        public float WeakTime { get; private set; } = 0;

        public float LazerRange { get; private set; } = 0;

        public float LazerRadius { get; private set; } = 0;

        public float LaserTime { get; private set; } = 0;

        public float RotateSpeed { get; private set; } = 0;

        public Vector3 Position { get; private set; } = Vector3.zero;

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        public BarrierWeakLaserPacket() { }

        public BarrierWeakLaserPacket(float weakTime, float lazerRange, float lazerRadius, float laserTime, float rotateSpeed, Vector3 position, Quaternion rotation)
        {
            WeakTime = weakTime;
            LazerRange = lazerRange;
            LazerRadius = lazerRadius;
            LaserTime = laserTime;
            RotateSpeed = rotateSpeed;
            Position = position;
            Rotation = rotation;
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte[] weakTime = BitConverter.GetBytes(WeakTime);
            byte[] lazerRange = BitConverter.GetBytes(LazerRange);
            byte[] lazerRadius = BitConverter.GetBytes(LazerRadius);
            byte[] lazerTime = BitConverter.GetBytes(LaserTime);
            byte[] rotateSpeed = BitConverter.GetBytes(RotateSpeed);

            byte[] posX = BitConverter.GetBytes(Position.x);
            byte[] posY = BitConverter.GetBytes(Position.y);
            byte[] posZ = BitConverter.GetBytes(Position.z);

            byte[] rotateX = BitConverter.GetBytes(Rotation.x);
            byte[] rotateY = BitConverter.GetBytes(Rotation.y);
            byte[] rotateZ = BitConverter.GetBytes(Rotation.z);
            byte[] rotateW = BitConverter.GetBytes(Rotation.w);

            return weakTime.Concat(lazerRange)
                           .Concat(lazerRadius)
                           .Concat(lazerTime)
                           .Concat(rotateSpeed)
                           .Concat(posX)
                           .Concat(posY)
                           .Concat(posZ)
                           .Concat(rotateX)
                           .Concat(rotateY)
                           .Concat(rotateZ)
                           .Concat(rotateW)
                           .ToArray();
        }

        protected override IPacket ParseBody(byte[] body)
        {
            int offset = 0;

            float weakTime = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float lazerRange = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float lazerRadius = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float lazerTime = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            float rotateSpeed = BitConverter.ToSingle(body, offset);
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

            return new BarrierWeakLaserPacket(weakTime, lazerRange, lazerRadius, lazerTime, rotateSpeed, pos, rotate);
        }
    }
}
