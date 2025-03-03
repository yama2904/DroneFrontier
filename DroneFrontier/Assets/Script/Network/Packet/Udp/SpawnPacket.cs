using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Network.Udp
{
    public class SpawnPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.Spawn;

        public string AddressKey { get; private set; } = string.Empty;

        public string ObjectId { get; private set; } = string.Empty;

        public Vector3 Position { get; private set; } = Vector3.zero;

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        public object SpawnData { get; private set; } = null;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public SpawnPacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="obj">�����I�u�W�F�N�g</param>
        public SpawnPacket(MyNetworkBehaviour obj)
        {
            AddressKey = obj.GetAddressKey();
            ObjectId = obj.ObjectId;
            Position = obj.transform.position;
            Rotation = obj.transform.rotation;
            SpawnData = obj.CreateSpawnData();
        }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="addressKey">�v���n�u�̃L�[</param>
        /// <param name="id">�I�u�W�F�N�gID</param>
        /// <param name="position">���W</param>
        /// <param name="rotation">����</param>
        /// <param name="data">�����f�[�^</param>
        public SpawnPacket(string addressKey, string id, Vector3 position, Quaternion rotation, object data)
        {
            AddressKey = addressKey;
            ObjectId = id;
            Position = position;
            Rotation = rotation;
            SpawnData = data;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            int offset = 0;
            
            int keyLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            string key = Encoding.UTF8.GetString(body, offset, keyLen);
            offset += keyLen;

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

            object data = NetworkUtil.ConvertToObject<object>(body.Skip(offset).ToArray());

            // �C���X�^���X���쐬���ĕԂ�
            return new SpawnPacket(key, id, pos, rotate, data);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte[] key = Encoding.UTF8.GetBytes(AddressKey);
            byte[] keyLen = BitConverter.GetBytes(key.Length);

            byte[] id = Encoding.UTF8.GetBytes(ObjectId);
            byte[] idLen = BitConverter.GetBytes(id.Length);

            byte[] posX = BitConverter.GetBytes(Position.x);
            byte[] posY = BitConverter.GetBytes(Position.y);
            byte[] posZ = BitConverter.GetBytes(Position.z);

            byte[] rotateX = BitConverter.GetBytes(Rotation.x);
            byte[] rotateY = BitConverter.GetBytes(Rotation.y);
            byte[] rotateZ = BitConverter.GetBytes(Rotation.z);
            byte[] rotateW = BitConverter.GetBytes(Rotation.w);

            byte[] data = NetworkUtil.ConvertToByteArray(SpawnData);

            return keyLen.Concat(key)
                         .Concat(idLen)
                         .Concat(id)
                         .Concat(posX)
                         .Concat(posY)
                         .Concat(posZ)
                         .Concat(rotateX)
                         .Concat(rotateY)
                         .Concat(rotateZ)
                         .Concat(rotateW)
                         .Concat(data)
                         .ToArray();
        }
    }
}