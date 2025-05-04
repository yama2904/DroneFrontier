using Network;
using System;
using System.Linq;
using System.Text;

namespace Battle.Packet
{
    public class DroneDestroyPacket : BasePacket
    {
        /// <summary>
        /// �v���C���[��
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// ���X�|�[���h���[���̃I�u�W�F�N�gID
        /// </summary>
        public string RespawnDroneId { get; private set; } = string.Empty;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DroneDestroyPacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="newId">���X�|�[���h���[���̃I�u�W�F�N�gID</param>
        public DroneDestroyPacket(string name, string newId)
        {
            Name = name;
            RespawnDroneId = string.IsNullOrEmpty(newId) ? string.Empty : newId;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            // �v���C���[����
            int nameLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �v���C���[��
            string name = Encoding.UTF8.GetString(body, offset, nameLen);
            offset += nameLen;

            // �h���[��ID��
            int idLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �h���[��ID
            string id = Encoding.UTF8.GetString(body, offset, idLen);
            offset += idLen;

            // �C���X�^���X���쐬���ĕԂ�
            return new DroneDestroyPacket(name, id);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // �v���C���[��
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);

            // �h���[��ID
            byte[] id = Encoding.UTF8.GetBytes(RespawnDroneId);
            byte[] idLen = BitConverter.GetBytes(id.Length);

            return nameLen.Concat(name)
                          .Concat(idLen)
                          .Concat(id)
                          .ToArray();
        }
    }
}
