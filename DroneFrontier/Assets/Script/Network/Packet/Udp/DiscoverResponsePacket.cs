using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Network.Udp
{
    public class DiscoverResponsePacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.DiscoverResponse;

        /// <summary>
        /// �z�X�g�̃v���C���[��
        /// </summary>
        public string HostName { get; private set; } = string.Empty;

        /// <summary>
        /// �e�N���C�A���g�̃v���C���[����IP�A�h���X
        /// </summary>
        public Dictionary<string, string> ClientAddresses = new Dictionary<string, string>();

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DiscoverResponsePacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="hostName">�z�X�g�̃v���C���[��</param>
        /// <param name="clientAdrs">�e�N���C�A���g�̃v���C���[����IP�A�h���X</param>
        public DiscoverResponsePacket(string hostName, Dictionary<string, string> clientAdrs)
        {
            HostName = hostName;
            ClientAddresses = clientAdrs;
        }

        public override Packet Parse(byte[] data)
        {
            // �{�f�B���擾
            Split(data, out _, out byte[] body);

            int offset = 0;

            // �z�X�g�v���C���[���̃o�C�g���擾
            int hostLen = BitConverter.ToInt32(body, offset);
            offset += 4;

            // �z�X�g�v���C���[���擾
            string hostName = Encoding.UTF8.GetString(body, offset, hostLen);
            offset += hostLen;

            // �v���C���[���擾
            int num = BitConverter.ToInt32(body, offset);
            offset += 4;

            // �e�v���C���[����IP�A�h���X�擾
            Dictionary<string, string> addresses = new Dictionary<string, string>();
            for (int i = 0; i < num; i++)
            {
                // �v���C���[���̃o�C�g���擾
                int nameLen = BitConverter.ToInt32(body, offset);
                offset += 4;

                // �v���C���[���擾
                string name = Encoding.UTF8.GetString(body, offset, nameLen);
                offset += nameLen;

                // IP�A�h���X�̃o�C�g���擾
                int addressLen = BitConverter.ToInt32(body, offset);
                offset += 4;

                // IP�A�h���X�擾
                string address = Encoding.UTF8.GetString(body, offset, addressLen);
                offset += addressLen;

                addresses.Add(name, address);
            }

            // �C���X�^���X���쐬���ĕԂ�
            return new DiscoverResponsePacket(hostName, addresses);
        }

        public override byte[] ConvertToPacket()
        {
            // �擪�̓w�b�_��
            byte[] data = GetHeaderBytes();

            // �z�X�g�̃v���C���[�����o�C�g�ϊ�
            byte[] hostByte = Encoding.UTF8.GetBytes(HostName);

            // �z�X�g�v���C���[���̃o�C�g�����擾
            byte[] hostLen = BitConverter.GetBytes(hostByte.Length);

            // �v���C���[�����p�P�b�g�Ɍ���
            byte[] num = BitConverter.GetBytes(ClientAddresses.Count);

            // [�z�X�g�v���C���[���o�C�g��] [�z�X�g�v���C���[��] [�v���C���[��] �̏��Ɍ���
            data = data.Concat(hostLen).Concat(hostByte).Concat(num).ToArray();

            // �e�v���C���[����IP�A�h���X���p�P�b�g�Ɍ�������
            foreach (string name in ClientAddresses.Keys)
            {
                // �v���C���[�����o�C�g�ϊ�
                byte[] nameByte = Encoding.UTF8.GetBytes(name);

                // �v���C���[���̃o�C�g�����擾
                byte[] nameLen = BitConverter.GetBytes(nameByte.Length);

                // IP�A�h���X���o�C�g�ϊ�
                byte[] addressByte = Encoding.UTF8.GetBytes(ClientAddresses[name]);

                // IP�A�h���X�̃o�C�g�����擾
                byte[] addressLen = BitConverter.GetBytes(addressByte.Length);

                // [�v���C���[���o�C�g��] [�v���C���[��] [IP�A�h���X�o�C�g��] [IP�A�h���X] �̏��Ɍ���
                data = data.Concat(nameLen).Concat(nameByte).Concat(addressLen).Concat(addressByte).ToArray();
            }

            return data;
        }
    }
}