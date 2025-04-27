using System.Text;

namespace Network.Udp
{
    internal class DiscoverPacket : BasePacket
    {
        /// <summary>
        /// �v���C���[��
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DiscoverPacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        public DiscoverPacket(string name)
        {
            Name = name;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            // �v���C���[���擾
            string name = Encoding.UTF8.GetString(body);

            // �C���X�^���X���쐬���ĕԂ�
            return new DiscoverPacket(name);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // �p�����[�^���o�C�g�ϊ�
            return Encoding.UTF8.GetBytes(Name);
        }
    }
}