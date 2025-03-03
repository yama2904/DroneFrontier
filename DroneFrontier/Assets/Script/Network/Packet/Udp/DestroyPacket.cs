using System.Text;

namespace Network.Udp
{
    public class DestroyPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.Destroy;

        /// <summary>
        /// �폜����I�u�W�F�N�g�̋��LID
        /// </summary>
        public string Id { get; private set; } = string.Empty;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DestroyPacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="id">�폜����I�u�W�F�N�g�̋��LID</param>
        public DestroyPacket(string id)
        {
            Id = id;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            // �C���X�^���X���쐬���ĕԂ�
            return new DestroyPacket(Encoding.UTF8.GetString(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return NetworkUtil.ConvertToByteArray(Encoding.UTF8.GetBytes(Id));
        }
    }
}