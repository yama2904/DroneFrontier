using System.Linq;
using System.Text;

namespace Network.Udp
{
    public class DiscoverPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.Discover;

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

        public override Packet Parse(byte[] data)
        {
            // �{�f�B���擾
            Split(data, out _, out byte[] body);

            // �v���C���[���擾
            string name = Encoding.UTF8.GetString(body);

            // �C���X�^���X���쐬���ĕԂ�
            return new DiscoverPacket(name);
        }

        public override byte[] ConvertToPacket()
        {
            // �p�����[�^���o�C�g�ϊ�
            byte[] name = Encoding.UTF8.GetBytes(Name);
            return GetHeaderBytes().Concat(name).ToArray();
        }
    }
}