using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Network.Udp
{
    public class PositionPacket : BasePacket
    {
        public string ObjectId { get; private set; } = string.Empty;

        public Vector3 Position { get; private set; } = Vector3.zero;

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PositionPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="obj">座標を同期するオブジェクト</param>
        public PositionPacket(NetworkBehaviour obj)
        {
            ObjectId = obj.ObjectId;
            Position = obj.transform.position;
            Rotation = obj.transform.rotation;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">オブジェクトID</param>
        /// <param name="position">座標</param>
        /// <param name="rotation">向き</param>
        public PositionPacket(string id, Vector3 position, Quaternion rotation)
        {
            ObjectId = id;
            Position = position;
            Rotation = rotation;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            int idLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            string id = Encoding.UTF8.GetString(body, offset, idLen);
            offset += idLen;

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

            // インスタンスを作成して返す
            return new PositionPacket(id, pos, rotate);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte[] id = Encoding.UTF8.GetBytes(ObjectId);
            byte[] idLen = BitConverter.GetBytes(id.Length);

            byte[] posX = BitConverter.GetBytes(Position.x);
            byte[] posY = BitConverter.GetBytes(Position.y);
            byte[] posZ = BitConverter.GetBytes(Position.z);

            byte[] rotateX = BitConverter.GetBytes(Rotation.x);
            byte[] rotateY = BitConverter.GetBytes(Rotation.y);
            byte[] rotateZ = BitConverter.GetBytes(Rotation.z);
            byte[] rotateW = BitConverter.GetBytes(Rotation.w);

            return idLen.Concat(id)
                        .Concat(posX)
                        .Concat(posY)
                        .Concat(posZ)
                        .Concat(rotateX)
                        .Concat(rotateY)
                        .Concat(rotateZ)
                        .Concat(rotateW)
                        .ToArray();
        }
    }
}